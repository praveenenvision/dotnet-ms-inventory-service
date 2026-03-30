using System.Text;
using System.Text.Json;
using DotnetMsPoc.Shared.Events;
using InventoryService.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace InventoryService.Infrastructure.Messaging;

public class OrderConfirmedConsumer : BackgroundService
{
    private readonly ILogger<OrderConfirmedConsumer> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private IConnection? _connection;
    private IModel? _channel;

    public OrderConfirmedConsumer(
        ILogger<OrderConfirmedConsumer> logger,
        IConfiguration configuration,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        var rabbitHost = _configuration["RabbitMQ:Host"] ?? "localhost";
        var rabbitPort = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672");

        for (int i = 0; i < 10; i++)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = rabbitHost,
                    Port = rabbitPort,
                    UserName = _configuration["RabbitMQ:Username"] ?? "guest",
                    Password = _configuration["RabbitMQ:Password"] ?? "guest"
                };

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                _channel.ExchangeDeclare("domain_events", ExchangeType.Topic, durable: true);
                _channel.QueueDeclare("inventory_order_confirmed", durable: true, exclusive: false, autoDelete: false);
                _channel.QueueBind("inventory_order_confirmed", "domain_events", "order.confirmed");

                _logger.LogInformation("OrderConfirmedConsumer connected to RabbitMQ");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to connect to RabbitMQ (attempt {Attempt}): {Message}", i + 1, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, i)), stoppingToken);
            }
        }

        if (_channel == null)
        {
            _logger.LogError("Could not connect to RabbitMQ after retries");
            return;
        }

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (_, ea) =>
        {
            try
            {
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                var orderEvent = JsonSerializer.Deserialize<OrderConfirmedEvent>(body);

                if (orderEvent != null)
                {
                    _logger.LogInformation("[TraceId: {TraceId}] Processing OrderConfirmed for Order #{OrderId}",
                        orderEvent.TraceId, orderEvent.OrderId);

                    using var scope = _serviceProvider.CreateScope();
                    var inventoryService = scope.ServiceProvider.GetRequiredService<IInventoryAppService>();

                    foreach (var item in orderEvent.Items)
                    {
                        await inventoryService.ReduceStockAsync(item.ProductId, item.Quantity, orderEvent.TraceId);
                    }
                }

                _channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing OrderConfirmed event");
                _channel.BasicNack(ea.DeliveryTag, false, true);
            }
        };

        _channel.BasicConsume("inventory_order_confirmed", autoAck: false, consumer: consumer);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}

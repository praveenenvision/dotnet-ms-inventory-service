using System.Text;
using System.Text.Json;
using DotnetMsPoc.Shared.Events;
using InventoryService.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace InventoryService.Infrastructure.Messaging;

public class RabbitMqEventPublisher : IEventPublisher
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMqEventPublisher> _logger;

    public RabbitMqEventPublisher(IConfiguration configuration, ILogger<RabbitMqEventPublisher> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task PublishAsync<T>(T domainEvent, string routingKey) where T : IDomainEvent
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:Host"] ?? "localhost",
                Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
                UserName = _configuration["RabbitMQ:Username"] ?? "guest",
                Password = _configuration["RabbitMQ:Password"] ?? "guest"
            };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.ExchangeDeclare("domain_events", ExchangeType.Topic, durable: true);

            var message = JsonSerializer.Serialize(domainEvent);
            var body = Encoding.UTF8.GetBytes(message);

            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.Headers = new Dictionary<string, object>
            {
                { "event_type", domainEvent.EventType },
                { "trace_id", domainEvent.TraceId }
            };

            channel.BasicPublish(
                exchange: "domain_events",
                routingKey: routingKey,
                basicProperties: properties,
                body: body);

            _logger.LogInformation("Published {EventType} event, TraceId: {TraceId}",
                domainEvent.EventType, domainEvent.TraceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish {EventType} event", domainEvent.EventType);
        }

        return Task.CompletedTask;
    }
}

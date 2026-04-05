using System.Reflection;
using System.Text.Json;
using InventoryService.Application.Interfaces;
using InventoryService.Application.Services;
using InventoryService.Domain.Interfaces;
using InventoryService.Infrastructure.Repositories;
using DotnetMsPoc.Shared.Events;
using DotnetMsPoc.Shared.Messaging;
using DotnetMsPoc.Shared.Middleware;
using DotnetMsPoc.Shared.Telemetry;
using DotnetMsPoc.Shared.ServiceDiscovery;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Inventory Service API",
        Version = "v1",
        Description = "Microservice for managing product inventory. Consumes OrderConfirmed events to reduce stock."
    });
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);
});

builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
builder.Services.AddScoped<IInventoryAppService, InventoryAppService>();
builder.Services.AddEventPublisher(builder.Configuration);
builder.Services.AddEventConsumer(
    builder.Configuration,
    queueName: "inventory_order_confirmed",
    routingKeys: ["order.confirmed"],
    handler: async (sp, routingKey, jsonBody) =>
    {
        var orderEvent = JsonSerializer.Deserialize<OrderConfirmedEvent>(jsonBody);
        if (orderEvent != null)
        {
            var inventoryService = sp.GetRequiredService<IInventoryAppService>();
            foreach (var item in orderEvent.Items)
            {
                await inventoryService.ReduceStockAsync(item.ProductId, item.Quantity, orderEvent.TraceId);
            }
        }
    });

builder.Services.AddCustomOpenTelemetry("InventoryService");

// Health checks & Service Discovery
builder.Services.AddHealthChecks();
builder.Services.AddConsulServiceDiscovery(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Inventory Service API v1"));

app.UseCors();
app.UseTraceIdMiddleware();
app.UseCustomOpenTelemetry();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

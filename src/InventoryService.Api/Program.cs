using System.Reflection;
using InventoryService.Application.Interfaces;
using InventoryService.Application.Services;
using InventoryService.Domain.Interfaces;
using InventoryService.Infrastructure.Messaging;
using InventoryService.Infrastructure.Repositories;
using DotnetMsPoc.Shared.Middleware;
using DotnetMsPoc.Shared.Telemetry;

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
builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
builder.Services.AddHostedService<OrderConfirmedConsumer>();

builder.Services.AddCustomOpenTelemetry("InventoryService");

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
app.MapControllers();

app.Run();

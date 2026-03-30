using InventoryService.Application.DTOs;
using InventoryService.Application.Interfaces;
using InventoryService.Application.Mappings;
using InventoryService.Domain.Interfaces;
using DotnetMsPoc.Shared.Events;
using Microsoft.Extensions.Logging;

namespace InventoryService.Application.Services;

public class InventoryAppService : IInventoryAppService
{
    private readonly IInventoryRepository _repository;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<InventoryAppService> _logger;

    public InventoryAppService(
        IInventoryRepository repository,
        IEventPublisher eventPublisher,
        ILogger<InventoryAppService> logger)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<IEnumerable<InventoryItemResponse>> GetAllInventoryAsync()
    {
        _logger.LogInformation("Retrieving all inventory items");
        var items = await _repository.GetAllAsync();
        return items.Select(i => i.ToResponse());
    }

    public async Task<InventoryItemResponse?> GetInventoryByProductIdAsync(int productId)
    {
        _logger.LogInformation("Retrieving inventory for product ID: {ProductId}", productId);
        var item = await _repository.GetByProductIdAsync(productId);
        return item?.ToResponse();
    }

    public async Task<InventoryItemResponse?> UpdateInventoryAsync(int productId, UpdateInventoryRequest request)
    {
        _logger.LogInformation("Updating inventory for product ID: {ProductId}", productId);
        var item = await _repository.UpdateAsync(productId, request.ProductName, request.QuantityAvailable);
        return item?.ToResponse();
    }

    public async Task<InventoryItemResponse?> ReduceStockAsync(int productId, int quantity, string traceId)
    {
        _logger.LogInformation("[TraceId: {TraceId}] Reducing stock for product {ProductId} by {Quantity}",
            traceId, productId, quantity);

        var result = await _repository.ReduceStockAsync(productId, quantity);
        if (result is null)
        {
            _logger.LogWarning("[TraceId: {TraceId}] Insufficient stock for product {ProductId}", traceId, productId);
            return null;
        }

        await _eventPublisher.PublishAsync(new InventoryReducedEvent
        {
            ProductId = productId,
            ProductName = result.ProductName,
            QuantityReduced = quantity,
            NewStock = result.QuantityAvailable,
            TraceId = traceId
        }, "inventory.reduced");

        _logger.LogInformation("[TraceId: {TraceId}] Stock reduced for product {ProductId}. New stock: {NewStock}",
            traceId, productId, result.QuantityAvailable);

        return result.ToResponse();
    }
}

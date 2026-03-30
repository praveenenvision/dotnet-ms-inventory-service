using InventoryService.Application.DTOs;
using InventoryService.Domain.Entities;

namespace InventoryService.Application.Mappings;

public static class InventoryMappings
{
    public static InventoryItemResponse ToResponse(this InventoryItem item) => new()
    {
        Id = item.Id,
        ProductId = item.ProductId,
        ProductName = item.ProductName,
        QuantityAvailable = item.QuantityAvailable,
        LastUpdated = item.LastUpdated
    };
}

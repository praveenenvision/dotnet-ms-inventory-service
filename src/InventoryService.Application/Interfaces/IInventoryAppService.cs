using InventoryService.Application.DTOs;

namespace InventoryService.Application.Interfaces;

public interface IInventoryAppService
{
    Task<IEnumerable<InventoryItemResponse>> GetAllInventoryAsync();
    Task<InventoryItemResponse?> GetInventoryByProductIdAsync(int productId);
    Task<InventoryItemResponse?> UpdateInventoryAsync(int productId, UpdateInventoryRequest request);
    Task<InventoryItemResponse?> ReduceStockAsync(int productId, int quantity, string traceId);
}

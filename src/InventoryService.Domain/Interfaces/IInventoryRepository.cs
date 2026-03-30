using InventoryService.Domain.Entities;

namespace InventoryService.Domain.Interfaces;

public interface IInventoryRepository
{
    Task<IEnumerable<InventoryItem>> GetAllAsync();
    Task<InventoryItem?> GetByProductIdAsync(int productId);
    Task<InventoryItem?> UpdateAsync(int productId, string productName, int quantityAvailable);
    Task<InventoryItem?> ReduceStockAsync(int productId, int quantity);
}

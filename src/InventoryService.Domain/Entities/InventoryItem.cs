namespace InventoryService.Domain.Entities;

public class InventoryItem
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int QuantityAvailable { get; set; }
    public DateTime LastUpdated { get; set; }
}

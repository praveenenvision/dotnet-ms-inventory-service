namespace InventoryService.Application.DTOs;

public class UpdateInventoryRequest
{
    public string ProductName { get; set; } = string.Empty;
    public int QuantityAvailable { get; set; }
}

using Dapper;
using InventoryService.Domain.Entities;
using InventoryService.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace InventoryService.Infrastructure.Repositories;

public class InventoryRepository : IInventoryRepository
{
    private readonly string _connectionString;

    public InventoryRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new ArgumentNullException("DefaultConnection string is not configured.");
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<IEnumerable<InventoryItem>> GetAllAsync()
    {
        using var connection = CreateConnection();
        return await connection.QueryAsync<InventoryItem>(
            @"SELECT id, product_id AS ProductId, product_name AS ProductName, quantity_available AS QuantityAvailable, last_updated AS LastUpdated
              FROM inventory_schema.inventory ORDER BY product_id");
    }

    public async Task<InventoryItem?> GetByProductIdAsync(int productId)
    {
        using var connection = CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<InventoryItem>(
            @"SELECT id, product_id AS ProductId, product_name AS ProductName, quantity_available AS QuantityAvailable, last_updated AS LastUpdated
              FROM inventory_schema.inventory WHERE product_id = @ProductId",
            new { ProductId = productId });
    }

    public async Task<InventoryItem?> UpdateAsync(int productId, string productName, int quantityAvailable)
    {
        using var connection = CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<InventoryItem>(
            @"INSERT INTO inventory_schema.inventory (product_id, product_name, quantity_available, last_updated)
              VALUES (@ProductId, @ProductName, @QuantityAvailable, NOW())
              ON CONFLICT (product_id) DO UPDATE SET product_name = @ProductName, quantity_available = @QuantityAvailable, last_updated = NOW()
              RETURNING id, product_id AS ProductId, product_name AS ProductName, quantity_available AS QuantityAvailable, last_updated AS LastUpdated",
            new { ProductId = productId, ProductName = productName, QuantityAvailable = quantityAvailable });
    }

    public async Task<InventoryItem?> ReduceStockAsync(int productId, int quantity)
    {
        using var connection = CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<InventoryItem>(
            @"UPDATE inventory_schema.inventory
              SET quantity_available = quantity_available - @Quantity, last_updated = NOW()
              WHERE product_id = @ProductId AND quantity_available >= @Quantity
              RETURNING id, product_id AS ProductId, product_name AS ProductName, quantity_available AS QuantityAvailable, last_updated AS LastUpdated",
            new { ProductId = productId, Quantity = quantity });
    }
}

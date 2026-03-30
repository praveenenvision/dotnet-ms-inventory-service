using Microsoft.AspNetCore.Mvc;
using InventoryService.Application.DTOs;
using InventoryService.Application.Interfaces;

namespace InventoryService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryAppService _inventoryService;

    public InventoryController(IInventoryAppService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<InventoryItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var items = await _inventoryService.GetAllInventoryAsync();
        return Ok(items);
    }

    [HttpGet("{productId:int}")]
    [ProducesResponseType(typeof(InventoryItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByProductId(int productId)
    {
        var item = await _inventoryService.GetInventoryByProductIdAsync(productId);
        if (item == null)
            return NotFound(new { message = $"Inventory for product {productId} not found" });
        return Ok(item);
    }

    [HttpPut("{productId:int}")]
    [ProducesResponseType(typeof(InventoryItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(int productId, [FromBody] UpdateInventoryRequest request)
    {
        var item = await _inventoryService.UpdateInventoryAsync(productId, request);
        if (item == null)
            return NotFound(new { message = $"Inventory for product {productId} not found" });
        return Ok(item);
    }
}

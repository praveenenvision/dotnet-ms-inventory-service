using FluentAssertions;
using InventoryService.Application.DTOs;
using InventoryService.Application.Interfaces;
using InventoryService.Application.Services;
using InventoryService.Domain.Entities;
using InventoryService.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using DotnetMsPoc.Shared.Events;
using DotnetMsPoc.Shared.Messaging;

namespace InventoryService.Tests.Application;

public class InventoryAppServiceTests
{
    private readonly Mock<IInventoryRepository> _repoMock = new();
    private readonly Mock<IEventPublisher> _publisherMock = new();
    private readonly Mock<ILogger<InventoryAppService>> _loggerMock = new();
    private readonly InventoryAppService _sut;

    public InventoryAppServiceTests()
    {
        _sut = new InventoryAppService(_repoMock.Object, _publisherMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetAllInventory_ReturnsAll()
    {
        var items = new List<InventoryItem>
        {
            new() { Id = 1, ProductId = 10, ProductName = "Widget", QuantityAvailable = 50, LastUpdated = DateTime.UtcNow },
            new() { Id = 2, ProductId = 20, ProductName = "Gadget", QuantityAvailable = 30, LastUpdated = DateTime.UtcNow }
        };
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(items);

        var result = (await _sut.GetAllInventoryAsync()).ToList();

        result.Should().HaveCount(2);
        result[0].ProductName.Should().Be("Widget");
        result[1].ProductName.Should().Be("Gadget");
    }

    [Fact]
    public async Task GetByProductId_Found_ReturnsResponse()
    {
        var item = new InventoryItem { Id = 1, ProductId = 10, ProductName = "Widget", QuantityAvailable = 50, LastUpdated = DateTime.UtcNow };
        _repoMock.Setup(r => r.GetByProductIdAsync(10)).ReturnsAsync(item);

        var result = await _sut.GetInventoryByProductIdAsync(10);

        result.Should().NotBeNull();
        result!.ProductId.Should().Be(10);
        result.ProductName.Should().Be("Widget");
    }

    [Fact]
    public async Task GetByProductId_NotFound_ReturnsNull()
    {
        _repoMock.Setup(r => r.GetByProductIdAsync(99)).ReturnsAsync((InventoryItem?)null);

        var result = await _sut.GetInventoryByProductIdAsync(99);

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateInventory_Found_ReturnsUpdated()
    {
        var updated = new InventoryItem { Id = 1, ProductId = 10, ProductName = "Widget v2", QuantityAvailable = 100, LastUpdated = DateTime.UtcNow };
        _repoMock.Setup(r => r.UpdateAsync(10, "Widget v2", 100)).ReturnsAsync(updated);

        var result = await _sut.UpdateInventoryAsync(10, new UpdateInventoryRequest { ProductName = "Widget v2", QuantityAvailable = 100 });

        result.Should().NotBeNull();
        result!.ProductName.Should().Be("Widget v2");
        result.QuantityAvailable.Should().Be(100);
    }

    [Fact]
    public async Task ReduceStock_SufficientStock_ReturnsUpdatedAndPublishesEvent()
    {
        var reduced = new InventoryItem { Id = 1, ProductId = 10, ProductName = "Widget", QuantityAvailable = 45, LastUpdated = DateTime.UtcNow };
        _repoMock.Setup(r => r.ReduceStockAsync(10, 5)).ReturnsAsync(reduced);

        var result = await _sut.ReduceStockAsync(10, 5, "trace-123");

        result.Should().NotBeNull();
        result!.QuantityAvailable.Should().Be(45);
        _publisherMock.Verify(p => p.PublishAsync(
            It.Is<InventoryReducedEvent>(e =>
                e.ProductId == 10 &&
                e.QuantityReduced == 5 &&
                e.NewStock == 45 &&
                e.TraceId == "trace-123"),
            "inventory.reduced"), Times.Once);
    }

    [Fact]
    public async Task ReduceStock_InsufficientStock_ReturnsNull()
    {
        _repoMock.Setup(r => r.ReduceStockAsync(10, 999)).ReturnsAsync((InventoryItem?)null);

        var result = await _sut.ReduceStockAsync(10, 999, "trace-456");

        result.Should().BeNull();
        _publisherMock.Verify(p => p.PublishAsync(It.IsAny<InventoryReducedEvent>(), It.IsAny<string>()), Times.Never);
    }
}

using DotnetMsPoc.Shared.Events;

namespace InventoryService.Application.Interfaces;

public interface IEventPublisher
{
    Task PublishAsync<T>(T domainEvent, string routingKey) where T : IDomainEvent;
}

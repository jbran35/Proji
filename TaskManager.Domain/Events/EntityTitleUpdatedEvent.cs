using TaskManager.Domain.Interfaces;

namespace TaskManager.Domain.Events
{
    public record EntityTitleUpdatedEvent (Guid Id) : IDomainEvent
    {
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }
}
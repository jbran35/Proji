using TaskManager.Domain.Interfaces;

namespace TaskManager.Domain.Events
{
    public record TodoItemCompletedEvent (Guid Id) : IDomainEvent
    {
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }
}
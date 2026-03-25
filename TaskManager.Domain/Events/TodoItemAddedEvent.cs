using TaskManager.Domain.Interfaces;

namespace TaskManager.Domain.Events
{
    public record TodoItemAddedEvent(Guid Id):IDomainEvent
    {
        public DateTime OccurredOn { get; } = DateTime.UtcNow;

    }
}

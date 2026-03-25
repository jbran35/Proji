using TaskManager.Domain.Enums;

namespace TaskManager.Domain.Interfaces
{
    public interface ITodoItemEntry
    {
        // Used so that Repo methods can directly return only what is needed to display a task in a grid/list. 
        // Not REALLY needed now, but would be more beneficial if the TodoItem entity is expanded. 
        public Guid Id { get; }
        public Guid OwnerId { get; }
        public Guid? AssigneeId { get; }
        public string Title { get; }
        public string? Description { get; }
        public string ProjectTitle { get; }
        public string? AssigneeName { get; }
        public string OwnerName { get; }
        public Priority? Priority { get; }
        public DateTime? DueDate { get; }
        public DateTime CreatedOn { get; }
        public Status Status { get; }
    }
}

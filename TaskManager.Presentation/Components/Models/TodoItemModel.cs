using TaskManager.Domain.Enums;

namespace TaskManager.Presentation.Components.Models
{
    public record TodoItemModel
    {
        public string? Title { get; set; }
        public Guid? ProjectId { get; set; }
        public string? Description { get; set; }
        public Guid? AssigneeId { get; set; }
        public Priority? Priority { get; set; }
        public DateTime? DueDate { get; set; }

    }
}

using System.ComponentModel.DataAnnotations;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.Projects.DTOs.Requests

{
    /// <summary>
    /// Request DTO for creating a new TodoItem.
    /// </summary>
    public record CreateTodoItemRequest
    {
        [Required(ErrorMessage = "Cannot Create a New Task Without a Name")]
        public required string Title { get; init; }

        [Required(ErrorMessage = "Need to Assign New Task To Project")]
        public required Guid ProjectId { get; init; }
        public string? Description { get; init; }
        public Guid? AssigneeId { get; init; } = Guid.Empty;
        public Status? Status { get; init; }
        public Priority? Priority { get; init; }
        public DateTime? DueDate { get; init; }
    }
}

using System.ComponentModel.DataAnnotations;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.TodoItems.DTOs.Requests
{

    /// <summary>
    /// A request to update the details of a TodoItem.
    /// </summary>
    public record UpdateTodoItemRequest
    {
        [Required(ErrorMessage = "Project ID is required.")]
        public required Guid ProjectId { get; init; }
        public string? Title { get; init; }
        public string? Description { get; init; }
        public Guid? AssigneeId { get; init; }
        public Status? Status { get; init; }
        public Priority? Priority { get; init; }
        public DateTime? DueDate { get; init; }
    }
}

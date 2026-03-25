using System.ComponentModel.DataAnnotations;

namespace TaskManager.Application.TodoItems.DTOs.Requests
{
    /// <summary>
    /// A request containing the necessary information to update the project assignment of a task,
    /// including: the new project ID, the task ID, and the user ID of the person making the request.
    /// ProjectId, NewProjectId, TodoItemId, 
    /// 
    /// Not currently used, but could be used in the future.
    /// </summary>

    public record UpdateTodoItemProjectAssignmentRequest
    {
        [Required(ErrorMessage = "Need to select new project for task assignment")]
        public required Guid ProjectId { get; set; }

        [Required(ErrorMessage = "Need to select new project for task assignment")]
        public required Guid NewProjectId { get; set; }

    }
}

namespace TaskManager.Application.TodoItems.DTOs.Responses
{
    /// <summary>
    /// A reqsponse containing the result of updating a task's project assignment. 
    /// Not currently used, but could be a future feature.
    /// </summary>
    public record UpdateTodoItemProjectAssignmentResponse
    {
        public Guid TodoItemId { get; init; }
        public Guid NewProjectId { get; init; }
        public string NewProjectTitle { get; init; } = string.Empty;
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
    }
}

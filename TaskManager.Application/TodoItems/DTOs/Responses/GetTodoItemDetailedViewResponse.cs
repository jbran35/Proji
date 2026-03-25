namespace TaskManager.Application.TodoItems.DTOs.Responses
{
    /// <summary>
    /// A DTO for returning detailed view of a TodoItem
    /// </summary>
    public record GetTodoItemDetailedViewResponse
    {
        public TodoItemEntry? TodoItemDetails { get; init; }
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;

    }
}

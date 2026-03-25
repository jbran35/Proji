namespace TaskManager.Application.TodoItems.DTOs
{
    /// <summary>
    /// Not currently used, because a "Detailed View" can be achieved using TodoItemEnty. 
    /// If TodoItems are expanded to include more than is reasonable to load when vieweing them in a list view, 
    /// this could be used for that retrieval. 
    /// </summary>
    public record TodoItemDetailedViewDto : TodoItemEntry
    {
       // ... Any added properties in the future
    }
}

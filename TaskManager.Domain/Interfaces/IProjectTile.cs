using TaskManager.Domain.Enums;

namespace TaskManager.Domain.Interfaces
{
    // Allows for the Project Repository to return only the needed information to display Project Tiles. 
    //Description is added so it can be cached immediately.
    public interface IProjectTile  
    {
        Guid Id { get; }
        Guid OwnerId { get; }
        string Title { get; }
        string? Description { get; }
        int TotalTodoItemCount { get; }
        int CompleteTodoItemCount { get; }
        DateTime CreatedOn { get; }
        Status Status { get; }

    }
}

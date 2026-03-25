using TaskManager.Domain.Enums;
using TaskManager.Domain.Interfaces;

namespace TaskManager.Application.Projects.DTOs
{
    /// <summary>
    /// A DTO with all the needed information to display a project as a tile on the user's dashboard.
    /// </summary>
    public record ProjectTileDto : IProjectTile
    {
        public Guid Id { get; init; }
        public Guid OwnerId { get; init; }
        public required string Title { get; set; }

        //Description is included here as it would always be included upon project creation.
        //The user only needs to see the tile-displayed details, but adding it here helps with caching.
        public string? Description { get; set; } = string.Empty;
        public int TotalTodoItemCount { get; set; }
        public int CompleteTodoItemCount { get; set; }
        public DateTime CreatedOn { get; init; }
        public Status Status { get; set; } = Status.Incomplete;
    }
}
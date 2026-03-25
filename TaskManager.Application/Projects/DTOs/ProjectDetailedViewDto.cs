using TaskManager.Application.TodoItems.DTOs;
using TaskManager.Domain.Interfaces;

namespace TaskManager.Application.Projects.DTOs
{
    /// <summary>
    /// A DTO conaining a project's basic details and a list of its Todo Items.
    /// </summary>
    public record ProjectDetailedViewDto : ProjectTileDto, IProjectDetailedView
    {
        public List<TodoItemEntry> TodoItems { get; set; } = [];
        IEnumerable<ITodoItemEntry> IProjectDetailedView.TodoItems => TodoItems;
    }
}

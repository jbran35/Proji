using MediatR;
using TaskManager.Application.Common;
using TaskManager.Application.Interfaces;
using TaskManager.Application.TodoItems.DTOs;
using TaskManager.Domain.Common;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.Projects.Commands
{
    /// <summary>
    /// Represents a request to create a todo item and add it to a project.
    /// </summary>
    /// <param name="ProjectId"></param>
    /// <param name="UserId"></param>
    /// <param name="AssigneeId"></param>
    /// <param name="Title"></param>
    /// <param name="Description"></param>
    /// <param name="DueDate"></param>
    /// <param name="Priority"></param>
    public record AddTodoItemCommand(
       Guid ProjectId,
       Guid UserId,
       Guid? AssigneeId,

       string Title,
       string? Description,

       DateTime? DueDate,
       Priority? Priority

   ) : IRequest<Result<TodoItemEntry>>, ICacheInvalidator
    {
        public string[] Keys => [.. GetKeys()];

        private IEnumerable<string> GetKeys()
        {
            yield return CacheKeys.ProjectDetailedViews(UserId, ProjectId);
            yield return CacheKeys.ProjectTiles(UserId);
        }
    }
}

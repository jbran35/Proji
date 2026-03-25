using TaskManager.Application.Projects.DTOs;
using TaskManager.Application.TodoItems.DTOs;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Interfaces;

namespace TaskManager.Application.Projects.Mappers
{
    /// <summary>
    /// A static class to provide easy conversion/mapping options between DTOs, or from entities to a particular DTO.
    /// </summary>
    public static class ProjectMapper
    {

        #region ToProjectDetailedView
        public static ProjectDetailedViewDto ToProjectDetailedView(this ProjectDetailsDto details)
        {
            return new ProjectDetailedViewDto
            {

                Id = details.Id,
                Title = details.Title,
                Description = details.Description,
                CreatedOn = details.CreatedOn,
            };
        }

        public static ProjectDetailedViewDto ToProjectDetailedViewDto(this IProjectDetailedView project)
        {
            return new ProjectDetailedViewDto
            {
                Id = project.Id,
                Title = project.Title,
                Description = project.Description,
                Status = project.Status,
                CreatedOn = project.CreatedOn,
                TotalTodoItemCount = project.TotalTodoItemCount,
                CompleteTodoItemCount = project.CompleteTodoItemCount,

                TodoItems = [.. project.TodoItems.Select(static t => new TodoItemEntry
                {
                    Id = t.Id,
                    OwnerId = t.OwnerId,
                    AssigneeId = t.AssigneeId,
                    Title = t.Title,
                    Description = t.Description,
                    ProjectTitle = t.ProjectTitle,
                    AssigneeName = t.AssigneeName,
                    OwnerName = t.OwnerName,
                    Priority = t.Priority,
                    DueDate = t.DueDate,
                    CreatedOn = t.CreatedOn,
                    Status = t.Status
                }
                )]
            };
        }
        public static ProjectDetailedViewDto ToProjectDetailedViewDto(this Project project)
        {
            var detailedDto = new ProjectDetailedViewDto
            {
                Id = project.Id,
                Title = project.Title,
                Description = project.Description,
                Status = project.Status,
                CreatedOn = project.CreatedOn
            };


            if (project.TodoItems.Count == 0)
            {
                return detailedDto;
            }

            detailedDto.TodoItems = [.. project.TodoItems.Select(static t => new TodoItemEntry
            {
                Id = t.Id,
                OwnerId = t.OwnerId,
                AssigneeId = t.AssigneeId,
                Title = t.Title,
                Description = t.Description,
                ProjectTitle = t.Project.Title,
                AssigneeName = t.Assignee?.FullName ?? string.Empty,
                OwnerName = t.Owner?.FullName ?? string.Empty,
                Priority = t.Priority,
                DueDate = t.DueDate,
                CreatedOn = t.CreatedOn,
                Status = t.Status
            })];

            return detailedDto;
        }

        #endregion

        #region ToProjectTileDto & List
        public static ProjectTileDto ToProjectTileDto(this ProjectDetailsDto details)
        {
            return new ProjectTileDto
            {
                Id = details.Id,
                Title = details.Title,
                Description = details.Description,
                CreatedOn = details.CreatedOn,
            };
        }

        public static ProjectTileDto ToProjectTileDto(this ProjectDetailedViewDto project)
        {
            return new ProjectTileDto
            {
                Id = project.Id,
                OwnerId = project.OwnerId,
                Title = project.Title,
                Description = project.Description,
                TotalTodoItemCount = project.TotalTodoItemCount,
                CompleteTodoItemCount = project.CompleteTodoItemCount,
                CreatedOn = project.CreatedOn,
                Status = project.Status
            };
        }

        public static List<ProjectTileDto> ToProjectTileDtoList(this IEnumerable<IProjectTile> tiles)
        {
            return [.. tiles.Select(t => new ProjectTileDto
            {
                Id = t.Id,
                OwnerId = t.OwnerId,
                Title = t.Title,
                Description = t.Description,
                TotalTodoItemCount = t.TotalTodoItemCount,
                CompleteTodoItemCount = t.CompleteTodoItemCount,
                CreatedOn = t.CreatedOn,
                Status = t.Status
            })];
        }

        #endregion

        #region ToProjectDetailsDto
        public static ProjectDetailsDto ToProjectDetailsDto(this Project project)
        {
            return new ProjectDetailsDto
            {
                Id = project.Id,
                Title = project.Title,
                Description = project.Description,
            };
        }
        #endregion

    }
}

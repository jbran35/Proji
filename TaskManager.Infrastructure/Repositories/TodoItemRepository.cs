using Microsoft.EntityFrameworkCore;
using TaskManager.Application.TodoItems.DTOs;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.Domain.Interfaces;

namespace TaskManager.Infrastructure.Repositories
{
    /// <summary>
    /// Repository containing the needed CRUD operations for Todo Items.
    /// </summary>
    /// <param name="context"></param>
    public class TodoItemRepository (ApplicationDbContext context) : ITodoItemRepository
    {
        private readonly ApplicationDbContext _context = context;
        public void Add(TodoItem todoItem) => _context.TodoItems.Add(todoItem);
        public void Delete(TodoItem todoItem) => _context.Remove(todoItem);

        public async Task<IReadOnlyList<ITodoItemEntry>> GetMyAssignedTodoItemsAsync(Guid userId, CancellationToken cancellationToken)
        {
            return await _context.TodoItems
                .Where(t => t.AssigneeId == userId && t.Status != Status.Deleted)
                .Select(t => new TodoItemEntry
                {
                    Id = t.Id, 
                    AssigneeId = t.AssigneeId,
                    OwnerId = t.OwnerId, 
                    Title = t.Title, 
                    Description = t.Description,
                    ProjectTitle = t.Project.Title, 
                    OwnerName = (t.Owner != null) ? t.Owner.FullName : string.Empty, 
                    Priority = t.Priority, 
                    DueDate = t.DueDate,
                    CreatedOn = t.CreatedOn,
                    Status = t.Status
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Guid>> GetMyTodoItemsAssignedToUser(Guid userId, Guid assigneeId, CancellationToken cancellationToken)
        {
            return await _context.TodoItems
                .Where(t => t.AssigneeId == assigneeId && t.OwnerId == userId && t.Status != Status.Deleted) 
                .Select(t => t.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Guid>> UnassignTasksByIdAsync(List<Guid> todoItemIds, CancellationToken cancellationToken)
        {
            var projectIds = new List<Guid>();
            
            if (todoItemIds.Count == 0) return projectIds;

            var todoItems = await _context.TodoItems.Where(t => todoItemIds.Contains(t.Id)).ToListAsync(cancellationToken);

            foreach (var todoItem in todoItems)
            {
                todoItem.Unassign();
                if (!projectIds.Contains(todoItem.ProjectId))
                    projectIds.Add(todoItem.ProjectId);
            }
            
            return projectIds;
        }

        public async Task<TodoItem?> GetTodoItemByIdAsync(Guid todoId, CancellationToken cancellationToken)
        {
            return await _context.TodoItems
                .Include(t => t.Assignee)
                .Include(t => t.Owner)
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.Id == todoId && t.Status != Status.Deleted, cancellationToken);
        }

        public void Update(TodoItem todoItem) => _context.Update(todoItem);

        public void Update(IEnumerable<TodoItem> todoList) => _context.Update(todoList);
    }
}

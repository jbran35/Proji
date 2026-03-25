using TaskManager.Domain.Common;
using TaskManager.Domain.Enums;
using TaskManager.Domain.Events;
using TaskManager.Domain.ValueObjects;

namespace TaskManager.Domain.Entities
{
    public sealed class Project : Entry
    {
        private readonly List<TodoItem> _todoItems = [];
        public IReadOnlyCollection<TodoItem> TodoItems => _todoItems.AsReadOnly();
        
        private Project() { } // Parameterless constructor for EFCore

        private Project(Title title, Guid ownerId, Description description)
            : base(title, description, ownerId) { }

        public static Result<Project> Create(Title title, Description description, Guid ownerId)
        {
            if (ownerId == Guid.Empty)
                return Result<Project>.Failure(ErrorCode.DomainRuleViolation,"OwnerId cannot be empty."); 
            
            return Result<Project>.Success(new Project(title, ownerId, description));
        }

        public override Result MarkAsComplete()
        {
            var deletedCheck = CheckIfDeleted();
            if (deletedCheck.IsFailure) return deletedCheck;

            if (Status == Status.Complete) return Result.Success();

            Status = Status.Complete;

            var incompleteItems = TodoItems.Where(t => t.Status == Status.Incomplete && t.Status != Status.Deleted);

            foreach (var item in incompleteItems)
            {
                item.MarkAsComplete();
            }

            AddDomainEvent(new ProjectCompletedEvent(this.Id));
            return Result.Success();
        }

        public Result AddTodoItem(TodoItem? todoItem)
        {
            var deletedCheck = CheckIfDeleted(); //User should not manipulate items in a deleted project
            if (deletedCheck.IsFailure) return deletedCheck;

            if (todoItem is null)
                return Result<Project>.Failure(ErrorCode.DomainRuleViolation,"Need task to add to project");

            if (todoItem.Status == Status.Deleted)
                return Result<Project>.Failure(ErrorCode.ObjectDeleted,"Cannot add a deleted TodoItem to a project.");

            if (todoItem.OwnerId != this.OwnerId)
                return Result<Project>.Failure(ErrorCode.Forbidden,"TodoItem owner does not match project owner.");

            if (todoItem.ProjectId != this.Id)
                return Result<Project>.Failure(ErrorCode.DomainRuleViolation,"TodoItem does not belong to this project.");

            if (TodoItems.Any(t => t.Id == todoItem.Id))
                return Result<Project>.Failure(ErrorCode.AlreadyExists,"TodoItem is already added to this project.");

            _todoItems.Add(todoItem);
            AddDomainEvent(new TodoItemAddedEvent(this.Id));

            return Result.Success();
        }

        public Result DeleteTodoItem(TodoItem? todoItem)
        {
            var deletedCheck = CheckIfDeleted(); //User should not manipulate items in a deleted project
            if (deletedCheck.IsFailure) return deletedCheck;

            if (todoItem is null)
                return Result<Project>.Failure(ErrorCode.DomainRuleViolation,"Error deleting task");

            if (todoItem.ProjectId != this.Id)
                return Result<Project>.Failure(ErrorCode.Forbidden,"TodoItem does not belong to this project.");

            if (_todoItems.Count == 0)
                return Result<Project>.Failure(ErrorCode.DomainRuleViolation,"No TodoItems are associated with this project.");

            if (!_todoItems.Contains(todoItem))
                return Result<Project>.Failure(ErrorCode.DomainRuleViolation,"TodoItem is not part of this project.");

            if (todoItem.Status == Status.Deleted)
                return Result<Project>.Failure(ErrorCode.ObjectDeleted, "TodoItem is already deleted.");

            todoItem.MarkAsDeleted(); 
            _todoItems.Remove(todoItem);
            AddDomainEvent(new TodoItemRemovedEvent(this.Id));

            return Result.Success();
        }
    }
}

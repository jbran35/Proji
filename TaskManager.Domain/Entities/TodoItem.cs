using TaskManager.Domain.Common;
using TaskManager.Domain.Enums;
using TaskManager.Domain.ValueObjects;


namespace TaskManager.Domain.Entities
{
    public sealed class TodoItem : Entry
    {
        public Guid ProjectId { get; private set; }
        public Project Project { get; private set; } = null!;
        public Guid? AssigneeId { get; set; }
        public User? Assignee { get; set; }
        public Priority? Priority { get; private set; }
        public DateTime? DueDate { get; private set; }
        
        private TodoItem() { } // Parameterless constructor for EFCore
        private TodoItem(Title title, Description description, Guid ownerId, Guid projectId, Guid? assigneeId, 
            Priority? priority, DateTime? dueDate) : base(title, description, ownerId)
        {
            this.ProjectId = projectId;
            this.AssigneeId = assigneeId;
            this.Priority = priority ?? Enums.Priority.None;
            this.DueDate = dueDate;
        }

        public static Result<TodoItem> Create(Title title, Description description, Guid ownerId, Guid projectId, Guid? assigneeId, 
            Priority? priority, DateTime? dueDate)
        {
            if (projectId == Guid.Empty)
                return Result<TodoItem>.Failure(ErrorCode.DomainRuleViolation, "Project ID cannot be empty.");

            if(ownerId == Guid.Empty)
                return Result<TodoItem>.Failure(ErrorCode.DomainRuleViolation,"Owner ID cannot be empty.");

            if(priority.HasValue && !Enum.IsDefined(priority.Value))
                return Result<TodoItem>.Failure(ErrorCode.DomainRuleViolation,"Invalid priority value.");


            return Result<TodoItem>.Success(new TodoItem(title, description, ownerId, projectId, assigneeId, priority, dueDate));
        }

        public Result UpdateDueDate(DateTime? dueDate)
        {
            this.DueDate = dueDate;
            return Result.Success();
        }

        public Result UpdateProjectAssignment(Guid projectId)
        {
            if (projectId == Guid.Empty)
                return Result.Failure(ErrorCode.DomainRuleViolation,"Project ID cannot be empty.");
            
            this.ProjectId = projectId;

            return Result.Success();
        }

        public Result UpdatePriority(Priority priority)
        {
            if (!Enum.IsDefined(priority))
                return Result.Failure(ErrorCode.DomainRuleViolation,"Invalid priority value.");

            this.Priority = priority;
            return Result.Success();
        }

        public Result AssignToUser(Guid userId)
        {
            if (userId == Guid.Empty)
                return Result.Failure(ErrorCode.DomainRuleViolation,"Could not identify user to assign");
            
            this.AssigneeId = userId;
            return Result.Success();
        }
        public Result Unassign()
        {
            this.AssigneeId = null;
            this.Assignee = null;
            return Result.Success();
        }
    }
}

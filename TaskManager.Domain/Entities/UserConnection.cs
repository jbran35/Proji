using TaskManager.Domain.Common;
using TaskManager.Domain.Enums;

namespace TaskManager.Domain.Entities
{
    //Class that represents an assigner/assignee relationship between two users.
    //The My Group page will pull a user's existing/active UserConnections.
    public class UserConnection : Entity
    {
        public Guid UserId { get; init; }
        public User? User { get; private set; }
        public Guid AssigneeId { get; init; }
        public User? Assignee { get; private set; }

        private UserConnection() { } // Parameterless constructor for EF

        private UserConnection(Guid userId, Guid assigneeId)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));

            if (assigneeId == Guid.Empty)
                throw new ArgumentException("Assignee ID cannot be empty.", nameof(assigneeId));

            if (userId == assigneeId)
                throw new ArgumentException("User ID and Assignee ID cannot be the same.");

            UserId = userId;
            AssigneeId = assigneeId;
        }

        public static Result<UserConnection> Create(Guid userId, Guid assigneeId)
        {
            if (userId == Guid.Empty)
                return Result<UserConnection>.Failure(ErrorCode.DomainRuleViolation, "UserID Cannot Be Empty");
            
            if(assigneeId == Guid.Empty)
                return Result<UserConnection>.Failure(ErrorCode.DomainRuleViolation, "AssigneeID Cannot Be Empty");
                
            return Result<UserConnection>.Success(new UserConnection(userId, assigneeId));
        }
    }
}

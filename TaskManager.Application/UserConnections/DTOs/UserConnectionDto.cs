namespace TaskManager.Application.UserConnections.DTOs
{
    /// <summary>
    /// A DTO object representing the connection between two users (assigner/assignee).
    /// </summary>
    public record UserConnectionDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid AssigneeId { get; set; }
        public string AssigneeName { get; set; } = string.Empty;
        public string AssigneeEmail { get; set; } = string.Empty;
    }
     
}

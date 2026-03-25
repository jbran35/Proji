namespace TaskManager.Application.UserConnections.DTOs.Requests
{
    /// <summary>
    /// A RequestDTO containing the ID of the user that the operating user wants to connect with.
    /// </summary>
    public record CreateUserConnectionRequest
    {
        public Guid AssigneeId { get; set; }
    }
}

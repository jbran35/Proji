namespace TaskManager.Application.Users.DTOs.Responses
{
    /// <summary>
    /// A response DTO for retrieving user information
    /// </summary>
    public record GetUserResponse
    {
        public Guid Id { get; init; }
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
    }
}

namespace TaskManager.Application.Users.DTOs.Responses
{
    /// <summary>
    /// Response DTO for user registration
    /// </summary>
    public record RegisterUserResponse
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
    }
}

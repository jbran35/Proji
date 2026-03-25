namespace TaskManager.Application.Users.DTOs.Responses
{
    /// <summary>
    /// A repsonse DTO for when a user updates their profile details.
    /// </summary>
    public record UpdateProfileResponse
    {
        public UserProfileDto? Profile { get; set; }
        public string? Token { get; set; }

    }
}

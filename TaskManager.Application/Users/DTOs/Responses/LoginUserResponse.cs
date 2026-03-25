namespace TaskManager.Application.Users.DTOs.Responses
{
    /// <summary>
    /// A response DTO for user login
    /// </summary>
    public record LoginUserResponse
    {
        //public Guid Id { get; set; } = Guid.Empty;
        //public string UserName { get; set; } = string.Empty;
        //public string FirstName { get; set; } = string.Empty;
        //public string LastName { get; set; } = string.Empty;
        //public string Email { get; set; } = string.Empty; 
        public required string Token { get; set; }
    }
}

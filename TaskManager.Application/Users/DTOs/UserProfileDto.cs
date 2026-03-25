namespace TaskManager.Application.Users.DTOs
{
    /// <summary>
    /// A DTO containing the details necessary to represent a user's profile.
    /// </summary>
    /// <param name="Id"></param>
    /// <param name="FirstName"></param>
    /// <param name="LastName"></param>
    /// <param name="Email"></param>
    /// <param name="UserName"></param>
    public record UserProfileDto(
        Guid Id,
        string FirstName,
        string LastName,
        string Email,
        string UserName); 
}
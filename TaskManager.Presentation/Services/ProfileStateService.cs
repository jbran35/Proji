using TaskManager.Application.Users.DTOs;

namespace TaskManager.Presentation.Services
{
    /// <summary>
    /// A service to maintain a current state of a user's profile.

    /// </summary>
    public class ProfileStateService
    {
        #region Properties
        public Guid Id { get; private set; } = Guid.Empty;
        public string FirstName { get; private set; } = string.Empty; 
        public string LastName { get; private set; } = string.Empty;
        public string Email { get; private set; } = string.Empty;
        public string UserName { get; private set; } = string.Empty;
        #endregion

        public event Action? OnProfileChanged; 

        public void SetProfile(Guid id, string firstName, string lastName, string email, string userName)
        {
            Id = id; 
            FirstName = firstName; 
            LastName = lastName;
            Email = email;
            UserName = userName;

            NotifyStateChanged();
        }

        public UserProfileDto GetProfile()
        {
            return new UserProfileDto(Id, FirstName, LastName, Email, UserName); 
        }
        private void NotifyStateChanged() => OnProfileChanged?.Invoke();
    }
}

using System.ComponentModel.DataAnnotations;

namespace TaskManager.API.DTOs.Account
{
    public class RegisterModel
    {
        [Required(ErrorMessage = "A Username is required in order to create an account")]
        public required string Username { get; set; }

        [EmailAddress]
        [Required(ErrorMessage = "An email is required in order to create an account")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "A password is required in order to create an account")]
        public required string Password { get; set; }

        [Required(ErrorMessage = "A first name is required in order to create an account")]
        public required string FirstName { get; set; }
        [Required(ErrorMessage = "A last name is required in order to create an account")]
        public required string LastName { get; set; }

    }
}

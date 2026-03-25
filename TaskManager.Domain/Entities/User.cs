using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace TaskManager.Domain.Entities
{
    public class User : IdentityUser<Guid>
    {
        [MaxLength(200)]
        public required string FirstName { get; set; }
        [MaxLength(200)]
        public required string LastName { get; set; }
        public string FullName => $"{FirstName} {LastName}";

        //Both collections needed for EF to understand relationship
        public ICollection<UserConnection> Connections { get; set; } = []; 
        public ICollection<UserConnection> ConnectedTo { get; set; } = []; 
    }
}

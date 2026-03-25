using System.ComponentModel.DataAnnotations;

namespace TaskManager.Application.Projects.DTOs.Requests
{
    /// <summary>
    /// Request DTO for creating a new Project.
    /// </summary>
    public record CreateProjectRequest
    {
        [Required(ErrorMessage = "Cannot Create Project Without a Name.")]
        public required string Title { get; set; }
        public string? Description { get; set; }
    }
}

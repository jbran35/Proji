using System.ComponentModel.DataAnnotations;

namespace TaskManager.Application.Projects.DTOs.Requests
{
    /// <summary>
    /// A request DTO for updating a project's details.
    /// </summary>
    public record UpdateProjectRequest
    {
        [Required(ErrorMessage = "Project Name is required.")]
        public required string Title { get; init; }
        public string? Description { get; init; }

    }
}

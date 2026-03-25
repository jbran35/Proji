namespace TaskManager.Application.Projects.DTOs
{
    /// <summary>
    /// A DTO with only the basic details for a project.
    /// </summary>
    public record ProjectDetailsDto
    {
        public Guid Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public string? Description { get; init; }
        public DateTime CreatedOn { get; init; }
    }
}

namespace TaskManager.Application.Projects.DTOs.Responses
{
    /// <summary>
    /// A response DTO for getting detailed view of a project (i.e., all details). 
    /// </summary>
    public record GetProjectDetailedViewResponse
    {
        public ProjectDetailedViewDto? ProjectDetails { get; init; }
    }
}

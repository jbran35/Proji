namespace TaskManager.Application.Projects.DTOs.Responses
{
    /// <summary>
    /// A Response DTO containing the Guid of a successfully deleted project & message.
    /// </summary>
    /// <param name="ProjectId"></param>
    /// <param name="Message"></param>
    public record DeleteProjectResponse(Guid ProjectId, string Message); 
    
}

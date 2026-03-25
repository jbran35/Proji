namespace TaskManager.Application.Projects.DTOs.Responses
{
    /// <summary>
    /// A record to represent the response after requesting to completing a project
    /// </summary>
    /// <param name="ProjectTile"></param>
    public record CompleteProjectResponse(ProjectTileDto ProjectTile);
        
}

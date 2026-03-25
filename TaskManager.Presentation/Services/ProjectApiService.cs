using TaskManager.Application.Projects.DTOs;
using TaskManager.Application.TodoItems.DTOs;

namespace TaskManager.Presentation.Services
{
    /// <summary>
    /// A service that contains the needed methods to interact with the API with project-related methods.
    /// </summary>
    /// <param name="apiClientService"></param>
    public class ProjectApiService(ApiClientService apiClientService)
    {
        private readonly ApiClientService _apiClientService = apiClientService;
        
        public async Task<TodoItemEntry?> UpdateTodoItemStatus(Guid todoItemId)
        {
            if (todoItemId == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(todoItemId), "Task ID Cannot Be Empty");
            }

            var client = _apiClientService.GetClient();
            var response = await client.PatchAsync($"api/todoitems/{todoItemId}/status", null)
                ?? throw new NullReferenceException("Unexpected Error Updating Task");

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException("Not authorized. Please log in.");
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                throw new Exception(errorMessage);
            }

            var result = await response.Content.ReadFromJsonAsync<TodoItemEntry>();

            return result; 
        }

        public async Task<ProjectDetailedViewDto?> GetProjectAsync(Guid projectId)
        {
            if (projectId == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(projectId), "ProjectId Cannot Be Empty");
            }

            var client = _apiClientService.GetClient();
            var response = await client.GetAsync($"api/projects/{projectId}/tasks")
                ?? throw new NullReferenceException("Unexpected Error Retrieving Project");

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException("Not authorized. Please log in.");
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                throw new Exception(errorMessage);
            }

            var result = await response.Content.ReadFromJsonAsync<ProjectDetailedViewDto>();
            return result;
        }
          
        public async Task<List<ProjectTileDto>> GetMyProjectsAsync()
        {

            var client = _apiClientService.GetClient();

            var response = await client.GetAsync("api/projects/MyProjects") ?? throw new NullReferenceException("Unexpected Error Retrieving Projects");

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException("Not authorized. Please log in.");
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                throw new Exception(errorMessage);
            }

            var result = await response.Content.ReadFromJsonAsync<List<ProjectTileDto>>();
            return result ?? []; 
        }
    }
}

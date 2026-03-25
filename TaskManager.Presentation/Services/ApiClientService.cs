namespace TaskManager.Presentation.Services
{
    public class ApiClientService(HttpClient httpClient)
    {
        private readonly HttpClient _httpClient = httpClient;

        public HttpClient GetClient()
        {
            return _httpClient;
        }
    }
}
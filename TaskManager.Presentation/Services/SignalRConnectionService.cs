using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR.Client;

namespace TaskManager.Presentation.Services
{
    public class SignalRConnectionService(AuthenticationStateProvider authStateProvider)
    {
        #region Dependency Injection & Setup
        private HubConnection? _hubConnection;
        private readonly AuthenticationStateProvider _authStateProvider = authStateProvider;
        public event Action? OnTodoItemUpdated;
        #endregion

        #region Helpers

        private HubConnectionState HubState => _hubConnection?.State ?? HubConnectionState.Disconnected;
        public bool IsConnected => HubState == HubConnectionState.Connected;

        #endregion

        #region Methods

        private async Task<string?> GetTokenAsync()
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            return user.FindFirst("jwt_token")?.Value;
        }
        public async Task InitializeConnection()
        {
            if (_hubConnection is not null) return; 

            _hubConnection = new HubConnectionBuilder()

            .WithUrl(("https://localhost:7109/taskHub"), options =>
            {
                options.AccessTokenProvider = async () => await GetTokenAsync();
            })
            .ConfigureLogging(logging => {
                logging.SetMinimumLevel(LogLevel.Information);
                logging.AddConsole();
            })
            .WithAutomaticReconnect()

            .Build();

            _hubConnection.On("TodoItemUpdated", () =>
            {
                try
                {
                    OnTodoItemUpdated?.Invoke();
                    return Task.CompletedTask;
                }
                catch (Exception exception)
                {
                    return Task.FromException(exception);
                }
            });
            while (true)
            {
                try
                {
                    await _hubConnection.StartAsync();
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error connecting: {ex.Message}");
                }
            }
        }
        #endregion
    }
}

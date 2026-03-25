using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TaskManager.API.Hubs
{
    [Authorize]
    public class TodoItemHub : Hub
    {
        public async Task SendUpdateNotification(string user)
        {
            await Clients.User(user).SendAsync("TodoItemUpdated");
        }
    }
}

using Microsoft.AspNetCore.SignalR;
using TaskManager.API.Hubs;
using TaskManager.Application.Interfaces;

namespace TaskManager.API.Services
{
    public class TodoItemUpdateNotificationService(IHubContext<TodoItemHub> hubContext) : ITodoItemUpdateNotificationService
    {
        private readonly IHubContext<TodoItemHub> _hubContext = hubContext;
        public async Task NotifyTodoItemUpdated(string assigneeId)
        {
            await _hubContext.Clients.User(assigneeId).SendAsync("TodoItemUpdated");
        }
    }
}

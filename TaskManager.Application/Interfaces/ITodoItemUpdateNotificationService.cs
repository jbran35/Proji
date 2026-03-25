namespace TaskManager.Application.Interfaces
{
    public interface ITodoItemUpdateNotificationService
    {
        /// <summary>
        /// Used so that if User A makes changes to a shared item/view, those updates are immediately reflected on User B's end.
        /// </summary>
        /// <param name="id"></param>
        Task NotifyTodoItemUpdated(string id);
    }
}

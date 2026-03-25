using Microsoft.AspNetCore.Components;
using TaskManager.Application.TodoItems.DTOs;
using TaskManager.Domain.Enums;
using TaskManager.Presentation.Services;

namespace TaskManager.Presentation.Components.Pages
{
    public partial class TodoItemDetailedView : AppComponentBase
    {
        #region Injections & Parameters
        [Inject] protected ApiClientService ApiClientService { get; set; } = default!;
        [Parameter] public required TodoItemEntry TodoItem { get; set; }
        [Parameter] public Guid ProjectId { get; set; }
        #endregion

        #region Properties
        private string Title { get; set; } = string.Empty;
        private string? Description { get; set; } = string.Empty;
        private string? Assignee { get; set; } = string.Empty;
        private DateTime? DueDate { get; set; }
        private Status Status { get; set; }
        private string ProjectName { get; set; } = string.Empty;
        private Priority? Priority { get; set; }
        #endregion
        
        protected async override Task OnInitializedAsync()
        {
            Title = TodoItem.Title;
            Description = TodoItem.Description;
            Assignee = TodoItem.AssigneeName;
            DueDate = TodoItem.DueDate;
            Priority = TodoItem.Priority;
        }

        #region Future Implmenetation Option
        //Not currently used (TodoItemEntry can reasonably contain all needed info), but if the TodoItem Entity is
        //expanded to include more data than is reasonable to load in TodoItemEntry, the GetTodoItemDetailedView endpoint 
        //& GetTodoItemDetailedView Query can be used for retrieval.
        //private async Task LoadTask()
        //{
        //    try
        //    {

        //       query.ProjectId = ProjectId; 
        //       query.TodoItemId = TodoItem.Id;

        //var client = ApiClientService.GetClient();
        //var response = await client.GetAsync($"api/todoitems/{ProjectId}/{TodoItemId}", query);

        //        if(response is null)
        //        {
        //            ToastService.Notify(new(ToastType.Danger, "Unexpected Error Retrieving Task"));
        //            return;
        //        }

        //        else if (!response.IsSuccessStatusCode)
        //            ToastService.Notify(new(ToastType.Danger, await response.Content.ReadAsStringAsync()));


        //        var task = await response.Content.ReadFromJsonAsync<TodoItemDetailedViewDto>();

        //        if (task is null)
        //            return;

        //        Title = task.Title ?? "-";
        //        Description = task.Description ?? "-";
        //        ProjectName = task.ProjectTitle;
        //        Assignee = task.AssigneeName ?? "-";
        //        DueDate = task.DueDate;
        //        Status = task.Status;
        //        Priority = task.Priority;
        //        .... // Added properties here

        //        StateHasChanged();

        //    }catch (Exception ex)
        //    {
        //        Message = $"Error loading task: {ex.Message}";
        //    }
        //} 
        #endregion
    }
}

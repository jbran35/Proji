using System.ComponentModel.DataAnnotations;

namespace TaskManager.Presentation.Components.Models
{
    /// <summary>
    /// Used in forms, primarily, to ensure that all required fields are present, as well as to save/restore any details filled out if the user
    /// clicks into the AddNewAssigneeModal from either AddTodoItemModal or EditTodoIemModal.
    /// </summary>
    public class ValidationModel
    {
        [Required(ErrorMessage = "Task name is required.")]
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid? AssigneeId { get; set; }
        public Domain.Enums.Priority Priority { get; set; } = Domain.Enums.Priority.None;
        public DateTime? DueDate { get; set; }
    }
}

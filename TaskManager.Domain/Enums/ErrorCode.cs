namespace TaskManager.Domain.Enums
{
    public enum ErrorCode
    {
        AlreadyExists,
        AssigneeNotFound,
        AuthError,
        ConnectionNotFound,
        DescriptionError,
        DomainRuleViolation,
        EmailError,
        Forbidden,
        None,
        ObjectDeleted,
        ProjectNotFound,
        TitleError,
        TodoItemNotFound,
        UnexpectedError,
        UserNameError,
        UserNotFound,
    }
}

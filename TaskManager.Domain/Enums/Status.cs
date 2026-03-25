namespace TaskManager.Domain.Enums
{
    public enum Status
    {
        Incomplete,
        Complete,
        Deleted, // Current implementation does not use soft delete, but the option is ready for use.
    }
}

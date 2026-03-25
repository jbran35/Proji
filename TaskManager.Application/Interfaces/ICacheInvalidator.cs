namespace TaskManager.Application.Interfaces
{
    /// <summary>
    /// Implmented by Commands to streamline Redis Key maintanence
    /// </summary>
    public interface ICacheInvalidator
    {
        Guid UserId { get; }
        string[] Keys { get; } 
    }
}

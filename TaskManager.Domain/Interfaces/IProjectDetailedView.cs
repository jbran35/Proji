namespace TaskManager.Domain.Interfaces
{
    public interface IProjectDetailedView : IProjectTile
    {
        //Allows the Project Repository to return only the needed information (i.e., if tasks were expanded, only 
        //that which is needed to present a project's task list is sent to the query handlers)
        public IEnumerable<ITodoItemEntry> TodoItems { get; }
    }
}

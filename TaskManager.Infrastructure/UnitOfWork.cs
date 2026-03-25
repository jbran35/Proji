using TaskManager.Domain.Interfaces;
using TaskManager.Infrastructure.Repositories;

namespace TaskManager.Infrastructure
{
    /// <summary>
    /// Links UnitOfWork to the existing repositories, so any CRUD operations can be done more safely, as well as 
    /// simplifying handler set up.
    /// </summary>
    /// <param name="context"></param>
    public class UnitOfWork (ApplicationDbContext context, IProjectRepository projectRepository, ITodoItemRepository todoItemRepository,
        IUserConnectionRepository userConnectionRepository) : IUnitOfWork
    {
        private readonly ApplicationDbContext _context = context;
        private IProjectRepository? _projectRepository = projectRepository;
        private ITodoItemRepository? _todoItemRepository = todoItemRepository;
        private IUserConnectionRepository? _userConnectionRepository = userConnectionRepository;

        public IProjectRepository ProjectRepository =>
            _projectRepository ??= new ProjectRepository(_context);

        public ITodoItemRepository TodoItemRepository =>
            _todoItemRepository ??= new TodoItemRepository(_context);

        public IUserConnectionRepository UserConnectionRepository =>
            _userConnectionRepository ??= new UserConnectionRepository(_context);

        public void Dispose()
        {
            _context.Dispose();
            GC.SuppressFinalize(this);
        }
        public async Task<int> SaveChangesAsync(CancellationToken token)
        {
            return await _context.SaveChangesAsync(token);
        }
    }
}

using System.Linq.Expressions;

namespace Gradely.Domain.Interfaces
{
    /// <summary>
    /// A generic repository interface that defines standard CRUD operations.
    /// The type parameter T must be a class (entity).
    /// 
    /// WHY generic? — So we don't write a separate repository for every entity
    /// (CourseRepository, AssignmentRepository, etc.). One implementation handles them all.
    /// </summary>
    public interface IGenericRepository<T> where T : class
    {
        /// <summary>
        /// Get a single entity by its primary key (string version).
        /// Used for entities with string IDs (e.g. ApplicationUser from ASP.NET Identity).
        /// </summary>
        Task<T?> GetByIdAsync(string id);

        /// <summary>
        /// Get a single entity by its primary key (Guid version).
        /// Used for our custom entities (Assignment, Submission, Report) that use Guid PKs.
        /// 
        /// WHY TWO OVERLOADS?
        ///   ASP.NET Identity uses string IDs (e.g. "a1b2c3d4-...").
        ///   Our custom entities use Guid IDs (e.g. Guid.NewGuid()).
        ///   EF Core's FindAsync() can handle both, but we need typed overloads
        ///   so callers don't have to convert Guid → string manually.
        /// </summary>
        Task<T?> GetByIdAsync(Guid id);

        /// <summary>
        /// Get all entities of this type.
        /// </summary>
        Task<IEnumerable<T>> GetAllAsync();

        /// <summary>
        /// Find entities matching a condition.
        /// Example: _repo.FindAsync(u => u.Email == "test@test.com")
        /// </summary>
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Add a new entity to the database (won't save until UnitOfWork.CompleteAsync is called).
        /// </summary>
        Task AddAsync(T entity);

        /// <summary>
        /// Mark an entity as modified.
        /// </summary>
        void Update(T entity);

        /// <summary>
        /// Mark an entity for deletion.
        /// </summary>
        void Delete(T entity);
    }
}

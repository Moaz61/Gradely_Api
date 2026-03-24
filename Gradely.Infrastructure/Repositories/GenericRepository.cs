using System.Linq.Expressions;
using Gradely.Domain.Interfaces;
using Gradely.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Gradely.Infrastructure.Repositories
{
    /// <summary>
    /// Implements IGenericRepository using EF Core.
    /// 
    /// HOW IT WORKS:
    ///   - _context is the database connection (AppDbContext).
    ///   - _dbSet is the specific table for entity T (e.g., DbSet<ApplicationUser> = AspNetUsers table).
    ///   - All write operations (Add, Update, Delete) only STAGE changes in memory.
    ///     Nothing is saved to the DB until UnitOfWork.CompleteAsync() is called.
    /// 
    /// WHY generic?
    ///   Instead of writing UserRepository, CourseRepository, AssignmentRepository...
    ///   we write ONE class that works for ANY entity. Less code, less bugs.
    /// </summary>
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        // ── Dependencies ─────────────────────────────────────────────
        protected readonly AppDbContext _context;   // The full database context
        protected readonly DbSet<T> _dbSet;         // The specific table for T

        public GenericRepository(AppDbContext context)
        {
            _context = context;
            // context.Set<T>() gets the DbSet for whatever T is.
            // If T = ApplicationUser, this returns the AspNetUsers table.
            _dbSet = context.Set<T>();
        }

        // ── READ operations ──────────────────────────────────────────

        /// <summary>
        /// Find one entity by its primary key.
        /// EF Core is smart enough to know which column is the PK.
        /// </summary>
        public async Task<T?> GetByIdAsync(string id)
        {
            return await _dbSet.FindAsync(id);
        }

        /// <summary>
        /// Get every row from the table.
        /// AsNoTracking() = read-only, better performance (EF won't track changes).
        /// </summary>
        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.AsNoTracking().ToListAsync();
        }

        /// <summary>
        /// Find rows matching a condition.
        /// Expression<Func<T, bool>> = a lambda that EF Core translates to SQL WHERE clause.
        /// 
        /// Example: 
        ///   await _repo.FindAsync(u => u.Email == "test@test.com")
        ///   → SQL: SELECT * FROM AspNetUsers WHERE Email = 'test@test.com'
        /// </summary>
        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }

        // ── WRITE operations (staged, not saved yet) ─────────────────

        /// <summary>
        /// Stage a new entity for insertion.
        /// The row is NOT in the DB yet — it's only in EF's change tracker.
        /// </summary>
        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

        /// <summary>
        /// Mark entity as modified so EF knows to UPDATE it on SaveChanges.
        /// </summary>
        public void Update(T entity)
        {
            _dbSet.Update(entity);
        }

        /// <summary>
        /// Mark entity for deletion. Removed from DB on SaveChanges.
        /// </summary>
        public void Delete(T entity)
        {
            _dbSet.Remove(entity);
        }
    }
}

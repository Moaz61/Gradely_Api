using Gradely.Domain.Entities;
using Gradely.Domain.Interfaces;
using Gradely.Infrastructure.Data;
using Gradely.Infrastructure.Repositories;

namespace Gradely.Infrastructure.UnitOfWork
{
    /// <summary>
    /// Implements the Unit of Work pattern.
    /// 
    /// THE BIG IDEA:
    ///   Without UoW: Each repository calls SaveChanges independently.
    ///     → If repo A saves but repo B fails, your data is inconsistent.
    ///   With UoW: All repos share ONE DbContext, and you call CompleteAsync() ONCE.
    ///     → Either everything saves, or nothing saves. All-or-nothing.
    /// 
    /// HOW TO USE (in a service):
    ///   _unitOfWork.Users.AddAsync(newUser);       // stage the change
    ///   await _unitOfWork.CompleteAsync();          // save to DB
    /// 
    /// LATER (when you add more entities):
    ///   Just add new repository properties here:
    ///   public IGenericRepository<Course> Courses { get; private set; }
    ///   public IGenericRepository<Assignment> Assignments { get; private set; }
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        // ── Dependencies ─────────────────────────────────────────────
        private readonly AppDbContext _context;

        // ── Repository Properties ────────────────────────────────────
        // Each property gives access to a repository for a specific entity.
        // "private set" = only this class can assign the repository (set in constructor).
        public IGenericRepository<ApplicationUser> Users { get; private set; }

        // ── Constructor ──────────────────────────────────────────────
        // All repositories share the SAME _context instance.
        // This is the key — one context = one transaction.
        public UnitOfWork(AppDbContext context)
        {
            _context = context;
            Users = new GenericRepository<ApplicationUser>(context);
        }

        // ── Save all changes ─────────────────────────────────────────
        /// <summary>
        /// Commits all staged changes to the database in a single transaction.
        /// Returns the number of rows affected.
        /// 
        /// WHAT HAPPENS INTERNALLY:
        ///   1. EF Core looks at its change tracker for all Add/Update/Delete operations.
        ///   2. Generates the SQL (INSERT, UPDATE, DELETE statements).
        ///   3. Wraps them in a transaction.
        ///   4. Executes against the database.
        ///   5. If anything fails → rolls back everything.
        /// </summary>
        public async Task<int> CompleteAsync()
        {
            return await _context.SaveChangesAsync();
        }

        // ── Cleanup ──────────────────────────────────────────────────
        /// <summary>
        /// Disposes the DbContext, releasing the database connection.
        /// Called automatically when the DI scope ends (end of HTTP request).
        /// </summary>
        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

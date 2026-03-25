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
    ///   _unitOfWork.Submissions.AddAsync(newSubmission);   // stage the change
    ///   await _unitOfWork.CompleteAsync();                  // save to DB
    /// 
    /// ADDING A NEW ENTITY:
    ///   1. Add a property:  public IGenericRepository<NewEntity> NewEntities { get; private set; }
    ///   2. Initialize it:   NewEntities = new GenericRepository<NewEntity>(context);
    ///   That's it! The GenericRepository handles all CRUD automatically.
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        // ── Dependencies ─────────────────────────────────────────────
        private readonly AppDbContext _context;

        // ── Repository Properties ────────────────────────────────────
        // Each property gives access to a repository for a specific entity.
        // "private set" = only this class can assign the repository (set in constructor).
        // ALL repositories share the SAME _context — this is the key to transactional safety.

        public IGenericRepository<ApplicationUser> Users { get; private set; }

        /// <summary>
        /// Repository for managing Assignment entities.
        /// Used by AssignmentService to query assignments.
        /// </summary>
        public IGenericRepository<Assignment> Assignments { get; private set; }

        /// <summary>
        /// Repository for managing Submission entities.
        /// Used by SubmissionService to create and query submissions.
        /// </summary>
        public IGenericRepository<Submission> Submissions { get; private set; }

        /// <summary>
        /// Repository for managing Report entities (ML grading results).
        /// Used by SubmissionService to fetch reports, and later by ML webhook to create them.
        /// </summary>
        public IGenericRepository<Report> Reports { get; private set; }

        // ── Constructor ──────────────────────────────────────────────
        // All repositories share the SAME _context instance.
        // This is the key — one context = one transaction.
        //
        // WHAT HAPPENS HERE:
        //   1. DI creates ONE AppDbContext for this HTTP request
        //   2. We pass that SAME context to every GenericRepository
        //   3. When any repo calls AddAsync/Update/Delete, changes are staged in the SAME context
        //   4. CompleteAsync() saves ALL changes in one transaction
        public UnitOfWork(AppDbContext context)
        {
            _context = context;
            Users = new GenericRepository<ApplicationUser>(context);
            Assignments = new GenericRepository<Assignment>(context);
            Submissions = new GenericRepository<Submission>(context);
            Reports = new GenericRepository<Report>(context);
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

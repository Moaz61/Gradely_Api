using Gradely.Domain.Entities;

namespace Gradely.Domain.Interfaces
{
    /// <summary>
    /// Unit of Work groups multiple repository operations into a single database transaction.
    /// 
    /// HOW IT WORKS:
    ///   1. You call repository methods (Add, Update, Delete) — these queue changes in memory.
    ///   2. You call CompleteAsync() — this saves ALL changes to the DB in one transaction.
    ///   3. If anything fails, nothing is saved (all-or-nothing).
    /// 
    /// EXAMPLE:
    ///   _unitOfWork.Users.AddAsync(newUser);
    ///   await _unitOfWork.CompleteAsync();   // commits to DB
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        /// <summary>
        /// Repository for ApplicationUser entities.
        /// As you add more entities (Course, Assignment, etc.), add more properties here.
        /// </summary>
        IGenericRepository<ApplicationUser> Users { get; }

        /// <summary>
        /// Saves all pending changes to the database in a single transaction.
        /// Returns the number of rows affected.
        /// </summary>
        Task<int> CompleteAsync();
    }
}

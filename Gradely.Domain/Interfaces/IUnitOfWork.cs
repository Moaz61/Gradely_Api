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
    ///   _unitOfWork.Submissions.AddAsync(newSubmission);
    ///   await _unitOfWork.CompleteAsync();   // commits to DB
    /// 
    /// ADDING NEW ENTITIES:
    ///   For every new entity, you add a repository property here.
    ///   The implementation (UnitOfWork.cs) instantiates the GenericRepository for each one.
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        /// <summary>
        /// Repository for ApplicationUser entities.
        /// </summary>
        IGenericRepository<ApplicationUser> Users { get; }

        /// <summary>
        /// Repository for Assignment entities.
        /// Used by AssignmentService to list and retrieve assignments.
        /// 
        /// Example usage in a service:
        ///   var assignments = await _unitOfWork.Assignments.GetAllAsync();
        ///   var assignment = await _unitOfWork.Assignments.GetByIdAsync(assignmentId);
        /// </summary>
        IGenericRepository<Assignment> Assignments { get; }

        /// <summary>
        /// Repository for Submission entities.
        /// Used by SubmissionService to create and query submissions.
        /// 
        /// Example usage in a service:
        ///   await _unitOfWork.Submissions.AddAsync(newSubmission);
        ///   var mySubmissions = await _unitOfWork.Submissions.FindAsync(s => s.StudentId == studentId);
        /// </summary>
        IGenericRepository<Submission> Submissions { get; }

        /// <summary>
        /// Repository for Report entities (ML grading results).
        /// Used to store and retrieve grading reports for submissions.
        /// 
        /// Example usage in a service:
        ///   var report = await _unitOfWork.Reports.FindAsync(r => r.SubmissionId == submissionId);
        /// </summary>
        IGenericRepository<Report> Reports { get; }

        /// <summary>
        /// Saves all pending changes to the database in a single transaction.
        /// Returns the number of rows affected.
        /// </summary>
        Task<int> CompleteAsync();
    }
}


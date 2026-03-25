namespace Gradely.Domain.Interfaces
{
    /// <summary>
    /// Defines the contract for assignment-related operations.
    /// 
    /// WHERE DOES EACH PIECE LIVE? (Clean Architecture)
    ///   ┌─────────────────────────────────────────────────┐
    ///   │  Domain Layer (this file)                       │
    ///   │  → Defines WHAT operations exist (interface)    │
    ///   │  → No implementation logic here                 │
    ///   ├─────────────────────────────────────────────────┤
    ///   │  Application Layer (AssignmentService.cs)       │
    ///   │  → Defines HOW they work (implementation)       │
    ///   │  → Contains business logic, mapping, validation │
    ///   ├─────────────────────────────────────────────────┤
    ///   │  API Layer (AssignmentsController.cs)           │
    ///   │  → Defines WHO can call them (HTTP endpoints)   │
    ///   │  → Handles auth, routing, HTTP responses        │
    ///   └─────────────────────────────────────────────────┘
    /// 
    /// WHY object RETURN TYPE?
    ///   The Domain layer cannot reference the Application layer (dependency rule).
    ///   DTOs live in Application, so we use "object" here.
    ///   The actual implementation in Application returns the concrete DTO type.
    ///   
    /// WHY THE TUPLE (bool, object?, string?)?
    ///   This is the same pattern used in IAuthService:
    ///   - Succeeded: true/false — did the operation work?
    ///   - Data: the result data (DTO) if successful, null if failed
    ///   - Error: error message if failed, null if successful
    ///   This pattern avoids throwing exceptions for expected failures
    ///   (e.g. "Assignment not found" is not an exception, it's a normal case).
    /// </summary>
    public interface IAssignmentService
    {
        /// <summary>
        /// Get all assignments available to students.
        /// Returns a list of assignment DTOs.
        /// 
        /// Used by: GET /api/assignments
        /// Auth: Student role
        /// </summary>
        Task<(bool Succeeded, object? Data, string? Error)> GetAllAssignmentsAsync();

        /// <summary>
        /// Get a single assignment by its ID.
        /// Returns the assignment DTO or an error if not found.
        /// 
        /// Used by: GET /api/assignments/{id}
        /// Auth: Student role
        /// </summary>
        Task<(bool Succeeded, object? Data, string? Error)> GetAssignmentByIdAsync(Guid id);
    }
}

using Gradely.Application.DTOs.Assignments;
using Gradely.Domain.Entities;
using Gradely.Domain.Interfaces;

namespace Gradely.Application.Services
{
    /// <summary>
    /// Implements IAssignmentService — contains the business logic for assignments.
    /// 
    /// IN CLEAN ARCHITECTURE:
    ///   Domain → WHAT operations exist (IAssignmentService interface)
    ///   Application (this file) → HOW they work (implementation + mapping)
    ///   Infrastructure → WHERE data is stored (UnitOfWork + Repositories)
    ///   API → WHO can call them (AssignmentsController)
    /// 
    /// THIS SERVICE IS SIMPLE because Phase 3 only has READ operations for assignments:
    ///   - GetAllAssignmentsAsync → list all assignments
    ///   - GetAssignmentByIdAsync → get one assignment
    /// 
    /// Creating assignments (POST) is for the Teacher role (Phase 5),
    /// so we don't have Create/Update/Delete here yet.
    /// 
    /// DEPENDENCIES:
    ///   - IUnitOfWork: gives us access to the Assignments repository
    ///     to query assignment data from the database.
    /// </summary>
    public class AssignmentService : IAssignmentService
    {
        // ── Dependencies (injected via DI) ───────────────────────────
        private readonly IUnitOfWork _unitOfWork;

        public AssignmentService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // ══════════════════════════════════════════════════════════════
        //  GET ALL ASSIGNMENTS
        // ══════════════════════════════════════════════════════════════
        /// <summary>
        /// Returns all assignments as a list of DTOs.
        /// 
        /// FLOW:
        ///   1. Query all assignments from the database via UnitOfWork
        ///   2. Map each Assignment entity → AssignmentDto
        ///   3. Return the list wrapped in the success tuple
        /// 
        /// WHY MAP TO DTOs?
        ///   The Assignment entity has a Submissions navigation property.
        ///   If we returned the entity directly, JSON serialization would:
        ///   a) Try to load all submissions (lazy loading issues)
        ///   b) Hit circular references (Submission → Assignment → Submissions → ...)
        ///   c) Expose internal data the client doesn't need
        ///   Mapping to DTOs avoids all of this.
        /// </summary>
        public async Task<(bool Succeeded, object? Data, string? Error)> GetAllAssignmentsAsync()
        {
            // Step 1: Get all assignments from the database
            var assignments = await _unitOfWork.Assignments.GetAllAsync();

            // Step 2: Map entities to DTOs
            // We use LINQ's .Select() to transform each entity into a DTO.
            // This is the "mapping" step — converting internal data to API-friendly data.
            var assignmentDtos = assignments.Select(a => MapToDto(a)).ToList();

            // Step 3: Return success with the list
            return (true, assignmentDtos, null);
        }

        // ══════════════════════════════════════════════════════════════
        //  GET ASSIGNMENT BY ID
        // ══════════════════════════════════════════════════════════════
        /// <summary>
        /// Returns a single assignment by its Guid ID.
        /// 
        /// FLOW:
        ///   1. Look up the assignment by ID
        ///   2. If not found → return failure with "Assignment not found"
        ///   3. If found → map to DTO and return success
        /// 
        /// WHY NOT THROW AN EXCEPTION FOR "NOT FOUND"?
        ///   "Assignment not found" is a normal, expected case — not an error.
        ///   Exceptions should be for unexpected situations (DB connection lost, etc.).
        ///   Using the tuple pattern (false, null, "message") is:
        ///   - More performant than try/catch
        ///   - More readable (the caller doesn't need try/catch blocks)
        ///   - Consistent with the IAuthService pattern
        /// </summary>
        public async Task<(bool Succeeded, object? Data, string? Error)> GetAssignmentByIdAsync(Guid id)
        {
            // Step 1: Look up the assignment
            var assignment = await _unitOfWork.Assignments.GetByIdAsync(id);

            // Step 2: Handle not found
            if (assignment == null)
                return (false, null, "Assignment not found.");

            // Step 3: Map to DTO and return
            return (true, MapToDto(assignment), null);
        }

        // ══════════════════════════════════════════════════════════════
        //  PRIVATE HELPER
        // ══════════════════════════════════════════════════════════════
        /// <summary>
        /// Maps an Assignment entity to an AssignmentDto.
        /// 
        /// WHY A PRIVATE METHOD?
        ///   Both GetAll and GetById need the same mapping logic.
        ///   Extracting it to a helper avoids code duplication.
        ///   If we add a field to AssignmentDto later, we only change it here.
        /// 
        /// NOTE: In larger projects, you'd use AutoMapper or Mapster for this.
        /// For a graduation project, manual mapping is simpler and clearer.
        /// </summary>
        private static AssignmentDto MapToDto(Assignment assignment)
        {
            return new AssignmentDto
            {
                Id = assignment.Id,
                Title = assignment.Title,
                Description = assignment.Description,
                DueDate = assignment.DueDate,
                MaxGrade = assignment.MaxGrade,
                CreatedAt = assignment.CreatedAt
            };
        }
    }
}

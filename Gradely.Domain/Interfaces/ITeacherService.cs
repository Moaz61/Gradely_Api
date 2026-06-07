namespace Gradely.Domain.Interfaces
{
    /// <summary>
    /// Operations exposed to the Teacher role (Phase 5).
    ///
    /// Same (Succeeded, Data, Error) tuple pattern used by the other
    /// services. The DTO types live in the Application layer, so this
    /// interface returns "object" — the implementation returns the
    /// concrete DTO and the controller passes it straight through.
    ///
    /// NOTE on scoping:
    ///   The Assignment entity does not have a TeacherId field, so we
    ///   cannot scope these queries to the assignments a specific teacher
    ///   created. Every teacher sees every assignment. If we add an
    ///   ownership column later, change these signatures to take a teacherId.
    /// </summary>
    public interface ITeacherService
    {
        Task<(bool Succeeded, object? Data, string? Error)> GetAllAssignmentsAsync();

        Task<(bool Succeeded, object? Data, string? Error)> GetSubmissionsForAssignmentAsync(Guid assignmentId);

        Task<(bool Succeeded, object? Data, string? Error)> GetSubmissionReportAsync(Guid submissionId);

        Task<(bool Succeeded, object? Data, string? Error)> GetStatsAsync();

        /// <summary>
        /// Create a new assignment.
        /// 
        /// Used by: POST /api/teacher/assignments
        /// Auth: Teacher role
        /// </summary>
        Task<(bool Succeeded, object? Data, string? Error)> CreateAssignmentAsync(object dto, string teacherId);

        /// <summary>
        /// Update an existing assignment by ID.
        /// 
        /// Used by: PUT /api/teacher/assignments/{id}
        /// Auth: Teacher role
        /// </summary>
        Task<(bool Succeeded, object? Data, string? Error)> UpdateAssignmentAsync(Guid id, object dto);

        /// <summary>
        /// Delete an assignment by ID.
        /// Prevents deletion if the assignment has submissions.
        /// 
        /// Used by: DELETE /api/teacher/assignments/{id}
        /// Auth: Teacher role
        /// </summary>
        Task<(bool Succeeded, object? Data, string? Error)> DeleteAssignmentAsync(Guid id);
    }
}

namespace Gradely.Domain.Interfaces
{
    /// <summary>
    /// Operations exposed to the Teacher role.
    ///
    /// Same (Succeeded, Data, Error) tuple pattern used by the other
    /// services. The DTO types live in the Application layer, so this
    /// interface returns "object" — the implementation returns the
    /// concrete DTO and the controller passes it straight through.
    ///
    /// ENDPOINTS COVERED:
    ///   GET    /api/teacher/assignments                          → list teacher's assignments
    ///   POST   /api/teacher/assignments                         → create assignment
    ///   GET    /api/teacher/assignments/{id}                    → single assignment detail
    ///   PUT    /api/teacher/assignments/{id}                    → update assignment
    ///   DELETE /api/teacher/assignments/{id}                    → delete assignment (cascade)
    ///   GET    /api/teacher/assignments/{id}/submissions        → submissions for assignment
    ///   GET    /api/teacher/assignments/{id}/student-status     → student submission status grid
    ///   GET    /api/teacher/submissions/{id}                    → single submission metadata
    ///   GET    /api/teacher/submissions/{id}/file               → file path for download
    ///   GET    /api/teacher/submissions/{id}/report             → submission grading report
    ///   GET    /api/teacher/students                            → list assigned students
    ///   GET    /api/teacher/submissions/student/{studentId}     → all submissions by one student
    ///   GET    /api/teacher/stats                               → grade statistics
    /// </summary>
    public interface ITeacherService
    {
        // ── Assignment management ──────────────────────────────────────

        Task<(bool Succeeded, object? Data, string? Error)> GetAllAssignmentsAsync(string teacherId);

        Task<(bool Succeeded, object? Data, string? Error)> GetAssignmentByIdAsync(Guid assignmentId, string teacherId);

        Task<(bool Succeeded, object? Data, string? Error)> CreateAssignmentAsync(object dto, string teacherId);

        Task<(bool Succeeded, object? Data, string? Error)> UpdateAssignmentAsync(Guid id, object dto, string teacherId);

        /// <summary>
        /// Deletes an assignment and cascade-deletes all its submissions and reports.
        /// </summary>
        Task<(bool Succeeded, object? Data, string? Error)> DeleteAssignmentAsync(Guid id, string teacherId);

        // ── Submission views ───────────────────────────────────────────

        Task<(bool Succeeded, object? Data, string? Error)> GetSubmissionsForAssignmentAsync(Guid assignmentId, string teacherId);

        Task<(bool Succeeded, object? Data, string? Error)> GetAssignmentStudentStatusAsync(Guid assignmentId, string teacherId);

        Task<(bool Succeeded, object? Data, string? Error)> GetSubmissionByIdAsync(Guid submissionId, string teacherId);

        /// <summary>
        /// Returns the disk file path for a student's submitted PDF so the
        /// controller can stream it back to the teacher.
        /// </summary>
        Task<(bool Succeeded, string? FilePath, string? OriginalFileName, string? Error)> GetSubmissionFilePathAsync(Guid submissionId, string teacherId);

        Task<(bool Succeeded, object? Data, string? Error)> GetSubmissionReportAsync(Guid submissionId, string teacherId);

        // ── Student views ──────────────────────────────────────────────

        Task<(bool Succeeded, object? Data, string? Error)> GetStudentsAsync(string teacherId);

        Task<(bool Succeeded, object? Data, string? Error)> GetStudentSubmissionsAsync(string studentId, string teacherId);

        // ── Statistics ────────────────────────────────────────────────

        Task<(bool Succeeded, object? Data, string? Error)> GetStatsAsync(string teacherId);
    }
}

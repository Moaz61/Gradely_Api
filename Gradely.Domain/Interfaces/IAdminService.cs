namespace Gradely.Domain.Interfaces
{
    /// <summary>
    /// Operations exposed to the Admin role.
    ///
    /// Same (Succeeded, Data, Error) tuple pattern used by the other
    /// services. The DTO types live in the Application layer, so this
    /// interface returns "object" — the implementation returns the
    /// concrete DTO and the controller passes it straight through.
    ///
    /// ENDPOINTS COVERED:
    ///   GET    /api/admin/users                    → list all users
    ///   DELETE /api/admin/students/{id}            → delete student (cascade)
    ///   DELETE /api/admin/teachers/{id}            → delete teacher (cascade + unassign)
    ///   PUT    /api/admin/teachers/{id}/verify     → mark teacher as verified
    ///   PUT    /api/admin/users/{id}/assign-teacher → assign/remove teacher from student
    ///   GET    /api/admin/assignments              → view all assignments
    ///   GET    /api/admin/stats                    → system-wide statistics
    /// </summary>
    public interface IAdminService
    {
        /// <summary>
        /// List all non-admin users in the system with their roles.
        /// </summary>
        Task<(bool Succeeded, object? Data, string? Error)> GetAllUsersAsync();

        /// <summary>
        /// Delete a Student account.
        /// Programmatically deletes their submissions and reports before deleting the user.
        /// </summary>
        Task<(bool Succeeded, string? Error)> DeleteStudentAsync(string studentId);

        /// <summary>
        /// Delete a Teacher account.
        /// Unassigns all students (sets their TeacherId = null),
        /// deletes all assignments the teacher created (cascading to submissions and reports),
        /// then deletes the teacher user record.
        /// </summary>
        Task<(bool Succeeded, string? Error)> DeleteTeacherAsync(string teacherId);

        /// <summary>
        /// Mark a teacher account as verified (IsVerified = true).
        /// Sets IsVerified = true on the teacher's ApplicationUser record.
        /// </summary>
        Task<(bool Succeeded, object? Data, string? Error)> VerifyTeacherAsync(string teacherId);

        /// <summary>
        /// Assign a verified teacher to a student.
        /// Pass null teacherId to remove the assignment.
        /// </summary>
        Task<(bool Succeeded, object? Data, string? Error)> AssignTeacherAsync(string studentId, string? teacherId);

        /// <summary>
        /// Get all assignments across the entire platform (with teacher names).
        /// </summary>
        Task<(bool Succeeded, object? Data, string? Error)> GetAllAssignmentsAsync();

        /// <summary>
        /// Get system-wide statistics including chart data:
        /// user counts, submission counts, average grades,
        /// weekly submissions, grade distribution, monthly user growth.
        /// </summary>
        Task<(bool Succeeded, object? Data, string? Error)> GetSystemStatsAsync();
    }
}

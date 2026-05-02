namespace Gradely.Domain.Interfaces
{
    /// <summary>
    /// Operations exposed to the Admin role (Phase 6).
    ///
    /// Same (Succeeded, Data, Error) tuple pattern used by the other
    /// services. The DTO types live in the Application layer, so this
    /// interface returns "object" — the implementation returns the
    /// concrete DTO and the controller passes it straight through.
    ///
    /// ENDPOINTS COVERED:
    ///   GET    /api/admin/users                → list all users
    ///   POST   /api/admin/teachers             → create a teacher account
    ///   DELETE /api/admin/teachers/{id}        → remove a teacher
    ///   PUT    /api/admin/teachers/{id}/verify → mark teacher as verified
    ///   GET    /api/admin/stats                → system-wide statistics
    /// </summary>
    public interface IAdminService
    {
        /// <summary>
        /// List all users in the system with their roles.
        /// </summary>
        Task<(bool Succeeded, object? Data, string? Error)> GetAllUsersAsync();

        /// <summary>
        /// Create a new user with the Teacher role.
        /// </summary>
        Task<(bool Succeeded, object? Data, string? Error)> CreateTeacherAsync(object createTeacherDto);

        /// <summary>
        /// Delete an existing teacher account by ID.
        /// Only users with the Teacher role can be deleted through this endpoint.
        /// </summary>
        Task<(bool Succeeded, string? Error)> DeleteTeacherAsync(string teacherId);

        /// <summary>
        /// Mark a teacher account as verified.
        /// Sets IsVerified = true on the teacher's ApplicationUser record.
        /// </summary>
        Task<(bool Succeeded, object? Data, string? Error)> VerifyTeacherAsync(string teacherId);

        /// <summary>
        /// Get system-wide statistics (user counts, submission counts, grades).
        /// </summary>
        Task<(bool Succeeded, object? Data, string? Error)> GetSystemStatsAsync();
    }
}

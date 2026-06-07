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
    /// NOTE: Teachers now self-register via POST /api/auth/register.
    ///       The admin's role is to VERIFY teacher accounts, not create them.
    ///
    /// ENDPOINTS COVERED:
    ///   GET    /api/admin/users                → list all users
    ///   DELETE /api/admin/users/{id}           → remove a teacher or student
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
        /// Delete an existing user account by ID.
        /// Admin accounts cannot be deleted through this endpoint.
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

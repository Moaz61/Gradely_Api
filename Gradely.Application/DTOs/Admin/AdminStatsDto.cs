namespace Gradely.Application.DTOs.Admin
{
    /// <summary>
    /// System-wide statistics returned by GET /api/admin/stats.
    ///
    /// Gives the admin a bird's-eye view of the entire system:
    /// user counts by role, submission counts, and average grades.
    ///
    /// EXAMPLE JSON:
    ///   {
    ///     "totalUsers": 42,
    ///     "totalStudents": 35,
    ///     "totalTeachers": 6,
    ///     "totalAdmins": 1,
    ///     "verifiedTeachers": 4,
    ///     "totalAssignments": 10,
    ///     "totalSubmissions": 120,
    ///     "gradedSubmissions": 95,
    ///     "pendingSubmissions": 25,
    ///     "overallAverageGrade": 78.5
    ///   }
    /// </summary>
    public class AdminStatsDto
    {
        // ── User counts ──
        public int TotalUsers { get; set; }
        public int TotalStudents { get; set; }
        public int TotalTeachers { get; set; }
        public int TotalAdmins { get; set; }
        public int VerifiedTeachers { get; set; }

        // ── Assignment & Submission counts ──
        public int TotalAssignments { get; set; }
        public int TotalSubmissions { get; set; }
        public int GradedSubmissions { get; set; }
        public int PendingSubmissions { get; set; }

        /// <summary>
        /// Overall average grade as a percentage (0–100) across all
        /// graded submissions. Null if there are no graded submissions.
        /// </summary>
        public double? OverallAverageGrade { get; set; }
    }
}

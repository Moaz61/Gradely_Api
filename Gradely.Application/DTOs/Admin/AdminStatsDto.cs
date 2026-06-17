using Gradely.Application.DTOs.Teacher;

namespace Gradely.Application.DTOs.Admin
{
    /// <summary>
    /// System-wide statistics returned by GET /api/admin/stats.
    ///
    /// Gives the admin a bird's-eye view of the entire system:
    /// user counts by role, submission counts, average grades,
    /// and chart data (weekly submissions, grade distribution, monthly growth).
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

        // ── Chart data ───────────────────────────────────────────────

        /// <summary>
        /// Number of submissions received per day for the last 7 days.
        /// Index 0 = oldest day, Index 6 = today.
        /// Used for the weekly submissions bar/line chart.
        /// </summary>
        public List<int> WeeklySubmissions { get; set; } = new();

        /// <summary>
        /// Grade distribution buckets across all graded submissions system-wide.
        /// Reuses GradeDistributionDto from Gradely.Application.DTOs.Teacher.
        /// </summary>
        public GradeDistributionDto GradeDistribution { get; set; } = new();

        /// <summary>
        /// Number of new user registrations per month for the last 6 months.
        /// Index 0 = oldest month, Index 5 = current month.
        /// Used for the monthly growth line chart.
        /// </summary>
        public List<int> MonthlyUserGrowth { get; set; } = new();
    }
}

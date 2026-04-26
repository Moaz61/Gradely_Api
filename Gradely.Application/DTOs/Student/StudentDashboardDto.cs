namespace Gradely.Application.DTOs.Student
{
    /// <summary>
    /// Dashboard summary returned by GET /api/student/dashboard.
    ///
    /// AverageGrade is a percentage (0–100) computed across the student's
    /// graded submissions only. It is null when the student has no graded
    /// submissions yet (instead of returning 0, which would be misleading).
    /// </summary>
    public class StudentDashboardDto
    {
        public int TotalAssignments { get; set; }
        public int SubmittedCount { get; set; }
        public int PendingCount { get; set; }

        /// <summary>Average grade as a percentage (0–100), or null if none graded.</summary>
        public double? AverageGrade { get; set; }
    }
}

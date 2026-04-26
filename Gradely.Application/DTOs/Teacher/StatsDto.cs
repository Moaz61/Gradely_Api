namespace Gradely.Application.DTOs.Teacher
{
    /// <summary>
    /// Aggregate stats returned by GET /api/teacher/stats.
    ///
    /// SHAPE:
    ///   - PerAssignment: average grade for each assignment
    ///   - GradeDistribution: how many graded submissions fall into each bucket
    ///   - Totals: high-level counts
    ///
    /// Only GRADED submissions are included in the average/distribution.
    /// Ungraded submissions are counted separately in PendingCount.
    /// </summary>
    public class StatsDto
    {
        public int TotalAssignments { get; set; }
        public int TotalSubmissions { get; set; }
        public int GradedCount { get; set; }
        public int PendingCount { get; set; }

        /// <summary>
        /// Overall average grade as a percentage (0–100), across all
        /// graded submissions. Null if there are no graded submissions.
        /// </summary>
        public double? OverallAveragePercent { get; set; }

        public List<AssignmentStatsDto> PerAssignment { get; set; } = new();
        public GradeDistributionDto GradeDistribution { get; set; } = new();
    }

    /// <summary>Per-assignment averages.</summary>
    public class AssignmentStatsDto
    {
        public Guid AssignmentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int MaxGrade { get; set; }
        public int SubmissionCount { get; set; }
        public int GradedCount { get; set; }

        /// <summary>Average grade as a percentage (0–100). Null if none graded yet.</summary>
        public double? AveragePercent { get; set; }
    }

    /// <summary>
    /// Buckets graded submissions by percentage score.
    /// Boundaries follow a typical letter-grade split.
    /// </summary>
    public class GradeDistributionDto
    {
        public int Below60 { get; set; }
        public int From60To69 { get; set; }
        public int From70To79 { get; set; }
        public int From80To89 { get; set; }
        public int From90To100 { get; set; }
    }
}

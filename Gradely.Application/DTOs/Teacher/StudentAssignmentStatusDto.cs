namespace Gradely.Application.DTOs.Teacher
{
    /// <summary>
    /// Represents the submission status of one student for a specific assignment.
    /// Returned by GET /api/teacher/assignments/{id}/student-status.
    ///
    /// Shows every student assigned to the teacher, and whether they have
    /// submitted for the given assignment (and their grade if graded).
    ///
    /// EXAMPLE JSON:
    ///   {
    ///     "studentId": "abc123",
    ///     "studentName": "Moaaz Ahmed",
    ///     "studentEmail": "moaaz@student.com",
    ///     "hasSubmitted": true,
    ///     "submittedAt": "2026-05-01T10:30:00Z",
    ///     "grade": 85,
    ///     "status": "Graded"
    ///   }
    /// </summary>
    public class StudentAssignmentStatusDto
    {
        /// <summary>The student's unique Identity ID.</summary>
        public string StudentId { get; set; } = string.Empty;

        /// <summary>The student's display name.</summary>
        public string StudentName { get; set; } = string.Empty;

        /// <summary>The student's email address.</summary>
        public string StudentEmail { get; set; } = string.Empty;

        /// <summary>True if the student has submitted for this assignment.</summary>
        public bool HasSubmitted { get; set; }

        /// <summary>When the student submitted (null if not submitted).</summary>
        public DateTime? SubmittedAt { get; set; }

        /// <summary>Grade assigned by ML system (null if not submitted or not graded yet).</summary>
        public int? Grade { get; set; }

        /// <summary>Submission status string (e.g. "Submitted", "Graded", or "Pending").</summary>
        public string Status { get; set; } = "Pending";
    }
}

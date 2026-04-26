namespace Gradely.Application.DTOs.Teacher
{
    /// <summary>
    /// Submission view for the teacher.
    ///
    /// Same as the student SubmissionDto, but ALSO includes the student's
    /// name + email and the grade (when graded). Teachers need to know
    /// which student submitted what.
    /// </summary>
    public class TeacherSubmissionDto
    {
        public Guid Id { get; set; }

        public Guid AssignmentId { get; set; }
        public string AssignmentTitle { get; set; } = string.Empty;

        // Student info — the teacher needs to see who submitted.
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;

        public string OriginalFileName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }

        /// <summary>Grade if graded, null otherwise.</summary>
        public int? Grade { get; set; }
    }
}

namespace Gradely.Application.DTOs.Submissions
{
    /// <summary>
    /// Response DTO for returning submission data to the student.
    /// 
    /// WHAT THE CLIENT SEES:
    ///   When a student calls GET /api/submissions or GET /api/submissions/{id},
    ///   they get back this DTO — NOT the raw Submission entity.
    /// 
    /// WHY INCLUDE AssignmentTitle?
    ///   The raw Submission entity only has AssignmentId (a Guid).
    ///   That's useless to the student — they want to see "Essay on Climate Change",
    ///   not "3f2504e0-4f89-11d3-9a0c-0305e82c3301".
    ///   So in the service, we JOIN with the Assignment and include the title.
    ///   This is a key benefit of DTOs — we can combine data from multiple entities.
    /// 
    /// WHY Status IS A STRING?
    ///   The entity stores Status as an enum (int in DB: 0 or 1).
    ///   For the API response, we convert it to a readable string ("Submitted" or "Graded")
    ///   so the frontend doesn't need to know what 0 and 1 mean.
    /// 
    /// EXAMPLE JSON returned to the client:
    ///   {
    ///     "id": "a1b2c3d4-...",
    ///     "assignmentId": "3f2504e0-...",
    ///     "assignmentTitle": "Essay on Climate Change",
    ///     "originalFileName": "my-essay.pdf",
    ///     "status": "Submitted",
    ///     "submittedAt": "2026-03-24T14:30:00Z"
    ///   }
    /// </summary>
    public class SubmissionDto
    {
        /// <summary>The submission's unique identifier.</summary>
        public Guid Id { get; set; }

        /// <summary>The ID of the assignment this submission is for.</summary>
        public Guid AssignmentId { get; set; }

        /// <summary>
        /// The assignment's title (e.g. "Essay on Climate Change").
        /// Included for display purposes — saves the client a separate API call.
        /// </summary>
        public string AssignmentTitle { get; set; } = string.Empty;

        /// <summary>
        /// The original filename the student uploaded (e.g. "my-essay.pdf").
        /// We show this instead of the server-side filename (which is a GUID).
        /// </summary>
        public string OriginalFileName { get; set; } = string.Empty;

        /// <summary>
        /// Current status as a readable string: "Submitted" or "Graded".
        /// Converted from the enum in the service layer.
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>When the student submitted the file (UTC).</summary>
        public DateTime SubmittedAt { get; set; }
    }
}

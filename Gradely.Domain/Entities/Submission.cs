using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Gradely.Domain.Enums;

namespace Gradely.Domain.Entities
{
    /// <summary>
    /// Represents a student's submission for an assignment.
    /// 
    /// IN THE GRADELY FLOW:
    ///   1. Student selects an assignment and uploads a PDF file
    ///   2. A Submission record is created with Status = Submitted
    ///   3. The ML system processes the PDF and creates a Report
    ///   4. Submission.Status changes to Graded
    ///   5. Student can view their report
    /// 
    /// RELATIONSHIPS:
    ///   Submission → Assignment  (many-to-one): Many students submit to one assignment
    ///   Submission → Student     (many-to-one): One student can have many submissions
    ///   Submission → Report      (one-to-one):  Each submission gets one grading report
    /// 
    /// WHY TWO ID TYPES?
    ///   - AssignmentId is Guid (our custom entity uses Guid PKs)
    ///   - StudentId is string (ASP.NET Identity uses string IDs by default)
    ///   This is normal in apps that mix Identity with custom entities.
    /// </summary>
    public class Submission
    {
        /// <summary>
        /// Primary key — auto-generated Guid.
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        // ── Foreign Keys ─────────────────────────────────────────────

        /// <summary>
        /// FK to the Assignment this submission is for.
        /// [ForeignKey] attribute tells EF Core: "This property points to Assignment.Id"
        /// and auto-creates the FK constraint in the database.
        /// </summary>
        [Required]
        public Guid AssignmentId { get; set; }

        /// <summary>
        /// FK to the student (ApplicationUser) who submitted.
        /// It's a string because ASP.NET Identity uses string IDs (GUIDs stored as strings).
        /// Nullable because if a student is deleted, their submissions are retained (SetNull behavior).
        /// </summary>
        public string? StudentId { get; set; }

        // ── File Info ────────────────────────────────────────────────

        /// <summary>
        /// The relative path where the uploaded PDF is stored.
        /// Example: "uploads/3f2504e0-student-essay.pdf"
        /// We store the relative path, not the full server path,
        /// so the app works if you move it to a different server.
        /// </summary>
        [Required]
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// The original filename the student uploaded (e.g. "my-essay.pdf").
        /// We keep this for display purposes — the actual file is renamed to avoid conflicts.
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string OriginalFileName { get; set; } = string.Empty;

        // ── Status ───────────────────────────────────────────────────

        /// <summary>
        /// Current status of the submission (Submitted or Graded).
        /// EF Core stores this as an integer in the database (0 or 1)
        /// because SubmissionStatus is an enum.
        /// </summary>
        public SubmissionStatus Status { get; set; } = SubmissionStatus.Submitted;

        /// <summary>
        /// When the student submitted the file.
        /// </summary>
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        // ── Navigation Properties ────────────────────────────────────
        // These let EF Core understand the relationships and allow eager loading.
        // Example: _context.Submissions.Include(s => s.Assignment).ToListAsync()

        /// <summary>
        /// The assignment this submission belongs to.
        /// [ForeignKey] links this navigation to the AssignmentId property above.
        /// </summary>
        [ForeignKey(nameof(AssignmentId))]
        public Assignment? Assignment { get; set; }

        /// <summary>
        /// The student who submitted this.
        /// [ForeignKey] links this navigation to the StudentId property above.
        /// </summary>
        [ForeignKey(nameof(StudentId))]
        public ApplicationUser? Student { get; set; }

        /// <summary>
        /// The grading report for this submission (one-to-one).
        /// Will be null until the ML system grades the submission.
        /// </summary>
        public Report? Report { get; set; }
    }
}

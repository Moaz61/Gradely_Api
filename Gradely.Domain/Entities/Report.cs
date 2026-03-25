using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gradely.Domain.Entities
{
    /// <summary>
    /// Stores the ML-generated grading report for a submission.
    /// 
    /// IN THE GRADELY FLOW:
    ///   1. Student uploads PDF → Submission created (Status = Submitted)
    ///   2. ML system analyzes the PDF
    ///   3. ML system sends results to POST /api/submissions/{id}/report (Phase 4)
    ///   4. This Report record is created with grade, feedback, and detailed mistakes
    ///   5. Submission.Status changes to Graded
    ///   6. Student can view this report via GET /api/submissions/{id}/report
    /// 
    /// WHY MistakesJson IS A STRING?
    ///   The ML system returns mistakes as a JSON array:
    ///   [{ "type": "grammar", "description": "Subject-verb agreement", "line": 3 }]
    ///   
    ///   We store this as a raw JSON string in the database because:
    ///   1. The structure might change as the ML model evolves
    ///   2. We don't need to query individual mistakes via SQL
    ///   3. It's simple — deserialize when needed, no extra tables
    ///   
    ///   SQL Server has a NVARCHAR(MAX) column for this,
    ///   which can hold up to ~1GB of text (more than enough).
    /// 
    /// RELATIONSHIP:
    ///   Report → Submission (one-to-one): Each submission gets exactly one report.
    /// </summary>
    public class Report
    {
        /// <summary>
        /// Primary key — auto-generated Guid.
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// FK to the Submission this report grades.
        /// One-to-one relationship: one report per submission.
        /// </summary>
        [Required]
        public Guid SubmissionId { get; set; }

        /// <summary>
        /// The numerical grade assigned by the ML system (e.g. 87 out of 100).
        /// Should be between 0 and Assignment.MaxGrade.
        /// </summary>
        public int Grade { get; set; }

        /// <summary>
        /// Overall text feedback from the ML system.
        /// Example: "Good work overall. Strong thesis statement but some grammar issues."
        /// </summary>
        public string Feedback { get; set; } = string.Empty;

        /// <summary>
        /// JSON string containing a detailed array of mistakes.
        /// 
        /// Example:
        /// [
        ///   { "type": "grammar", "description": "Subject-verb agreement", "line": 3 },
        ///   { "type": "spelling", "description": "Misspelled 'receive'", "line": 7 }
        /// ]
        /// 
        /// Stored as raw JSON because:
        ///   - The ML model might change the structure over time
        ///   - We don't need to SQL-query individual mistakes
        ///   - Simple to deserialize in C# with JsonSerializer.Deserialize
        /// </summary>
        public string MistakesJson { get; set; } = "[]";

        /// <summary>
        /// When the ML system generated this report.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ── Navigation Property ──────────────────────────────────────

        /// <summary>
        /// The submission this report belongs to.
        /// [ForeignKey] tells EF Core to use SubmissionId as the FK column.
        /// </summary>
        [ForeignKey(nameof(SubmissionId))]
        public Submission? Submission { get; set; }
    }
}

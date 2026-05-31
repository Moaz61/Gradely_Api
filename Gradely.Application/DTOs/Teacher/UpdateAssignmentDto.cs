using System.ComponentModel.DataAnnotations;

namespace Gradely.Application.DTOs.Teacher
{
    /// <summary>
    /// Request DTO for updating an existing assignment.
    /// 
    /// Sent by: Teacher via PUT /api/teacher/assignments/{id}
    /// 
    /// EXAMPLE JSON the teacher sends:
    ///   {
    ///     "title": "Updated Essay Title",
    ///     "description": "Updated instructions...",
    ///     "dueDate": "2026-08-01T23:59:59Z",
    ///     "maxGrade": 150
    ///   }
    /// 
    /// NOTE: All fields are included — this is a full update (PUT), not a partial update (PATCH).
    ///       The teacher sends all fields, and they all get overwritten.
    /// </summary>
    public class UpdateAssignmentDto
    {
        /// <summary>The updated assignment title. Required, max 200 chars.</summary>
        [Required(ErrorMessage = "Title is required.")]
        [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
        public string Title { get; set; } = string.Empty;

        /// <summary>Updated instructions for the assignment (optional).</summary>
        public string? Description { get; set; }

        /// <summary>Updated due date (UTC). Required.</summary>
        [Required(ErrorMessage = "Due date is required.")]
        public DateTime DueDate { get; set; }

        /// <summary>Updated maximum possible grade. Must be between 1 and 1000.</summary>
        [Range(1, 1000, ErrorMessage = "Max grade must be between 1 and 1000.")]
        public int MaxGrade { get; set; } = 100;
    }
}

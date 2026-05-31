using System.ComponentModel.DataAnnotations;

namespace Gradely.Application.DTOs.Teacher
{
    /// <summary>
    /// Request DTO for creating a new assignment.
    /// 
    /// Sent by: Teacher via POST /api/teacher/assignments
    /// 
    /// EXAMPLE JSON the teacher sends:
    ///   {
    ///     "title": "Essay on Climate Change",
    ///     "description": "Write a 500-word essay about the effects of climate change.",
    ///     "dueDate": "2026-07-15T23:59:59Z",
    ///     "maxGrade": 100
    ///   }
    /// 
    /// VALIDATION:
    ///   [Required] and [MaxLength] are Data Annotations — ASP.NET Core
    ///   validates these BEFORE the controller action even runs.
    ///   If validation fails → 400 Bad Request with error details.
    /// </summary>
    public class CreateAssignmentDto
    {
        /// <summary>The assignment title (e.g. "Essay on Climate Change"). Required, max 200 chars.</summary>
        [Required(ErrorMessage = "Title is required.")]
        [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
        public string Title { get; set; } = string.Empty;

        /// <summary>Detailed instructions for the assignment (optional).</summary>
        public string? Description { get; set; }

        /// <summary>When the assignment is due (UTC). Required.</summary>
        [Required(ErrorMessage = "Due date is required.")]
        public DateTime DueDate { get; set; }

        /// <summary>Maximum possible grade (e.g. 100). Defaults to 100 if not provided.</summary>
        [Range(1, 1000, ErrorMessage = "Max grade must be between 1 and 1000.")]
        public int MaxGrade { get; set; } = 100;
    }
}

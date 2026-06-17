using System.ComponentModel.DataAnnotations;

namespace Gradely.Application.DTOs.Admin
{
    /// <summary>
    /// Request body for PUT /api/admin/users/{studentId}/assign-teacher.
    /// Assigns (or removes) a teacher from a student account.
    ///
    /// To ASSIGN a teacher: provide TeacherId = "some-teacher-id"
    /// To REMOVE a teacher: provide TeacherId = null
    /// </summary>
    public class AssignTeacherDto
    {
        /// <summary>
        /// The ID of the teacher to assign to the student.
        /// Set to null to unassign the current teacher.
        /// </summary>
        public string? TeacherId { get; set; }
    }
}

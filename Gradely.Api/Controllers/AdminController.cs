using Gradely.Application.DTOs.Admin;
using Gradely.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gradely.Api.Controllers
{
    /// <summary>
    /// Admin API Controller — manages users, assignments, and system statistics.
    ///
    /// ENDPOINTS:
    ///   GET    /api/admin/users                      → list all non-admin users
    ///   DELETE /api/admin/students/{id}              → delete student (cascade)
    ///   DELETE /api/admin/teachers/{id}              → delete teacher (cascade + unassign)
    ///   PUT    /api/admin/teachers/{id}/verify       → mark teacher as verified
    ///   PUT    /api/admin/users/{id}/assign-teacher  → assign/unassign teacher to student
    ///   GET    /api/admin/assignments                → view all assignments (with teacher names)
    ///   GET    /api/admin/stats                      → system-wide statistics + chart data
    ///
    /// AUTH: every endpoint requires the "Admin" role.
    /// </summary>
    [Route("api/admin")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        // ── GET /api/admin/users ──────────────────────────────────────
        /// <summary>
        /// List all non-admin users in the system with their roles.
        /// </summary>
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var (succeeded, data, error) = await _adminService.GetAllUsersAsync();
            if (!succeeded)
                return BadRequest(new { success = false, message = error });

            return Ok(new { success = true, data });
        }

        // ── DELETE /api/admin/students/{id} ───────────────────────────
        /// <summary>
        /// Delete a student account.
        /// Programmatically deletes their submissions and reports before deleting the user.
        /// </summary>
        [HttpDelete("students/{id}")]
        public async Task<IActionResult> DeleteStudent(string id)
        {
            var (succeeded, error) = await _adminService.DeleteStudentAsync(id);
            if (!succeeded)
                return NotFound(new { success = false, message = error });

            return Ok(new { success = true, message = "Student deleted successfully." });
        }

        // ── DELETE /api/admin/teachers/{id} ───────────────────────────
        /// <summary>
        /// Delete a teacher account.
        /// Unassigns their students, deletes their assignments (cascading to
        /// submissions and reports), then deletes the teacher user record.
        /// </summary>
        [HttpDelete("teachers/{id}")]
        public async Task<IActionResult> DeleteTeacher(string id)
        {
            var (succeeded, error) = await _adminService.DeleteTeacherAsync(id);
            if (!succeeded)
                return NotFound(new { success = false, message = error });

            return Ok(new { success = true, message = "Teacher deleted successfully." });
        }

        // ── PUT /api/admin/teachers/{id}/verify ───────────────────────
        /// <summary>
        /// Verify a teacher account so they can log in.
        /// </summary>
        [HttpPut("teachers/{id}/verify")]
        public async Task<IActionResult> VerifyTeacher(string id)
        {
            var (succeeded, data, error) = await _adminService.VerifyTeacherAsync(id);
            if (!succeeded)
                return BadRequest(new { success = false, message = error });

            return Ok(new { success = true, data });
        }

        // ── PUT /api/admin/users/{id}/assign-teacher ──────────────────
        /// <summary>
        /// Assign (or remove) a teacher from a student account.
        /// Pass { "teacherId": null } to remove the current assignment.
        /// </summary>
        [HttpPut("users/{id}/assign-teacher")]
        public async Task<IActionResult> AssignTeacher(string id, [FromBody] AssignTeacherDto dto)
        {
            var (succeeded, data, error) = await _adminService.AssignTeacherAsync(id, dto.TeacherId);
            if (!succeeded)
                return BadRequest(new { success = false, message = error });

            return Ok(new { success = true, data });
        }

        // ── GET /api/admin/assignments ────────────────────────────────
        /// <summary>
        /// View all assignments across the entire platform, including teacher names.
        /// </summary>
        [HttpGet("assignments")]
        public async Task<IActionResult> GetAllAssignments()
        {
            var (succeeded, data, error) = await _adminService.GetAllAssignmentsAsync();
            if (!succeeded)
                return BadRequest(new { success = false, message = error });

            return Ok(new { success = true, data });
        }

        // ── GET /api/admin/stats ──────────────────────────────────────
        /// <summary>
        /// Get system-wide statistics including chart data for the admin dashboard.
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var (succeeded, data, error) = await _adminService.GetSystemStatsAsync();
            if (!succeeded)
                return BadRequest(new { success = false, message = error });

            return Ok(new { success = true, data });
        }
    }
}

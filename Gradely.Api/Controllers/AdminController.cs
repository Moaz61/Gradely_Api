using Gradely.Application.DTOs.Admin;
using Gradely.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gradely.Api.Controllers
{
    /// <summary>
    /// Admin API Controller (Phase 6).
    ///
    /// ENDPOINTS:
    ///   GET    /api/admin/users                → list all users (excluding admins)
    ///   POST   /api/admin/teachers             → create a teacher account
    ///   DELETE /api/admin/users/{id}           → remove a teacher or student
    ///   PUT    /api/admin/teachers/{id}/verify → mark teacher as verified
    ///   GET    /api/admin/stats                → system-wide statistics
    ///
    /// AUTH: every endpoint requires the "Admin" role.
    ///
    /// ROUTE: explicit "api/admin" so URLs match the spec exactly.
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
        /// List all users in the system with their roles.
        /// </summary>
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var (succeeded, data, error) = await _adminService.GetAllUsersAsync();
            if (!succeeded)
                return BadRequest(new { success = false, message = error });

            return Ok(new { success = true, data });
        }

        // ── POST /api/admin/teachers ──────────────────────────────────
        /// <summary>
        /// Create a new teacher account.
        /// </summary>
        [HttpPost("teachers")]
        public async Task<IActionResult> CreateTeacher([FromBody] CreateTeacherDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage);
                return BadRequest(new { success = false, message = string.Join(", ", errors) });
            }

            var (succeeded, data, error) = await _adminService.CreateTeacherAsync(dto);
            if (!succeeded)
                return BadRequest(new { success = false, message = error });

            return Ok(new { success = true, data });
        }

        // ── DELETE /api/admin/users/{id} ──────────────────────────────
        /// <summary>
        /// Delete a user account (Teacher or Student) by ID.
        /// Admin accounts cannot be deleted through this endpoint.
        /// </summary>
        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var (succeeded, error) = await _adminService.DeleteTeacherAsync(id);
            if (!succeeded)
                return NotFound(new { success = false, message = error });

            return Ok(new { success = true, message = "User deleted successfully." });
        }

        // ── PUT /api/admin/teachers/{id}/verify ───────────────────────
        /// <summary>
        /// Mark a teacher as verified.
        /// </summary>
        [HttpPut("teachers/{id}/verify")]
        public async Task<IActionResult> VerifyTeacher(string id)
        {
            var (succeeded, data, error) = await _adminService.VerifyTeacherAsync(id);
            if (!succeeded)
                return BadRequest(new { success = false, message = error });

            return Ok(new { success = true, data });
        }

        // ── GET /api/admin/stats ──────────────────────────────────────
        /// <summary>
        /// Get system-wide statistics.
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

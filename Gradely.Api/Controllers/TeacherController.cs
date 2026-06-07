using Gradely.Application.DTOs.Teacher;
using Gradely.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Gradely.Api.Controllers
{
    /// <summary>
    /// Teacher API Controller (Phase 5).
    ///
    /// ENDPOINTS:
    ///   GET    /api/teacher/assignments                       → list all assignments
    ///   GET    /api/teacher/assignments/{id}/submissions      → all student submissions for one assignment
    ///   GET    /api/teacher/submissions/{id}/report           → any student's report
    ///   GET    /api/teacher/stats                             → grade averages + distribution
    ///   POST   /api/teacher/assignments                      → create a new assignment
    ///   PUT    /api/teacher/assignments/{id}                  → update an existing assignment
    ///   DELETE /api/teacher/assignments/{id}                  → delete an assignment
    ///
    /// AUTH: every endpoint requires the "Teacher" role.
    ///
    /// ROUTE: explicit "api/teacher" instead of [controller] so the URLs
    /// match the spec exactly (/api/teacher/...) regardless of the class name.
    /// </summary>
    [Route("api/teacher")]
    [ApiController]
    [Authorize(Roles = "Teacher")]
    public class TeacherController : ControllerBase
    {
        private readonly ITeacherService _teacherService;

        public TeacherController(ITeacherService teacherService)
        {
            _teacherService = teacherService;
        }

        [HttpGet("assignments")]
        public async Task<IActionResult> GetAllAssignments()
        {
            var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(teacherId))
                return Unauthorized(new { success = false, message = "User ID not found in token." });

            var (succeeded, data, error) = await _teacherService.GetAllAssignmentsAsync(teacherId);
            if (!succeeded)
                return BadRequest(new { success = false, message = error });

            return Ok(new { success = true, data });
        }

        [HttpGet("assignments/{id}/submissions")]
        public async Task<IActionResult> GetSubmissionsForAssignment(Guid id)
        {
            var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(teacherId))
                return Unauthorized(new { success = false, message = "User ID not found in token." });

            var (succeeded, data, error) = await _teacherService.GetSubmissionsForAssignmentAsync(id, teacherId);
            if (!succeeded)
                return NotFound(new { success = false, message = error });

            return Ok(new { success = true, data });
        }

        [HttpGet("submissions/{id}/report")]
        public async Task<IActionResult> GetSubmissionReport(Guid id)
        {
            var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(teacherId))
                return Unauthorized(new { success = false, message = "User ID not found in token." });

            var (succeeded, data, error) = await _teacherService.GetSubmissionReportAsync(id, teacherId);
            if (!succeeded)
                return NotFound(new { success = false, message = error });

            return Ok(new { success = true, data });
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(teacherId))
                return Unauthorized(new { success = false, message = "User ID not found in token." });

            var (succeeded, data, error) = await _teacherService.GetStatsAsync(teacherId);
            if (!succeeded)
                return BadRequest(new { success = false, message = error });

            return Ok(new { success = true, data });
        }

        // ══════════════════════════════════════════════════════════════
        //  POST /api/teacher/assignments — Create a new assignment
        // ══════════════════════════════════════════════════════════════
        /// <summary>
        /// Create a new assignment.
        ///
        /// FLOW:
        ///   1. Teacher sends POST with JSON body (title, description, dueDate, maxGrade)
        ///   2. ASP.NET validates the DTO (Data Annotations)
        ///   3. Service creates the Assignment entity and saves to DB
        ///   4. Returns 201 Created with the new assignment data
        ///
        /// RETURNS:
        ///   201 Created → { success: true, data: { id, title, ... } }
        ///   400 Bad Request → validation errors
        ///   401/403 → unauthorized or wrong role
        /// </summary>
        [HttpPost("assignments")]
        public async Task<IActionResult> CreateAssignment([FromBody] CreateAssignmentDto dto)
        {
            var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(teacherId))
                return Unauthorized(new { success = false, message = "User ID not found in token." });

            var (succeeded, data, error) = await _teacherService.CreateAssignmentAsync(dto, teacherId);
            if (!succeeded)
                return BadRequest(new { success = false, message = error });

            return StatusCode(201, new { success = true, data });
        }

        // ══════════════════════════════════════════════════════════════
        //  PUT /api/teacher/assignments/{id} — Update an assignment
        // ══════════════════════════════════════════════════════════════
        /// <summary>
        /// Update an existing assignment.
        ///
        /// RETURNS:
        ///   200 OK → { success: true, data: { id, title, ... } }
        ///   404 Not Found → assignment doesn't exist
        ///   400 Bad Request → validation errors
        ///   401/403 → unauthorized or wrong role
        /// </summary>
        [HttpPut("assignments/{id}")]
        public async Task<IActionResult> UpdateAssignment(Guid id, [FromBody] UpdateAssignmentDto dto)
        {
            var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(teacherId))
                return Unauthorized(new { success = false, message = "User ID not found in token." });

            var (succeeded, data, error) = await _teacherService.UpdateAssignmentAsync(id, dto, teacherId);
            if (!succeeded)
            {
                if (error != null && error.Contains("not found"))
                    return NotFound(new { success = false, message = error });
                return BadRequest(new { success = false, message = error });
            }

            return Ok(new { success = true, data });
        }

        // ══════════════════════════════════════════════════════════════
        //  DELETE /api/teacher/assignments/{id} — Delete an assignment
        // ══════════════════════════════════════════════════════════════
        /// <summary>
        /// Delete an assignment by ID.
        /// Will refuse if the assignment has any submissions (safety check).
        ///
        /// RETURNS:
        ///   200 OK → { success: true, data: { message: "..." } }
        ///   404 Not Found → assignment doesn't exist
        ///   400 Bad Request → has submissions, can't delete
        ///   401/403 → unauthorized or wrong role
        /// </summary>
        [HttpDelete("assignments/{id}")]
        public async Task<IActionResult> DeleteAssignment(Guid id)
        {
            var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(teacherId))
                return Unauthorized(new { success = false, message = "User ID not found in token." });

            var (succeeded, data, error) = await _teacherService.DeleteAssignmentAsync(id, teacherId);
            if (!succeeded)
            {
                if (error != null && error.Contains("not found"))
                    return NotFound(new { success = false, message = error });
                return BadRequest(new { success = false, message = error });
            }

            return Ok(new { success = true, data });
        }
    }
}


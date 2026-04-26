using Gradely.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gradely.Api.Controllers
{
    /// <summary>
    /// Teacher API Controller (Phase 5).
    ///
    /// ENDPOINTS:
    ///   GET /api/teacher/assignments                       → list all assignments
    ///   GET /api/teacher/assignments/{id}/submissions      → all student submissions for one assignment
    ///   GET /api/teacher/submissions/{id}/report           → any student's report
    ///   GET /api/teacher/stats                             → grade averages + distribution
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
            var (succeeded, data, error) = await _teacherService.GetAllAssignmentsAsync();
            if (!succeeded)
                return BadRequest(new { success = false, message = error });

            return Ok(new { success = true, data });
        }

        [HttpGet("assignments/{id}/submissions")]
        public async Task<IActionResult> GetSubmissionsForAssignment(Guid id)
        {
            var (succeeded, data, error) = await _teacherService.GetSubmissionsForAssignmentAsync(id);
            if (!succeeded)
                return NotFound(new { success = false, message = error });

            return Ok(new { success = true, data });
        }

        [HttpGet("submissions/{id}/report")]
        public async Task<IActionResult> GetSubmissionReport(Guid id)
        {
            var (succeeded, data, error) = await _teacherService.GetSubmissionReportAsync(id);
            if (!succeeded)
                return NotFound(new { success = false, message = error });

            return Ok(new { success = true, data });
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var (succeeded, data, error) = await _teacherService.GetStatsAsync();
            if (!succeeded)
                return BadRequest(new { success = false, message = error });

            return Ok(new { success = true, data });
        }
    }
}

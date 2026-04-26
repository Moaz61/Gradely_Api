using System.Security.Claims;
using Gradely.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gradely.Api.Controllers
{
    /// <summary>
    /// Student dashboard endpoint (Phase 5).
    ///
    /// ENDPOINTS:
    ///   GET /api/student/dashboard → totals + average grade for the current student
    ///
    /// AUTH: requires the "Student" role. The student id is read from the
    /// JWT (NameIdentifier claim), never from the URL or body — same pattern
    /// as SubmissionsController.
    /// </summary>
    [Route("api/student")]
    [ApiController]
    [Authorize(Roles = "Student")]
    public class StudentController : ControllerBase
    {
        private readonly IStudentService _studentService;

        public StudentController(IStudentService studentService)
        {
            _studentService = studentService;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(studentId))
                return Unauthorized(new { success = false, message = "Invalid token." });

            var (succeeded, data, error) = await _studentService.GetDashboardAsync(studentId);
            if (!succeeded)
                return BadRequest(new { success = false, message = error });

            return Ok(new { success = true, data });
        }
    }
}

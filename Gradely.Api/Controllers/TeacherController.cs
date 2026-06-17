using Gradely.Application.DTOs.Teacher;
using Gradely.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Gradely.Api.Controllers
{
    /// <summary>
    /// Teacher API Controller — manages assignments, views submissions,
    /// downloads files, views students, and accesses grade statistics.
    ///
    /// ENDPOINTS:
    ///   GET    /api/teacher/assignments                        → list teacher's assignments
    ///   POST   /api/teacher/assignments                       → create new assignment
    ///   GET    /api/teacher/assignments/{id}                  → view single assignment
    ///   PUT    /api/teacher/assignments/{id}                  → update assignment
    ///   DELETE /api/teacher/assignments/{id}                  → delete assignment (cascade)
    ///   GET    /api/teacher/assignments/{id}/submissions      → submissions for assignment
    ///   GET    /api/teacher/assignments/{id}/student-status   → student submission status grid
    ///   GET    /api/teacher/submissions/{id}                  → view single submission
    ///   GET    /api/teacher/submissions/{id}/file             → download student PDF
    ///   GET    /api/teacher/submissions/{id}/report           → view grading report
    ///   GET    /api/teacher/students                          → list assigned students
    ///   GET    /api/teacher/submissions/student/{studentId}   → all submissions by a student
    ///   GET    /api/teacher/stats                             → grade statistics
    ///
    /// AUTH: every endpoint requires the "Teacher" role.
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

        // ── Helper ────────────────────────────────────────────────────
        private string? GetTeacherId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

        // ── GET /api/teacher/assignments ──────────────────────────────
        [HttpGet("assignments")]
        public async Task<IActionResult> GetAllAssignments()
        {
            var teacherId = GetTeacherId();
            if (string.IsNullOrEmpty(teacherId))
                return Unauthorized(new { success = false, message = "User ID not found in token." });

            var (succeeded, data, error) = await _teacherService.GetAllAssignmentsAsync(teacherId);
            if (!succeeded)
                return BadRequest(new { success = false, message = error });

            return Ok(new { success = true, data });
        }

        // ── POST /api/teacher/assignments ─────────────────────────────
        [HttpPost("assignments")]
        public async Task<IActionResult> CreateAssignment([FromBody] CreateAssignmentDto dto)
        {
            var teacherId = GetTeacherId();
            if (string.IsNullOrEmpty(teacherId))
                return Unauthorized(new { success = false, message = "User ID not found in token." });

            var (succeeded, data, error) = await _teacherService.CreateAssignmentAsync(dto, teacherId);
            if (!succeeded)
                return BadRequest(new { success = false, message = error });

            return StatusCode(201, new { success = true, data });
        }

        // ── GET /api/teacher/assignments/{id} ─────────────────────────
        [HttpGet("assignments/{id}")]
        public async Task<IActionResult> GetAssignmentById(Guid id)
        {
            var teacherId = GetTeacherId();
            if (string.IsNullOrEmpty(teacherId))
                return Unauthorized(new { success = false, message = "User ID not found in token." });

            var (succeeded, data, error) = await _teacherService.GetAssignmentByIdAsync(id, teacherId);
            if (!succeeded)
                return NotFound(new { success = false, message = error });

            return Ok(new { success = true, data });
        }

        // ── PUT /api/teacher/assignments/{id} ─────────────────────────
        [HttpPut("assignments/{id}")]
        public async Task<IActionResult> UpdateAssignment(Guid id, [FromBody] UpdateAssignmentDto dto)
        {
            var teacherId = GetTeacherId();
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

        // ── DELETE /api/teacher/assignments/{id} ──────────────────────
        [HttpDelete("assignments/{id}")]
        public async Task<IActionResult> DeleteAssignment(Guid id)
        {
            var teacherId = GetTeacherId();
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

        // ── GET /api/teacher/assignments/{id}/submissions ─────────────
        [HttpGet("assignments/{id}/submissions")]
        public async Task<IActionResult> GetSubmissionsForAssignment(Guid id)
        {
            var teacherId = GetTeacherId();
            if (string.IsNullOrEmpty(teacherId))
                return Unauthorized(new { success = false, message = "User ID not found in token." });

            var (succeeded, data, error) = await _teacherService.GetSubmissionsForAssignmentAsync(id, teacherId);
            if (!succeeded)
                return NotFound(new { success = false, message = error });

            return Ok(new { success = true, data });
        }

        // ── GET /api/teacher/assignments/{id}/student-status ──────────
        [HttpGet("assignments/{id}/student-status")]
        public async Task<IActionResult> GetAssignmentStudentStatus(Guid id)
        {
            var teacherId = GetTeacherId();
            if (string.IsNullOrEmpty(teacherId))
                return Unauthorized(new { success = false, message = "User ID not found in token." });

            var (succeeded, data, error) = await _teacherService.GetAssignmentStudentStatusAsync(id, teacherId);
            if (!succeeded)
                return NotFound(new { success = false, message = error });

            return Ok(new { success = true, data });
        }

        // ── GET /api/teacher/submissions/{id} ─────────────────────────
        // NOTE: This route must come BEFORE submissions/student/{studentId} to avoid
        //       ambiguous routing. ASP.NET Core matches the more specific route first.
        [HttpGet("submissions/{id:guid}")]
        public async Task<IActionResult> GetSubmissionById(Guid id)
        {
            var teacherId = GetTeacherId();
            if (string.IsNullOrEmpty(teacherId))
                return Unauthorized(new { success = false, message = "User ID not found in token." });

            var (succeeded, data, error) = await _teacherService.GetSubmissionByIdAsync(id, teacherId);
            if (!succeeded)
                return NotFound(new { success = false, message = error });

            return Ok(new { success = true, data });
        }

        // ── GET /api/teacher/submissions/{id}/file ────────────────────
        [HttpGet("submissions/{id}/file")]
        public async Task<IActionResult> DownloadSubmissionFile(Guid id)
        {
            var teacherId = GetTeacherId();
            if (string.IsNullOrEmpty(teacherId))
                return Unauthorized(new { success = false, message = "User ID not found in token." });

            var (succeeded, filePath, originalFileName, error) =
                await _teacherService.GetSubmissionFilePathAsync(id, teacherId);

            if (!succeeded)
                return NotFound(new { success = false, message = error });

            // Build the full disk path from the relative stored path
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", filePath!);
            if (!System.IO.File.Exists(fullPath))
                return NotFound(new { success = false, message = "File not found on server." });

            var fileBytes = await System.IO.File.ReadAllBytesAsync(fullPath);
            return File(fileBytes, "application/pdf", originalFileName ?? "submission.pdf");
        }

        // ── GET /api/teacher/submissions/{id}/report ──────────────────
        [HttpGet("submissions/{id}/report")]
        public async Task<IActionResult> GetSubmissionReport(Guid id)
        {
            var teacherId = GetTeacherId();
            if (string.IsNullOrEmpty(teacherId))
                return Unauthorized(new { success = false, message = "User ID not found in token." });

            var (succeeded, data, error) = await _teacherService.GetSubmissionReportAsync(id, teacherId);
            if (!succeeded)
                return NotFound(new { success = false, message = error });

            return Ok(new { success = true, data });
        }

        // ── GET /api/teacher/students ─────────────────────────────────
        [HttpGet("students")]
        public async Task<IActionResult> GetStudents()
        {
            var teacherId = GetTeacherId();
            if (string.IsNullOrEmpty(teacherId))
                return Unauthorized(new { success = false, message = "User ID not found in token." });

            var (succeeded, data, error) = await _teacherService.GetStudentsAsync(teacherId);
            if (!succeeded)
                return BadRequest(new { success = false, message = error });

            return Ok(new { success = true, data });
        }

        // ── GET /api/teacher/submissions/student/{studentId} ──────────
        [HttpGet("submissions/student/{studentId}")]
        public async Task<IActionResult> GetStudentSubmissions(string studentId)
        {
            var teacherId = GetTeacherId();
            if (string.IsNullOrEmpty(teacherId))
                return Unauthorized(new { success = false, message = "User ID not found in token." });

            var (succeeded, data, error) = await _teacherService.GetStudentSubmissionsAsync(studentId, teacherId);
            if (!succeeded)
                return NotFound(new { success = false, message = error });

            return Ok(new { success = true, data });
        }

        // ── GET /api/teacher/stats ────────────────────────────────────
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var teacherId = GetTeacherId();
            if (string.IsNullOrEmpty(teacherId))
                return Unauthorized(new { success = false, message = "User ID not found in token." });

            var (succeeded, data, error) = await _teacherService.GetStatsAsync(teacherId);
            if (!succeeded)
                return BadRequest(new { success = false, message = error });

            return Ok(new { success = true, data });
        }
    }
}

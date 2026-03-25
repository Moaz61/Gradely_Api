using System.Security.Claims;
using Gradely.Application.DTOs.Submissions;
using Gradely.Application.Services;
using Gradely.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gradely.Api.Controllers
{
    /// <summary>
    /// Submissions API Controller.
    /// 
    /// ENDPOINTS:
    ///   POST /api/submissions              → Upload a PDF submission
    ///   GET  /api/submissions              → List student's own submissions
    ///   GET  /api/submissions/{id}         → Get a single submission
    ///   GET  /api/submissions/{id}/report  → Get the ML grading report
    /// 
    /// AUTH:
    ///   All endpoints require the "Student" role.
    ///   Every endpoint uses the JWT token to identify WHO the student is,
    ///   ensuring students can only see their own submissions.
    /// 
    /// FILE UPLOAD:
    ///   The POST endpoint uses [FromForm] instead of [FromBody].
    ///   [FromBody] = JSON request body
    ///   [FromForm] = multipart/form-data (used for file uploads)
    ///   The client sends the file + assignmentId as form fields, not JSON.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Student")]
    public class SubmissionsController : ControllerBase
    {
        // ── Dependencies ─────────────────────────────────────────────
        // We inject BOTH the interface and the concrete service:
        //   - ISubmissionService: for the read operations (GetStudentSubmissions, etc.)
        //   - SubmissionService (concrete): for SubmitWithFileAsync which accepts IFormFile
        //     (the interface can't reference IFormFile because it's in the Domain layer)
        private readonly ISubmissionService _submissionService;
        private readonly SubmissionService _submissionServiceConcrete;

        public SubmissionsController(
            ISubmissionService submissionService,
            SubmissionService submissionServiceConcrete)
        {
            _submissionService = submissionService;
            _submissionServiceConcrete = submissionServiceConcrete;
        }

        // ══════════════════════════════════════════════════════════════
        //  POST /api/submissions
        // ══════════════════════════════════════════════════════════════
        /// <summary>
        /// Upload a PDF submission for an assignment.
        /// 
        /// HOW FILE UPLOAD WORKS IN ASP.NET:
        ///   1. Client sends a multipart/form-data request
        ///   2. ASP.NET binds form fields to the DTO ([FromForm])
        ///   3. ASP.NET binds the file to the IFormFile parameter
        ///   4. We extract the student's ID from the JWT token
        ///   5. We pass everything to the SubmissionService
        /// 
        /// [FromForm] vs [FromBody]:
        ///   [FromBody] → expects JSON: { "assignmentId": "..." }
        ///   [FromForm] → expects form-data: assignmentId=...&file=binary
        ///   File uploads MUST use [FromForm] because JSON can't carry binary files.
        /// 
        /// RETURNS:
        ///   200 OK → { success: true, data: { id, assignmentTitle, status, ... } }
        ///   400 Bad Request → { success: false, message: "Only PDF files are allowed." }
        ///   401 Unauthorized → no JWT token
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Submit(
            [FromForm] SubmitAssignmentDto dto,
            IFormFile file)
        {
            // Extract the student's ID from the JWT token
            // This is the same pattern used in AuthController.GetCurrentUser()
            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(studentId))
                return Unauthorized(new { success = false, message = "Invalid token." });

            // Call the concrete service method that accepts IFormFile
            var (succeeded, data, error) = await _submissionServiceConcrete
                .SubmitWithFileAsync(dto, file, studentId);

            if (!succeeded)
                return BadRequest(new { success = false, message = error });

            return Ok(new { success = true, data });
        }

        // ══════════════════════════════════════════════════════════════
        //  GET /api/submissions
        // ══════════════════════════════════════════════════════════════
        /// <summary>
        /// List all submissions for the current student.
        /// 
        /// DATA ISOLATION:
        ///   The studentId comes from the JWT token, not from a query parameter.
        ///   This means a student can NEVER see another student's submissions.
        ///   Even if they modify the request, the server always uses the token's ID.
        /// 
        /// RETURNS:
        ///   200 OK → { success: true, data: [ { id, assignmentTitle, status, ... }, ... ] }
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetMySubmissions()
        {
            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(studentId))
                return Unauthorized(new { success = false, message = "Invalid token." });

            var (succeeded, data, error) = await _submissionService
                .GetStudentSubmissionsAsync(studentId);

            if (!succeeded)
                return BadRequest(new { success = false, message = error });

            return Ok(new { success = true, data });
        }

        // ══════════════════════════════════════════════════════════════
        //  GET /api/submissions/{id}
        // ══════════════════════════════════════════════════════════════
        /// <summary>
        /// Get a single submission by ID (must belong to the current student).
        /// 
        /// SECURITY:
        ///   The service checks that submission.StudentId == the token's studentId.
        ///   If a student tries to access another student's submission by guessing
        ///   the GUID, they get "You do not have access to this submission."
        /// 
        /// RETURNS:
        ///   200 OK → { success: true, data: { id, assignmentTitle, ... } }
        ///   404 Not Found → submission doesn't exist
        ///   403-like → submission belongs to another student
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(studentId))
                return Unauthorized(new { success = false, message = "Invalid token." });

            var (succeeded, data, error) = await _submissionService
                .GetSubmissionByIdAsync(id, studentId);

            if (!succeeded)
                return NotFound(new { success = false, message = error });

            return Ok(new { success = true, data });
        }

        // ══════════════════════════════════════════════════════════════
        //  GET /api/submissions/{id}/report
        // ══════════════════════════════════════════════════════════════
        /// <summary>
        /// Get the ML grading report for a submission.
        /// 
        /// WHEN DOES A REPORT EXIST?
        ///   After the ML system processes the submission (Phase 4).
        ///   Until then, this endpoint returns "No report available yet."
        /// 
        /// ROUTE:
        ///   GET /api/submissions/a1b2c3d4-.../report
        ///   The "report" part is a fixed string, not a parameter.
        ///   ASP.NET knows {id} is the Guid and "report" is literal text.
        /// 
        /// RETURNS:
        ///   200 OK → { success: true, data: { grade, feedback, mistakes, ... } }
        ///   404 Not Found → no report yet (still being graded)
        /// </summary>
        [HttpGet("{id}/report")]
        public async Task<IActionResult> GetReport(Guid id)
        {
            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(studentId))
                return Unauthorized(new { success = false, message = "Invalid token." });

            var (succeeded, data, error) = await _submissionService
                .GetSubmissionReportAsync(id, studentId);

            if (!succeeded)
                return NotFound(new { success = false, message = error });

            return Ok(new { success = true, data });
        }
    }
}

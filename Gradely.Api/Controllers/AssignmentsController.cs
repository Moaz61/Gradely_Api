using Gradely.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gradely.Api.Controllers
{
    /// <summary>
    /// Assignments API Controller.
    /// 
    /// ENDPOINTS:
    ///   GET /api/assignments       → List all assignments
    ///   GET /api/assignments/{id}  → Get a single assignment by ID
    /// 
    /// AUTH:
    ///   All endpoints require the "Student" role.
    ///   [Authorize(Roles = "Student")] is applied at the controller level,
    ///   meaning ALL endpoints in this controller require a valid JWT token
    ///   AND the user must have the "Student" role.
    /// 
    /// HOW ROLE-BASED AUTH WORKS:
    ///   1. Client sends: Authorization: Bearer eyJhbGciOi...
    ///   2. ASP.NET middleware validates the token
    ///   3. Reads the role claim from the token
    ///   4. Checks if the role matches "Student"
    ///   5. If not → 403 Forbidden (or 401 if no token)
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Student")]
    public class AssignmentsController : ControllerBase
    {
        // ── Dependencies ─────────────────────────────────────────────
        private readonly IAssignmentService _assignmentService;

        public AssignmentsController(IAssignmentService assignmentService)
        {
            _assignmentService = assignmentService;
        }

        // ══════════════════════════════════════════════════════════════
        //  GET /api/assignments
        // ══════════════════════════════════════════════════════════════
        /// <summary>
        /// List all available assignments.
        /// 
        /// FLOW:
        ///   1. Student sends GET request with JWT token
        ///   2. [Authorize] middleware validates the token and role
        ///   3. This method calls AssignmentService.GetAllAssignmentsAsync()
        ///   4. Service queries the database and maps to DTOs
        ///   5. Returns 200 OK with the list of assignments
        /// 
        /// RETURNS:
        ///   200 OK → { success: true, data: [ { id, title, ... }, ... ] }
        ///   401 Unauthorized → if no JWT token provided
        ///   403 Forbidden → if user doesn't have "Student" role
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var (succeeded, data, error) = await _assignmentService.GetAllAssignmentsAsync();

            if (!succeeded)
                return BadRequest(new { success = false, message = error });

            return Ok(new { success = true, data });
        }

        // ══════════════════════════════════════════════════════════════
        //  GET /api/assignments/{id}
        // ══════════════════════════════════════════════════════════════
        /// <summary>
        /// Get a single assignment by its ID.
        /// 
        /// ROUTE PARAMETER:
        ///   {id} is extracted from the URL and bound to the Guid parameter.
        ///   Example: GET /api/assignments/3f2504e0-4f89-11d3-9a0c-0305e82c3301
        ///   ASP.NET automatically parses the string to a Guid.
        /// 
        /// RETURNS:
        ///   200 OK → { success: true, data: { id, title, ... } }
        ///   404 Not Found → { success: false, message: "Assignment not found." }
        ///   401/403 → unauthorized or wrong role
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var (succeeded, data, error) = await _assignmentService.GetAssignmentByIdAsync(id);

            if (!succeeded)
                return NotFound(new { success = false, message = error });

            return Ok(new { success = true, data });
        }
    }
}

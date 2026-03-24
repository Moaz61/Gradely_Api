using System.Security.Claims;
using Gradely.Application.DTOs.Auth;
using Gradely.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gradely.Api.Controllers
{
    /// <summary>
    /// Authentication API Controller.
    /// 
    /// WHAT IS A CONTROLLER?
    ///   A controller is the "front door" of your API. It:
    ///   1. Receives HTTP requests from clients
    ///   2. Validates input (model binding + data annotations)
    ///   3. Calls the service layer (AuthService) to do the work
    ///   4. Returns HTTP responses (200 OK, 400 Bad Request, 401 Unauthorized, etc.)
    /// 
    /// ATTRIBUTES:
    ///   [Route("api/[controller]")] → URL = /api/auth  (controller name minus "Controller")
    ///   [ApiController] → enables automatic model validation + 400 responses
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        // ── Dependencies (injected via DI) ───────────────────────────
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // ══════════════════════════════════════════════════════════════
        //  POST /api/auth/register
        // ══════════════════════════════════════════════════════════════
        /// <summary>
        /// Register a new student account.
        /// 
        /// FLOW:
        ///   Client sends JSON → ASP.NET deserializes to RegisterDto
        ///   → [ApiController] validates [Required], [EmailAddress], etc. automatically
        ///   → If validation fails, returns 400 without reaching this method
        ///   → If validation passes, this method runs and calls AuthService
        /// 
        /// [AllowAnonymous] = no JWT token needed (anyone can register)
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            var (succeeded, data, error) = await _authService.RegisterAsync(registerDto);

            if (!succeeded)
                return BadRequest(new { success = false, message = error });

            return Ok(new { success = true, data });
        }

        // ══════════════════════════════════════════════════════════════
        //  POST /api/auth/login
        // ══════════════════════════════════════════════════════════════
        /// <summary>
        /// Login with email and password, receive a JWT token.
        /// 
        /// The client stores this token and includes it in future requests:
        ///   Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
        /// 
        /// [AllowAnonymous] = no token needed (you're trying to GET a token)
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var (succeeded, data, error) = await _authService.LoginAsync(loginDto);

            if (!succeeded)
                return Unauthorized(new { success = false, message = error });

            return Ok(new { success = true, data });
        }

        // ══════════════════════════════════════════════════════════════
        //  GET /api/auth/me
        // ══════════════════════════════════════════════════════════════
        /// <summary>
        /// Get the current logged-in user's profile.
        /// 
        /// [Authorize] = JWT token REQUIRED, otherwise → 401 Unauthorized
        /// 
        /// HOW WE KNOW WHO THE USER IS:
        ///   1. Client sends: Authorization: Bearer eyJhbGciOi...
        ///   2. ASP.NET middleware validates the token
        ///   3. Extracts claims from the token (user ID, email, role)
        ///   4. Puts them in User.Claims (accessible via this.User)
        ///   5. We read the NameIdentifier claim = user's ID
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            // Extract the user ID from the JWT token's claims
            // ClaimTypes.NameIdentifier = the "sub" claim we set in AuthService.GenerateJwtToken
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { success = false, message = "Invalid token." });

            var (succeeded, data, error) = await _authService.GetCurrentUserAsync(userId);

            if (!succeeded)
                return NotFound(new { success = false, message = error });

            return Ok(new { success = true, data });
        }

        // ══════════════════════════════════════════════════════════════
        //  POST /api/auth/logout
        // ══════════════════════════════════════════════════════════════
        /// <summary>
        /// Logout the current user.
        /// 
        /// WITH JWT, LOGOUT IS CLIENT-SIDE:
        ///   JWTs are stateless — the server doesn't store sessions.
        ///   So "logout" = the client deletes its stored token.
        ///   This endpoint exists so the frontend has a consistent API to call.
        /// 
        /// FOR REAL SERVER-SIDE LOGOUT (optional, more complex):
        ///   You'd need a token blacklist (store revoked tokens in DB/Redis
        ///   and check every request). Not needed for a graduation project.
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            // JWT is stateless — nothing to do on the server.
            // The client should delete the token from localStorage/cookies.
            return Ok(new { success = true, message = "Logged out successfully. Please remove your token." });
        }
    }
}

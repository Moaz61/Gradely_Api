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
        /// Register a new user account (Student or Teacher).
        /// 
        /// FLOW:
        ///   Client sends JSON with role field → ASP.NET deserializes to RegisterDto
        ///   → [ApiController] validates [Required], [EmailAddress], etc. automatically
        ///   → If validation fails, returns 400 without reaching this method
        ///   → If validation passes, this method runs and calls AuthService
        /// 
        /// ROLE BEHAVIOR:
        ///   - Student: registered immediately, receives JWT + refresh token
        ///   - Teacher: registered with IsVerified = false, must wait for admin approval
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
        /// Login with email and password, receive a JWT access token + refresh token.
        /// 
        /// The client stores both tokens:
        ///   - Access token: sent in every request as Authorization: Bearer eyJhbGciOi...
        ///   - Refresh token: stored securely, used to get new access token when it expires
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
        //  POST /api/auth/refresh
        // ══════════════════════════════════════════════════════════════
        /// <summary>
        /// Get a new access token using a refresh token.
        /// 
        /// WHEN TO USE:
        ///   When the client's access token expires (after 30 minutes), instead of
        ///   redirecting the user to the login page, the client calls this endpoint
        ///   with the refresh token to silently get a new access token.
        /// 
        /// TOKEN ROTATION:
        ///   Each call returns a NEW refresh token and revokes the old one.
        ///   This means each refresh token can only be used ONCE — if someone steals it
        ///   and tries to use it after the real user, it will be revoked.
        /// 
        /// [AllowAnonymous] = no JWT needed (the user's access token is expired!)
        /// </summary>
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
        {
            var (succeeded, data, error) = await _authService.RefreshTokenAsync(refreshTokenDto);

            if (!succeeded)
                return Unauthorized(new { success = false, message = error });

            return Ok(new { success = true, data });
        }

        // ══════════════════════════════════════════════════════════════
        //  POST /api/auth/logout
        // ══════════════════════════════════════════════════════════════
        /// <summary>
        /// Logout the current user.
        /// 
        /// WITH REFRESH TOKENS, LOGOUT IS NOW SERVER-SIDE:
        ///   Unlike before (where JWT logout was purely client-side),
        ///   we now revoke all active refresh tokens in the database.
        ///   This means even if someone has the user's refresh token,
        ///   they can't use it after logout.
        /// 
        /// The client should also:
        ///   1. Delete the access token from localStorage/cookies
        ///   2. Delete the refresh token from localStorage/cookies
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { success = false, message = "Invalid token." });

            // Revoke all active refresh tokens for this user
            var (succeeded, error) = await _authService.RevokeRefreshTokenAsync(userId);

            if (!succeeded)
                return BadRequest(new { success = false, message = error });

            return Ok(new { success = true, message = "Logged out successfully. All refresh tokens have been revoked." });
        }
    }
}

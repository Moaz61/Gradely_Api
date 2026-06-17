using Gradely.Application.DTOs.Profile;
using Gradely.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Gradely.Api.Controllers
{
    /// <summary>
    /// Profile Controller — allows any authenticated user to update their profile.
    ///
    /// ENDPOINTS:
    ///   PUT /api/profile → Update FullName and/or Email
    ///
    /// AUTH: requires any authenticated user (Student, Teacher, or Admin).
    /// </summary>
    [Route("api/profile")]
    [ApiController]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly ProfileService _profileService;

        public ProfileController(ProfileService profileService)
        {
            _profileService = profileService;
        }

        // ── PUT /api/profile ──────────────────────────────────────────
        /// <summary>
        /// Update the current user's profile (FullName and/or Email).
        ///
        /// REQUEST:
        ///   { "fullName": "New Name", "email": "new@email.com" }
        ///   Both fields are optional — only provided fields are updated.
        ///
        /// RESPONSE (200 OK):
        ///   { "success": true, "message": "Profile updated successfully.", "data": { ... } }
        /// </summary>
        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { success = false, message = "User ID not found in token." });

            var (succeeded, data, error) = await _profileService.UpdateProfileAsync(userId, dto);
            if (!succeeded)
                return BadRequest(new { success = false, message = error });

            return Ok(new { success = true, message = "Profile updated successfully.", data });
        }
    }
}

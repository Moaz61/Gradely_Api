using Gradely.Application.DTOs.Admin;
using Gradely.Application.DTOs.Profile;
using Gradely.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Gradely.Application.Services
{
    /// <summary>
    /// Handles profile updates for any authenticated user (Student, Teacher, Admin).
    /// Used by PUT /api/profile.
    /// </summary>
    public class ProfileService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfileService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        /// <summary>
        /// Updates the FullName and/or Email of the currently authenticated user.
        /// Only non-null fields in the DTO are applied.
        /// Returns a UserDto with the updated values.
        /// </summary>
        public async Task<(bool Succeeded, object? Data, string? Error)> UpdateProfileAsync(
            string userId, UpdateProfileDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return (false, null, "User not found.");

            bool changed = false;

            // Update FullName if provided
            if (!string.IsNullOrWhiteSpace(dto.FullName))
            {
                user.FullName = dto.FullName.Trim();
                changed = true;
            }

            // Update Email if provided and different from current
            if (!string.IsNullOrWhiteSpace(dto.Email) &&
                !string.Equals(user.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
            {
                // Check email is not taken by another user
                var existingUser = await _userManager.FindByEmailAsync(dto.Email);
                if (existingUser != null && existingUser.Id != userId)
                    return (false, null, "This email address is already in use.");

                user.Email = dto.Email;
                user.UserName = dto.Email;      // Identity keeps UserName = Email
                user.NormalizedEmail = dto.Email.ToUpperInvariant();
                user.NormalizedUserName = dto.Email.ToUpperInvariant();
                changed = true;
            }

            if (!changed)
                return (false, null, "No changes provided.");

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return (false, null, errors);
            }

            // Return updated profile info
            var roles = await _userManager.GetRolesAsync(user);
            var data = new
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = roles.FirstOrDefault() ?? string.Empty
            };

            return (true, data, null);
        }
    }
}

using System.ComponentModel.DataAnnotations;

namespace Gradely.Application.DTOs.Profile
{
    /// <summary>
    /// Request body for PUT /api/profile.
    /// Allows any authenticated user (Student, Teacher, Admin) to
    /// update their FullName and/or Email address.
    ///
    /// Both fields are optional — only non-null values will be updated.
    /// </summary>
    public class UpdateProfileDto
    {
        /// <summary>
        /// The user's new display name.
        /// Leave null to keep the current name.
        /// </summary>
        [MaxLength(100)]
        public string? FullName { get; set; }

        /// <summary>
        /// The user's new email address.
        /// Leave null to keep the current email.
        /// Must be a valid email format if provided.
        /// </summary>
        [EmailAddress]
        public string? Email { get; set; }
    }
}

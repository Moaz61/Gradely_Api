namespace Gradely.Application.DTOs.Admin
{
    /// <summary>
    /// DTO for listing users in the admin panel.
    /// Used by GET /api/admin/users.
    ///
    /// EXAMPLE JSON returned:
    ///   {
    ///     "id": "abc123...",
    ///     "fullName": "Moaaz Ahmed",
    ///     "email": "moaaz@gradely.com",
    ///     "role": "Student",
    ///     "isVerified": false,
    ///     "createdAt": "2026-03-24T15:10:00Z"
    ///   }
    /// </summary>
    public class UserDto
    {
        /// <summary>The user's unique ID (from ASP.NET Identity).</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>The user's full display name.</summary>
        public string FullName { get; set; } = string.Empty;

        /// <summary>The user's email address.</summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>The user's role (Student, Teacher, or Admin).</summary>
        public string Role { get; set; } = string.Empty;

        /// <summary>Whether the user has been verified by an admin.</summary>
        public bool IsVerified { get; set; }

        /// <summary>When the account was created.</summary>
        public DateTime CreatedAt { get; set; }
    }
}

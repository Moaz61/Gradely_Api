namespace Gradely.Application.DTOs.Auth
{
    /// <summary>
    /// Data Transfer Object returned after successful login or registration.
    /// 
    /// WHY a separate response DTO?
    ///   We never return the raw ApplicationUser entity to the client because:
    ///   1. It contains PasswordHash — a security disaster if exposed
    ///   2. It contains Identity fields the client doesn't need (SecurityStamp, ConcurrencyStamp, etc.)
    ///   3. We want to include the JWT token, which isn't part of the entity
    /// 
    /// EXAMPLE JSON returned to the client:
    ///   {
    ///     "token": "eyJhbGciOiJIUzI1NiIs...",
    ///     "expiresAt": "2026-03-25T23:00:00Z",
    ///     "email": "moaaz@gradely.com",
    ///     "fullName": "Moaaz Ahmed",
    ///     "role": "Student"
    ///   }
    /// </summary>
    public class AuthResponseDto
    {
        /// <summary>The JWT token the client must include in the Authorization header for future requests.</summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>When the token expires. After this, the client must login again.</summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>The user's email.</summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>The user's display name.</summary>
        public string FullName { get; set; } = string.Empty;

        /// <summary>The user's role (Student, Teacher, or Admin).</summary>
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// The refresh token — a long-lived token used to get a new access token
        /// when the current one expires, without re-entering credentials.
        /// </summary>
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>When the refresh token expires (30 days from creation).</summary>
        public DateTime RefreshTokenExpiresAt { get; set; }
    }
}

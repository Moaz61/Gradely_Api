namespace Gradely.Domain.Entities
{
    /// <summary>
    /// Represents a refresh token stored in the database.
    /// 
    /// WHY REFRESH TOKENS?
    ///   JWT access tokens are short-lived (30 minutes) for security.
    ///   When they expire, instead of making the user login again,
    ///   the client sends the refresh token to get a NEW access token.
    ///   This keeps the user "logged in" for 30 days without re-entering credentials.
    /// 
    /// HOW IT WORKS:
    ///   1. User logs in → gets access token (30 min) + refresh token (30 days)
    ///   2. Access token expires → client calls POST /api/auth/refresh with the refresh token
    ///   3. Server validates the refresh token → issues a NEW access token + NEW refresh token
    ///   4. Old refresh token is revoked (can't be reused) — this is called "token rotation"
    ///   5. After 30 days, refresh token expires → user must login again
    /// </summary>
    public class RefreshToken
    {
        /// <summary>Primary key.</summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The actual refresh token value — a cryptographically random Base64 string.
        /// This is what the client stores and sends back to get a new access token.
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Foreign key to the user who owns this refresh token.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Navigation property to the user.
        /// </summary>
        public ApplicationUser User { get; set; } = null!;

        /// <summary>When this refresh token expires (30 days from creation).</summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>When this refresh token was created.</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When this refresh token was revoked (invalidated).
        /// Null = still active. Set when:
        ///   - User logs out (all tokens revoked)
        ///   - Token is "rotated" (old token revoked when new one is issued)
        /// </summary>
        public DateTime? RevokedAt { get; set; }

        // ── Computed Properties ──────────────────────────────────────

        /// <summary>True if this token has been explicitly revoked.</summary>
        public bool IsRevoked => RevokedAt != null;

        /// <summary>True if this token's expiry date has passed.</summary>
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

        /// <summary>True if the token can still be used (not revoked AND not expired).</summary>
        public bool IsActive => !IsRevoked && !IsExpired;
    }
}

using Microsoft.AspNetCore.Identity;

namespace Gradely.Domain.Entities
{
    /// <summary>
    /// Extends the built-in IdentityUser with Gradely-specific properties.
    /// IdentityUser already gives us: Id, UserName, Email, PasswordHash, PhoneNumber, etc.
    /// We only add what's extra for our app.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        /// <summary>
        /// The user's full display name (e.g. "Moaaz Ahmed").
        /// </summary>
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// When the account was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

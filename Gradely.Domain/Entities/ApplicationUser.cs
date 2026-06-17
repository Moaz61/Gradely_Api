using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

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

        /// <summary>
        /// Whether the user has been verified by an admin.
        /// Primarily used for Teacher accounts — an admin marks a teacher
        /// as verified via PUT /api/admin/teachers/{id}/verify.
        /// </summary>
        public bool IsVerified { get; set; } = false;

        /// <summary>
        /// Soft-delete flag. When true the user is considered deleted
        /// but the record is retained for data integrity.
        /// Active hard-delete endpoints clean up related data first, then
        /// call Identity DeleteAsync — this flag exists for future use /
        /// audit trails.
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// Self-referential FK: the ID of the Teacher (ApplicationUser) assigned
        /// to this Student. NULL if no teacher has been assigned yet.
        /// Only relevant on Student accounts.
        /// Set by Admin via PUT /api/admin/users/{id}/assign-teacher.
        /// </summary>
        public string? TeacherId { get; set; }

        // ── Navigation Properties ─────────────────────────────────────

        /// <summary>
        /// The teacher assigned to this student (many students → one teacher).
        /// Navigation property for the self-referential TeacherId FK.
        /// </summary>
        [ForeignKey(nameof(TeacherId))]
        public ApplicationUser? Teacher { get; set; }

        /// <summary>
        /// Students assigned to this teacher (one teacher → many students).
        /// Inverse navigation of the TeacherId self-referential relationship.
        /// </summary>
        [InverseProperty(nameof(Teacher))]
        public ICollection<ApplicationUser> Students { get; set; } = new List<ApplicationUser>();
    }
}

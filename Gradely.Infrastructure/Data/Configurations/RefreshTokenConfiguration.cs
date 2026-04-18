using Gradely.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gradely.Infrastructure.Data.Configurations
{
    /// <summary>
    /// EF Core Fluent API configuration for the RefreshToken entity.
    /// 
    /// WHY A SEPARATE CONFIG CLASS?
    ///   Same pattern as the other entity configs — keeps AppDbContext clean.
    ///   EF Core discovers this via ApplyConfigurationsFromAssembly() in AppDbContext.
    /// 
    /// WHAT WE CONFIGURE:
    ///   1. Primary key
    ///   2. Required fields + max lengths
    ///   3. Index on Token column (for fast lookups when client sends refresh token)
    ///   4. Index on UserId (for revoking all tokens on logout)
    ///   5. Foreign key relationship to ApplicationUser
    ///   6. Ignore computed properties (IsRevoked, IsExpired, IsActive — not stored in DB)
    /// </summary>
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            // ── Primary Key ──
            builder.HasKey(rt => rt.Id);

            // ── Token column ──
            // The token is a Base64 string (~44 chars), but we allow 256 for safety.
            builder.Property(rt => rt.Token)
                .IsRequired()
                .HasMaxLength(256);

            // ── UserId column ──
            builder.Property(rt => rt.UserId)
                .IsRequired();

            // ── Indexes ──
            // Fast lookup when client sends a refresh token to /api/auth/refresh
            builder.HasIndex(rt => rt.Token)
                .IsUnique();

            // Fast lookup when revoking all tokens for a user on logout
            builder.HasIndex(rt => rt.UserId);

            // ── Relationship ──
            // Each RefreshToken belongs to one ApplicationUser.
            // A user can have multiple refresh tokens (e.g. logged in on phone + laptop).
            builder.HasOne(rt => rt.User)
                .WithMany()
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Delete tokens when user is deleted

            // ── Ignore computed properties ──
            // These are calculated in C# code, not stored in the database.
            builder.Ignore(rt => rt.IsRevoked);
            builder.Ignore(rt => rt.IsExpired);
            builder.Ignore(rt => rt.IsActive);
        }
    }
}

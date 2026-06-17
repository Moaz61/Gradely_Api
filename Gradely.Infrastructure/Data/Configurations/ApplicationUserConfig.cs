using Gradely.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gradely.Infrastructure.Data.Configurations
{
    /// <summary>
    /// EF Core configuration for the ApplicationUser entity.
    ///
    /// Configures:
    ///   - FullName: required, max 100 chars
    ///   - CreatedAt: default = GETUTCDATE()
    ///   - IsVerified: default = false
    ///   - IsDeleted: default = false
    ///   - Self-referential Teacher → Students relationship via TeacherId FK
    /// </summary>
    public class ApplicationUserConfig : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            // FullName is required and has a max length
            builder.Property(u => u.FullName)
                   .IsRequired()
                   .HasMaxLength(100);

            // CreatedAt defaults to the current UTC time at the database level
            builder.Property(u => u.CreatedAt)
                   .HasDefaultValueSql("GETUTCDATE()");

            // IsVerified defaults to false — admin can verify teacher accounts
            builder.Property(u => u.IsVerified)
                   .HasDefaultValue(false);

            // IsDeleted defaults to false — used for soft-delete tracking
            builder.Property(u => u.IsDeleted)
                   .HasDefaultValue(false);

            // ── Self-referential Teacher → Student relationship ──────────
            // One Teacher (ApplicationUser) can have many Students (ApplicationUser).
            // A Student has a nullable TeacherId FK pointing to their assigned teacher.
            // When a teacher is deleted, assigned students have their TeacherId set to null
            // (cascading is handled programmatically in AdminService to avoid EF limitations
            //  with self-referential cascade deletes on SQL Server).
            builder.HasOne(u => u.Teacher)
                   .WithMany(u => u.Students)
                   .HasForeignKey(u => u.TeacherId)
                   .IsRequired(false)
                   .OnDelete(DeleteBehavior.Restrict); // No DB-level cascade; handled in code
        }
    }
}

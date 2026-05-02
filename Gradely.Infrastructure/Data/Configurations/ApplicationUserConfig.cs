using Gradely.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gradely.Infrastructure.Data.Configurations
{
    /// <summary>
    /// EF Core configuration for the ApplicationUser entity.
    /// 
    /// WHAT IS IEntityTypeConfiguration?
    ///   Instead of putting all entity config inside AppDbContext.OnModelCreating
    ///   (which gets HUGE as you add more entities), we split each entity's config
    ///   into its own file. This is called the "Configuration Class" pattern.
    /// 
    /// HOW DOES EF CORE FIND THIS?
    ///   In AppDbContext.OnModelCreating, we call:
    ///     builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    ///   EF Core scans the assembly for ALL classes that implement 
    ///   IEntityTypeConfiguration<T> and applies them automatically.
    ///   No manual registration needed — just create the class and it works!
    /// 
    /// WHY THIS PATTERN?
    ///   1. Each entity's config is in ONE file — easy to find and maintain
    ///   2. AppDbContext stays clean — no 200-line OnModelCreating
    ///   3. Each config class is independently testable
    ///   4. When your team adds a new entity, they just add a new Config class
    /// </summary>
    public class ApplicationUserConfig : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            // FullName is required and has a max length
            builder.Property(u => u.FullName)
                   .IsRequired()
                   .HasMaxLength(100);

            // CreatedAt has a default value set by the database
            builder.Property(u => u.CreatedAt)
                   .HasDefaultValueSql("GETUTCDATE()");

            // IsVerified defaults to false — admin can verify teacher accounts
            builder.Property(u => u.IsVerified)
                   .HasDefaultValue(false);
        }
    }
}

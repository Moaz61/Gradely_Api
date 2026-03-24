using Gradely.Domain.Entities;
using Gradely.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Gradely.Infrastructure.Data
{
    /// <summary>
    /// The main EF Core database context for Gradely.
    /// 
    /// WHY IdentityDbContext<ApplicationUser>?
    /// — IdentityDbContext automatically creates all the tables ASP.NET Identity needs:
    ///   AspNetUsers, AspNetRoles, AspNetUserRoles, AspNetUserClaims, etc.
    ///   We just add our own DbSets for custom entities (Courses, Assignments, etc. later).
    /// 
    /// HOW IT WORKS:
    ///   EF Core reads this class and builds a "model" of your database.
    ///   Each DbSet<T> = one table in the database.
    ///   OnModelCreating = where you configure relationships, seeds, constraints.
    /// </summary>
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        // ── Constructor ──────────────────────────────────────────────
        // DbContextOptions carries the connection string + provider config.
        // It's injected via Dependency Injection from Program.cs.
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // ── Future DbSets will go here ──────────────────────────────
        // public DbSet<Course> Courses { get; set; }
        // public DbSet<Assignment> Assignments { get; set; }
        // public DbSet<Submission> Submissions { get; set; }
        // public DbSet<Report> Reports { get; set; }

        // ── Model Configuration ─────────────────────────────────────
        protected override void OnModelCreating(ModelBuilder builder)
        {
            // IMPORTANT: Always call base.OnModelCreating first!
            // This lets IdentityDbContext configure all its Identity tables.
            // If you skip this, Identity tables won't be created.
            base.OnModelCreating(builder);

            // ── Configure ApplicationUser table ──
            builder.Entity<ApplicationUser>(entity =>
            {
                // FullName is required and has a max length
                entity.Property(u => u.FullName)
                      .IsRequired()
                      .HasMaxLength(100);

                // CreatedAt has a default value set by the database
                entity.Property(u => u.CreatedAt)
                      .HasDefaultValueSql("GETUTCDATE()");
            });

            // ── Seed the 3 roles into the database ──
            // This means when you run the migration, these roles are automatically inserted.
            // The IDs are fixed GUIDs so migrations are repeatable (same ID every time).
            builder.Entity<IdentityRole>().HasData(
                new IdentityRole
                {
                    Id = "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
                    Name = UserRole.Student.ToString(),
                    NormalizedName = UserRole.Student.ToString().ToUpper()
                },
                new IdentityRole
                {
                    Id = "b2c3d4e5-f6a7-8901-bcde-f12345678901",
                    Name = UserRole.Teacher.ToString(),
                    NormalizedName = UserRole.Teacher.ToString().ToUpper()
                },
                new IdentityRole
                {
                    Id = "c3d4e5f6-a7b8-9012-cdef-123456789012",
                    Name = UserRole.Admin.ToString(),
                    NormalizedName = UserRole.Admin.ToString().ToUpper()
                }
            );
        }
    }
}

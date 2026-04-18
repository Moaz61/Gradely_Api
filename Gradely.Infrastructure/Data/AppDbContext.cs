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

        // ── DbSets ───────────────────────────────────────────────────
        // Each DbSet<T> maps to one table in the database.
        // EF Core uses the property name as the table name by default.
        //   DbSet<Assignment> Assignments  →  [Assignments] table
        //   DbSet<Submission> Submissions  →  [Submissions] table
        //   DbSet<Report>     Reports      →  [Reports] table

        /// <summary>
        /// The Assignments table — tasks created for students to complete.
        /// </summary>
        public DbSet<Assignment> Assignments { get; set; }

        /// <summary>
        /// The Submissions table — student PDF uploads linked to assignments.
        /// </summary>
        public DbSet<Submission> Submissions { get; set; }

        /// <summary>
        /// The Reports table — ML-generated grading results for submissions.
        /// </summary>
        public DbSet<Report> Reports { get; set; }

        /// <summary>
        /// The RefreshTokens table — stores refresh tokens for JWT token rotation.
        /// </summary>
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        // ── Model Configuration ─────────────────────────────────────
        protected override void OnModelCreating(ModelBuilder builder)
        {
            // IMPORTANT: Always call base.OnModelCreating first!
            // This lets IdentityDbContext configure all its Identity tables.
            // If you skip this, Identity tables won't be created.
            base.OnModelCreating(builder);

            // ── Apply all entity configurations from the Configurations folder ──
            // This single line replaces hundreds of lines of inline Fluent API config.
            // 
            // HOW IT WORKS:
            //   1. EF Core scans this assembly (Gradely.Infrastructure) 
            //   2. Finds ALL classes that implement IEntityTypeConfiguration<T>
            //      (ApplicationUserConfig, AssignmentConfig, SubmissionConfig, ReportConfig)
            //   3. Calls their Configure() method automatically
            //
            // WHY THIS PATTERN?
            //   - AppDbContext stays clean and short
            //   - Each entity's config is in its own file (Data/Configurations/)
            //   - Adding a new entity = just add a new Config class, no changes here
            //   - Easy for team members to find and edit configs independently
            builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

            // ── Seed the 3 roles into the database ──
            // This stays here (not in a Config class) because role seeding
            // is database seed data, not entity configuration.
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

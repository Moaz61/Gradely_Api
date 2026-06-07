using Gradely.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gradely.Infrastructure.Data.Configurations
{
    /// <summary>
    /// EF Core configuration for the Assignment entity.
    /// 
    /// CONFIGURES:
    ///   - Primary key (Guid, auto-generated)
    ///   - Title: required, max 200 chars
    ///   - CreatedAt: defaults to UTC now
    /// 
    /// WHY USE FLUENT API AND NOT JUST DATA ANNOTATIONS?
    ///   Data annotations ([Required], [MaxLength]) handle simple cases.
    ///   But for complex config (default SQL values, composite indexes),
    ///   Fluent API is more powerful. We use BOTH for extra safety:
    ///   annotations on the entity + Fluent API here.
    /// </summary>
    public class AssignmentConfig : IEntityTypeConfiguration<Assignment>
    {
        public void Configure(EntityTypeBuilder<Assignment> builder)
        {
            // Primary key — EF Core already detects "Id" as PK,
            // but being explicit is good documentation.
            builder.HasKey(a => a.Id);

            // Auto-generate a new Guid when inserting a row.
            // NEWID() is SQL Server's function to create a random GUID.
            // ValueGeneratedOnAdd = the database generates it, not your C# code.
            builder.Property(a => a.Id)
                   .HasDefaultValueSql("NEWID()");

            // Title: required, max 200 chars
            builder.Property(a => a.Title)
                   .IsRequired()
                   .HasMaxLength(200);

            // CreatedAt: auto-set to UTC when the row is inserted
            builder.Property(a => a.CreatedAt)
                   .HasDefaultValueSql("GETUTCDATE()");

            // Relationship: Assignment → Teacher (many-to-one)
            // A teacher can create many assignments.
            // On delete: SET NULL (when a teacher is deleted, TeacherId becomes NULL,
            // but the assignments remain intact in the database).
            builder.HasOne(a => a.Teacher)
                   .WithMany()
                   .HasForeignKey(a => a.TeacherId)
                   .OnDelete(DeleteBehavior.SetNull);
        }
    }
}

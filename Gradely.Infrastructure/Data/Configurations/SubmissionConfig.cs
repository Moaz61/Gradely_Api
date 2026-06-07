using Gradely.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gradely.Infrastructure.Data.Configurations
{
    /// <summary>
    /// EF Core configuration for the Submission entity.
    /// 
    /// This is the most complex configuration because Submission has:
    ///   1. Two foreign keys (AssignmentId + StudentId)
    ///   2. A unique constraint (one submission per student per assignment)
    ///   3. Different delete behaviors for each FK
    /// 
    /// CONFIGURES:
    ///   - Primary key (Guid, auto-generated)
    ///   - Unique index on (AssignmentId, StudentId) — prevents duplicates
    ///   - FK to Assignment with CASCADE delete
    ///   - FK to Student with RESTRICT delete
    ///   - Property constraints (FilePath, OriginalFileName, SubmittedAt)
    /// </summary>
    public class SubmissionConfig : IEntityTypeConfiguration<Submission>
    {
        public void Configure(EntityTypeBuilder<Submission> builder)
        {
            builder.HasKey(s => s.Id);

            builder.Property(s => s.Id)
                   .HasDefaultValueSql("NEWID()");

            // ── UNIQUE CONSTRAINT: One active submission per student per assignment ──
            // HasIndex creates a database index on these two columns.
            // IsUnique makes it a UNIQUE constraint.
            // HasFilter ensures the constraint is only enforced for active students
            // (i.e. StudentId is not NULL). This allows multiple submissions from deleted students.
            builder.HasIndex(s => new { s.AssignmentId, s.StudentId })
                   .IsUnique()
                   .HasFilter("[StudentId] IS NOT NULL")
                   .HasDatabaseName("IX_Submission_Assignment_Student");

            // ── Relationship: Submission → Assignment (many-to-one) ──
            // HasOne = this Submission has ONE Assignment
            // WithMany = that Assignment has MANY Submissions
            // HasForeignKey = the FK column in the Submissions table
            // OnDelete = what happens when the Assignment is deleted
            //
            // CASCADE = if you delete an Assignment,
            //   all its Submissions are automatically deleted too.
            //   This makes sense: no assignment = no submissions.
            builder.HasOne(s => s.Assignment)
                   .WithMany(a => a.Submissions)
                   .HasForeignKey(s => s.AssignmentId)
                   .OnDelete(DeleteBehavior.Cascade);

            // ── Relationship: Submission → Student (many-to-one) ──
            // SET NULL = when a Student is deleted, their StudentId is set to NULL,
            //   but the Submission record (and academic file) remains in the DB.
            builder.HasOne(s => s.Student)
                   .WithMany()    // ApplicationUser doesn't have a Submissions collection
                   .HasForeignKey(s => s.StudentId)
                   .OnDelete(DeleteBehavior.SetNull);

            // FileName has a max length to prevent absurdly long names
            builder.Property(s => s.OriginalFileName)
                   .IsRequired()
                   .HasMaxLength(255);

            // FilePath is required (every submission must have a file)
            builder.Property(s => s.FilePath)
                   .IsRequired();

            // SubmittedAt auto-set to UTC when inserted
            builder.Property(s => s.SubmittedAt)
                   .HasDefaultValueSql("GETUTCDATE()");
        }
    }
}

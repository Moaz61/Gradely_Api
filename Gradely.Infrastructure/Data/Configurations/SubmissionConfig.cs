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

            // ── UNIQUE CONSTRAINT: One submission per student per assignment ──
            // HasIndex creates a database index on these two columns.
            // IsUnique makes it a UNIQUE constraint.
            // 
            // WHY?
            //   Without this, a student could submit multiple times to the
            //   same assignment, which would break our grading flow.
            //   The DB will reject duplicate (AssignmentId + StudentId) pairs
            //   with a clear SQL error, which we catch in the service layer.
            builder.HasIndex(s => new { s.AssignmentId, s.StudentId })
                   .IsUnique()
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
            // RESTRICT = you can NOT delete a User who has submissions.
            //   This prevents accidental data loss.
            //   If you need to delete a user, delete their submissions first.
            //
            // WHY RESTRICT and not CASCADE?
            //   Deleting a user shouldn't silently delete all their academic records.
            //   That would be a data integrity nightmare.
            builder.HasOne(s => s.Student)
                   .WithMany()    // ApplicationUser doesn't have a Submissions collection
                   .HasForeignKey(s => s.StudentId)
                   .OnDelete(DeleteBehavior.Restrict);

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

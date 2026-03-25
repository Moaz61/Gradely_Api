using Gradely.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gradely.Infrastructure.Data.Configurations
{
    /// <summary>
    /// EF Core configuration for the Report entity.
    /// 
    /// CONFIGURES:
    ///   - Primary key (Guid, auto-generated)
    ///   - One-to-one relationship with Submission
    ///   - Feedback: required, NVARCHAR(MAX)
    ///   - MistakesJson: required, defaults to "[]" (empty JSON array)
    ///   - CreatedAt: defaults to UTC now
    /// </summary>
    public class ReportConfig : IEntityTypeConfiguration<Report>
    {
        public void Configure(EntityTypeBuilder<Report> builder)
        {
            builder.HasKey(r => r.Id);

            builder.Property(r => r.Id)
                   .HasDefaultValueSql("NEWID()");

            // ── Relationship: Report → Submission (one-to-one) ──
            // HasOne + WithOne = one-to-one relationship.
            // HasForeignKey = Report.SubmissionId is the FK.
            //
            // ONE-TO-ONE means:
            //   Each Submission can have at most ONE Report.
            //   Each Report belongs to exactly ONE Submission.
            //   EF Core enforces this via a unique FK constraint.
            //
            // CASCADE = if a Submission is deleted, its Report is deleted too.
            builder.HasOne(r => r.Submission)
                   .WithOne(s => s.Report)
                   .HasForeignKey<Report>(r => r.SubmissionId)
                   .OnDelete(DeleteBehavior.Cascade);

            // Feedback can be long text (NVARCHAR(MAX))
            builder.Property(r => r.Feedback)
                   .IsRequired();

            // MistakesJson stored as NVARCHAR(MAX) — can hold large JSON
            builder.Property(r => r.MistakesJson)
                   .IsRequired()
                   .HasDefaultValue("[]");

            // CreatedAt auto-set to UTC
            builder.Property(r => r.CreatedAt)
                   .HasDefaultValueSql("GETUTCDATE()");
        }
    }
}

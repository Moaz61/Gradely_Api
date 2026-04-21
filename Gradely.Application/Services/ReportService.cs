using System.Text.Json;
using Gradely.Application.DTOs.Submissions;
using Gradely.Domain.Entities;
using Gradely.Domain.Enums;
using Gradely.Domain.Interfaces;

namespace Gradely.Application.Services
{
    public class ReportService : IReportService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ReportService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<(bool Succeeded, object? Data, string? Error)> SaveReportAsync(
            Guid submissionId, object dto)
        {
            // Cast from object → concrete DTO (same pattern used in SubmissionService)
            var reportDto = dto as CreateReportDto;
            if (reportDto == null)
                return (false, null, "Invalid report data.");

            // Step 1: Check the submission exists
            var submission = await _unitOfWork.Submissions.GetByIdAsync(submissionId);
            if (submission == null)
                return (false, null, "Submission not found.");

            // Step 2: Check a report doesn't already exist for this submission
            var existing = await _unitOfWork.Reports.FindAsync(r => r.SubmissionId == submissionId);
            if (existing.Any())
                return (false, null, "A report already exists for this submission.");

            // Step 3: Serialize the mistakes list → JSON string for storage
            // The DB stores it as a raw string. We convert it back when reading.
            var mistakesJson = JsonSerializer.Serialize(reportDto.Mistakes);

            // Step 4: Create the Report entity
            var report = new Report
            {
                SubmissionId = submissionId,
                Grade = reportDto.Grade,
                Feedback = reportDto.Feedback,
                MistakesJson = mistakesJson,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Reports.AddAsync(report);

            // Step 5: Update the submission status from Submitted → Graded
            submission.Status = SubmissionStatus.Graded;
            _unitOfWork.Submissions.Update(submission);

            // Step 6: Commit both changes to the database in one transaction
            // If the DB save fails, neither change is applied (atomicity)
            await _unitOfWork.CompleteAsync();

            return (true, new { reportId = report.Id, submissionId, grade = report.Grade }, null);
        }
    }
}

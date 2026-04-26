using Gradely.Application.DTOs.Student;
using Gradely.Domain.Enums;
using Gradely.Domain.Interfaces;

namespace Gradely.Application.Services
{
    /// <summary>
    /// Builds the student dashboard summary.
    ///
    /// AverageGrade is across the student's GRADED submissions only,
    /// expressed as a percentage of each assignment's MaxGrade. A student
    /// with no graded submissions yet gets null (not 0).
    /// </summary>
    public class StudentService : IStudentService
    {
        private readonly IUnitOfWork _unitOfWork;

        public StudentService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<(bool Succeeded, object? Data, string? Error)> GetDashboardAsync(string studentId)
        {
            var assignments = (await _unitOfWork.Assignments.GetAllAsync()).ToList();
            var mySubmissions = (await _unitOfWork.Submissions
                .FindAsync(s => s.StudentId == studentId)).ToList();

            int submittedCount = mySubmissions.Count;
            int pendingCount = assignments.Count - submittedCount;
            if (pendingCount < 0) pendingCount = 0;

            // Compute average percentage across the student's graded submissions.
            double percentSum = 0;
            int gradedCount = 0;

            foreach (var sub in mySubmissions)
            {
                if (sub.Status != SubmissionStatus.Graded) continue;

                var reports = await _unitOfWork.Reports
                    .FindAsync(r => r.SubmissionId == sub.Id);
                var report = reports.FirstOrDefault();
                if (report == null) continue;

                var assignment = assignments.FirstOrDefault(a => a.Id == sub.AssignmentId);
                if (assignment == null || assignment.MaxGrade <= 0) continue;

                percentSum += (double)report.Grade / assignment.MaxGrade * 100.0;
                gradedCount++;
            }

            var dto = new StudentDashboardDto
            {
                TotalAssignments = assignments.Count,
                SubmittedCount = submittedCount,
                PendingCount = pendingCount,
                AverageGrade = gradedCount == 0
                    ? null
                    : Math.Round(percentSum / gradedCount, 2)
            };

            return (true, dto, null);
        }
    }
}

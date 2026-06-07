using System.Text.Json;
using Gradely.Application.DTOs.Assignments;
using Gradely.Application.DTOs.Submissions;
using Gradely.Application.DTOs.Teacher;
using Gradely.Domain.Entities;
using Gradely.Domain.Enums;
using Gradely.Domain.Interfaces;

namespace Gradely.Application.Services
{
    /// <summary>
    /// Teacher-facing read operations: list assignments, view all
    /// submissions for an assignment, view any student's report,
    /// and aggregated stats.
    ///
    /// Reuses ReportDto / AssignmentDto from the existing layers so
    /// teachers and students see the same shape of data where it makes
    /// sense.
    /// </summary>
    public class TeacherService : ITeacherService
    {
        private readonly IUnitOfWork _unitOfWork;

        public TeacherService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // ── GET /api/teacher/assignments ──────────────────────────────
        public async Task<(bool Succeeded, object? Data, string? Error)> GetAllAssignmentsAsync()
        {
            var assignments = await _unitOfWork.Assignments.GetAllAsync();

            var dtos = assignments.Select(a => new AssignmentDto
            {
                Id = a.Id,
                Title = a.Title,
                Description = a.Description,
                DueDate = a.DueDate,
                MaxGrade = a.MaxGrade,
                CreatedAt = a.CreatedAt
            }).ToList();

            return (true, dtos, null);
        }

        // ── GET /api/teacher/assignments/{id}/submissions ─────────────
        public async Task<(bool Succeeded, object? Data, string? Error)> GetSubmissionsForAssignmentAsync(
            Guid assignmentId)
        {
            var assignment = await _unitOfWork.Assignments.GetByIdAsync(assignmentId);
            if (assignment == null)
                return (false, null, "Assignment not found.");

            var submissions = await _unitOfWork.Submissions
                .FindAsync(s => s.AssignmentId == assignmentId);

            var result = new List<TeacherSubmissionDto>();
            foreach (var submission in submissions)
            {
                // Look up the student so we can show name + email.
                ApplicationUser? student = null;
                if (!string.IsNullOrEmpty(submission.StudentId))
                {
                    student = await _unitOfWork.Users.GetByIdAsync(submission.StudentId);
                }

                // Look up the report (may be null if not graded yet).
                var reports = await _unitOfWork.Reports
                    .FindAsync(r => r.SubmissionId == submission.Id);
                var report = reports.FirstOrDefault();

                result.Add(new TeacherSubmissionDto
                {
                    Id = submission.Id,
                    AssignmentId = submission.AssignmentId,
                    AssignmentTitle = assignment.Title,
                    StudentId = submission.StudentId,
                    StudentName = student?.FullName ?? "Unknown Student",
                    StudentEmail = student?.Email ?? string.Empty,
                    OriginalFileName = submission.OriginalFileName,
                    Status = submission.Status.ToString(),
                    SubmittedAt = submission.SubmittedAt,
                    Grade = report?.Grade
                });
            }

            return (true, result, null);
        }

        // ── GET /api/teacher/submissions/{id}/report ──────────────────
        public async Task<(bool Succeeded, object? Data, string? Error)> GetSubmissionReportAsync(
            Guid submissionId)
        {
            var submission = await _unitOfWork.Submissions.GetByIdAsync(submissionId);
            if (submission == null)
                return (false, null, "Submission not found.");

            var reports = await _unitOfWork.Reports
                .FindAsync(r => r.SubmissionId == submissionId);
            var report = reports.FirstOrDefault();

            if (report == null)
                return (false, null, "No report available yet for this submission.");

            var mistakes = DeserializeMistakes(report.MistakesJson);

            var dto = new ReportDto
            {
                Grade = report.Grade,
                Feedback = report.Feedback,
                Mistakes = mistakes,
                CreatedAt = report.CreatedAt
            };

            return (true, dto, null);
        }

        // ── GET /api/teacher/stats ────────────────────────────────────
        public async Task<(bool Succeeded, object? Data, string? Error)> GetStatsAsync()
        {
            var assignments = (await _unitOfWork.Assignments.GetAllAsync()).ToList();
            var submissions = (await _unitOfWork.Submissions.GetAllAsync()).ToList();
            var reports = (await _unitOfWork.Reports.GetAllAsync()).ToList();

            // Map SubmissionId → Report for fast lookup.
            var reportBySubmission = reports.ToDictionary(r => r.SubmissionId);

            var distribution = new GradeDistributionDto();
            var perAssignment = new List<AssignmentStatsDto>();
            double totalPercentSum = 0;
            int totalGraded = 0;

            foreach (var assignment in assignments)
            {
                var assignmentSubs = submissions
                    .Where(s => s.AssignmentId == assignment.Id)
                    .ToList();

                int gradedForAssignment = 0;
                double percentSumForAssignment = 0;

                foreach (var sub in assignmentSubs)
                {
                    if (sub.Status != SubmissionStatus.Graded) continue;
                    if (!reportBySubmission.TryGetValue(sub.Id, out var report)) continue;
                    if (assignment.MaxGrade <= 0) continue; // avoid divide-by-zero

                    var percent = (double)report.Grade / assignment.MaxGrade * 100.0;

                    gradedForAssignment++;
                    percentSumForAssignment += percent;

                    totalGraded++;
                    totalPercentSum += percent;

                    BumpDistribution(distribution, percent);
                }

                perAssignment.Add(new AssignmentStatsDto
                {
                    AssignmentId = assignment.Id,
                    Title = assignment.Title,
                    MaxGrade = assignment.MaxGrade,
                    SubmissionCount = assignmentSubs.Count,
                    GradedCount = gradedForAssignment,
                    AveragePercent = gradedForAssignment == 0
                        ? null
                        : Math.Round(percentSumForAssignment / gradedForAssignment, 2)
                });
            }

            var stats = new StatsDto
            {
                TotalAssignments = assignments.Count,
                TotalSubmissions = submissions.Count,
                GradedCount = totalGraded,
                PendingCount = submissions.Count - totalGraded,
                OverallAveragePercent = totalGraded == 0
                    ? null
                    : Math.Round(totalPercentSum / totalGraded, 2),
                PerAssignment = perAssignment,
                GradeDistribution = distribution
            };

            return (true, stats, null);
        }

        // ── POST /api/teacher/assignments ─────────────────────────────
        /// <summary>
        /// Creates a new Assignment entity from the DTO and saves it to the database.
        /// Returns the created assignment as an AssignmentDto.
        /// </summary>
        public async Task<(bool Succeeded, object? Data, string? Error)> CreateAssignmentAsync(object dto, string teacherId)
        {
            var createDto = dto as CreateAssignmentDto;
            if (createDto == null)
                return (false, null, "Invalid assignment data.");

            var assignment = new Assignment
            {
                Id = Guid.NewGuid(),
                Title = createDto.Title,
                Description = createDto.Description,
                DueDate = createDto.DueDate,
                MaxGrade = createDto.MaxGrade,
                TeacherId = teacherId,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Assignments.AddAsync(assignment);
            await _unitOfWork.CompleteAsync();

            var responseDto = new AssignmentDto
            {
                Id = assignment.Id,
                Title = assignment.Title,
                Description = assignment.Description,
                DueDate = assignment.DueDate,
                MaxGrade = assignment.MaxGrade,
                CreatedAt = assignment.CreatedAt
            };

            return (true, responseDto, null);
        }

        // ── PUT /api/teacher/assignments/{id} ─────────────────────────
        /// <summary>
        /// Updates an existing assignment's fields.
        /// Returns the updated assignment as an AssignmentDto.
        /// </summary>
        public async Task<(bool Succeeded, object? Data, string? Error)> UpdateAssignmentAsync(Guid id, object dto)
        {
            var updateDto = dto as UpdateAssignmentDto;
            if (updateDto == null)
                return (false, null, "Invalid assignment data.");

            var assignment = await _unitOfWork.Assignments.GetByIdAsync(id);
            if (assignment == null)
                return (false, null, "Assignment not found.");

            // Update the fields
            assignment.Title = updateDto.Title;
            assignment.Description = updateDto.Description;
            assignment.DueDate = updateDto.DueDate;
            assignment.MaxGrade = updateDto.MaxGrade;

            _unitOfWork.Assignments.Update(assignment);
            await _unitOfWork.CompleteAsync();

            var responseDto = new AssignmentDto
            {
                Id = assignment.Id,
                Title = assignment.Title,
                Description = assignment.Description,
                DueDate = assignment.DueDate,
                MaxGrade = assignment.MaxGrade,
                CreatedAt = assignment.CreatedAt
            };

            return (true, responseDto, null);
        }

        // ── DELETE /api/teacher/assignments/{id} ──────────────────────
        /// <summary>
        /// Deletes an assignment by ID.
        /// Safety: refuses to delete if the assignment has any submissions
        /// (to avoid orphaning student work).
        /// </summary>
        public async Task<(bool Succeeded, object? Data, string? Error)> DeleteAssignmentAsync(Guid id)
        {
            var assignment = await _unitOfWork.Assignments.GetByIdAsync(id);
            if (assignment == null)
                return (false, null, "Assignment not found.");

            // Safety check: don't delete if students have already submitted work
            var submissions = await _unitOfWork.Submissions
                .FindAsync(s => s.AssignmentId == id);

            if (submissions.Any())
                return (false, null, $"Cannot delete this assignment because it has {submissions.Count()} submission(s). Remove submissions first.");

            _unitOfWork.Assignments.Delete(assignment);
            await _unitOfWork.CompleteAsync();

            return (true, new { message = "Assignment deleted successfully." }, null);
        }

        // ── Helpers ───────────────────────────────────────────────────

        private static void BumpDistribution(GradeDistributionDto dist, double percent)
        {
            if (percent < 60) dist.Below60++;
            else if (percent < 70) dist.From60To69++;
            else if (percent < 80) dist.From70To79++;
            else if (percent < 90) dist.From80To89++;
            else dist.From90To100++;
        }

        private static List<MistakeDto> DeserializeMistakes(string mistakesJson)
        {
            if (string.IsNullOrEmpty(mistakesJson) || mistakesJson == "[]")
                return new List<MistakeDto>();

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<List<MistakeDto>>(mistakesJson, options)
                       ?? new List<MistakeDto>();
            }
            catch (JsonException)
            {
                return new List<MistakeDto>();
            }
        }
    }
}

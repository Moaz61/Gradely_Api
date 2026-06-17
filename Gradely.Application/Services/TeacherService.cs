using System.Text.Json;
using Gradely.Application.DTOs.Assignments;
using Gradely.Application.DTOs.Admin;
using Gradely.Application.DTOs.Submissions;
using Gradely.Application.DTOs.Teacher;
using Gradely.Domain.Entities;
using Gradely.Domain.Enums;
using Gradely.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Gradely.Application.Services
{
    /// <summary>
    /// Teacher-facing operations: list/manage assignments, view submissions,
    /// view reports, download files, view students, and aggregated stats.
    /// All data is scoped strictly to the logged-in teacher's assignments.
    /// </summary>
    public class TeacherService : ITeacherService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public TeacherService(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        // ── GET /api/teacher/assignments ──────────────────────────────
        public async Task<(bool Succeeded, object? Data, string? Error)> GetAllAssignmentsAsync(string teacherId)
        {
            var assignments = await _unitOfWork.Assignments.FindAsync(a => a.TeacherId == teacherId);

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

        // ── GET /api/teacher/assignments/{id} ─────────────────────────
        public async Task<(bool Succeeded, object? Data, string? Error)> GetAssignmentByIdAsync(Guid assignmentId, string teacherId)
        {
            var assignment = await _unitOfWork.Assignments.GetByIdAsync(assignmentId);
            if (assignment == null || assignment.TeacherId != teacherId)
                return (false, null, "Assignment not found or you do not have permission to view it.");

            var dto = new AssignmentDto
            {
                Id = assignment.Id,
                Title = assignment.Title,
                Description = assignment.Description,
                DueDate = assignment.DueDate,
                MaxGrade = assignment.MaxGrade,
                CreatedAt = assignment.CreatedAt
            };

            return (true, dto, null);
        }

        // ── POST /api/teacher/assignments ─────────────────────────────
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
        public async Task<(bool Succeeded, object? Data, string? Error)> UpdateAssignmentAsync(Guid id, object dto, string teacherId)
        {
            var updateDto = dto as UpdateAssignmentDto;
            if (updateDto == null)
                return (false, null, "Invalid assignment data.");

            var assignment = await _unitOfWork.Assignments.GetByIdAsync(id);
            if (assignment == null || assignment.TeacherId != teacherId)
                return (false, null, "Assignment not found or you do not have permission to update it.");

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
        /// Deletes an assignment and cascade-deletes all its submissions and reports.
        /// </summary>
        public async Task<(bool Succeeded, object? Data, string? Error)> DeleteAssignmentAsync(Guid id, string teacherId)
        {
            var assignment = await _unitOfWork.Assignments.GetByIdAsync(id);
            if (assignment == null || assignment.TeacherId != teacherId)
                return (false, null, "Assignment not found or you do not have permission to delete it.");

            // Cascade: delete all reports + submissions for this assignment
            var submissions = (await _unitOfWork.Submissions.FindAsync(s => s.AssignmentId == id)).ToList();
            foreach (var submission in submissions)
            {
                var reports = (await _unitOfWork.Reports.FindAsync(r => r.SubmissionId == submission.Id)).ToList();
                foreach (var report in reports)
                    _unitOfWork.Reports.Delete(report);

                _unitOfWork.Submissions.Delete(submission);
            }

            _unitOfWork.Assignments.Delete(assignment);
            await _unitOfWork.CompleteAsync();

            return (true, new { message = "Assignment deleted successfully." }, null);
        }

        // ── GET /api/teacher/assignments/{id}/submissions ─────────────
        public async Task<(bool Succeeded, object? Data, string? Error)> GetSubmissionsForAssignmentAsync(
            Guid assignmentId, string teacherId)
        {
            var assignment = await _unitOfWork.Assignments.GetByIdAsync(assignmentId);
            if (assignment == null || assignment.TeacherId != teacherId)
                return (false, null, "Assignment not found or you do not have permission to view its submissions.");

            var submissions = await _unitOfWork.Submissions.FindAsync(s => s.AssignmentId == assignmentId);

            var result = new List<TeacherSubmissionDto>();
            foreach (var submission in submissions)
            {
                ApplicationUser? student = null;
                if (!string.IsNullOrEmpty(submission.StudentId))
                    student = await _unitOfWork.Users.GetByIdAsync(submission.StudentId);

                var reports = await _unitOfWork.Reports.FindAsync(r => r.SubmissionId == submission.Id);
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

        // ── GET /api/teacher/assignments/{id}/student-status ──────────
        public async Task<(bool Succeeded, object? Data, string? Error)> GetAssignmentStudentStatusAsync(
            Guid assignmentId, string teacherId)
        {
            var assignment = await _unitOfWork.Assignments.GetByIdAsync(assignmentId);
            if (assignment == null || assignment.TeacherId != teacherId)
                return (false, null, "Assignment not found or you do not have permission to view it.");

            // Get all students assigned to this teacher
            var students = (await _unitOfWork.Users.FindAsync(u => u.TeacherId == teacherId)).ToList();

            // Get all submissions for this assignment
            var submissions = (await _unitOfWork.Submissions.FindAsync(s => s.AssignmentId == assignmentId)).ToList();

            var result = new List<StudentAssignmentStatusDto>();

            foreach (var student in students)
            {
                var submission = submissions.FirstOrDefault(s => s.StudentId == student.Id);
                int? grade = null;

                if (submission != null && submission.Status == SubmissionStatus.Graded)
                {
                    var reports = await _unitOfWork.Reports.FindAsync(r => r.SubmissionId == submission.Id);
                    grade = reports.FirstOrDefault()?.Grade;
                }

                result.Add(new StudentAssignmentStatusDto
                {
                    StudentId = student.Id,
                    StudentName = student.FullName,
                    StudentEmail = student.Email ?? string.Empty,
                    HasSubmitted = submission != null,
                    SubmittedAt = submission?.SubmittedAt,
                    Grade = grade,
                    Status = submission == null ? "Pending" : submission.Status.ToString()
                });
            }

            return (true, result, null);
        }

        // ── GET /api/teacher/submissions/{id} ─────────────────────────
        public async Task<(bool Succeeded, object? Data, string? Error)> GetSubmissionByIdAsync(
            Guid submissionId, string teacherId)
        {
            var submission = await _unitOfWork.Submissions.GetByIdAsync(submissionId);
            if (submission == null)
                return (false, null, "Submission not found.");

            var assignment = await _unitOfWork.Assignments.GetByIdAsync(submission.AssignmentId);
            if (assignment == null || assignment.TeacherId != teacherId)
                return (false, null, "Submission not found or you do not have permission to view it.");

            ApplicationUser? student = null;
            if (!string.IsNullOrEmpty(submission.StudentId))
                student = await _unitOfWork.Users.GetByIdAsync(submission.StudentId);

            var reports = await _unitOfWork.Reports.FindAsync(r => r.SubmissionId == submissionId);
            var report = reports.FirstOrDefault();

            var dto = new TeacherSubmissionDto
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
            };

            return (true, dto, null);
        }

        // ── GET /api/teacher/submissions/{id}/file ────────────────────
        public async Task<(bool Succeeded, string? FilePath, string? OriginalFileName, string? Error)> GetSubmissionFilePathAsync(
            Guid submissionId, string teacherId)
        {
            var submission = await _unitOfWork.Submissions.GetByIdAsync(submissionId);
            if (submission == null)
                return (false, null, null, "Submission not found.");

            var assignment = await _unitOfWork.Assignments.GetByIdAsync(submission.AssignmentId);
            if (assignment == null || assignment.TeacherId != teacherId)
                return (false, null, null, "Submission not found or you do not have permission to access this file.");

            return (true, submission.FilePath, submission.OriginalFileName, null);
        }

        // ── GET /api/teacher/submissions/{id}/report ──────────────────
        public async Task<(bool Succeeded, object? Data, string? Error)> GetSubmissionReportAsync(
            Guid submissionId, string teacherId)
        {
            var submission = await _unitOfWork.Submissions.GetByIdAsync(submissionId);
            if (submission == null)
                return (false, null, "Submission not found.");

            var assignment = await _unitOfWork.Assignments.GetByIdAsync(submission.AssignmentId);
            if (assignment == null || assignment.TeacherId != teacherId)
                return (false, null, "Submission report not found or you do not have permission to view it.");

            var reports = await _unitOfWork.Reports.FindAsync(r => r.SubmissionId == submissionId);
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

        // ── GET /api/teacher/students ─────────────────────────────────
        public async Task<(bool Succeeded, object? Data, string? Error)> GetStudentsAsync(string teacherId)
        {
            var students = await _unitOfWork.Users.FindAsync(u => u.TeacherId == teacherId);

            var dtos = students.Select(s => new
            {
                Id = s.Id,
                FullName = s.FullName,
                Email = s.Email,
                CreatedAt = s.CreatedAt
            }).ToList();

            return (true, dtos, null);
        }

        // ── GET /api/teacher/submissions/student/{studentId} ──────────
        public async Task<(bool Succeeded, object? Data, string? Error)> GetStudentSubmissionsAsync(
            string studentId, string teacherId)
        {
            // Verify the student is assigned to this teacher
            var student = await _unitOfWork.Users.GetByIdAsync(studentId);
            if (student == null || student.TeacherId != teacherId)
                return (false, null, "Student not found or not assigned to you.");

            var submissions = await _unitOfWork.Submissions.FindAsync(s => s.StudentId == studentId);

            var result = new List<TeacherSubmissionDto>();
            foreach (var submission in submissions)
            {
                var assignment = await _unitOfWork.Assignments.GetByIdAsync(submission.AssignmentId);
                var reports = await _unitOfWork.Reports.FindAsync(r => r.SubmissionId == submission.Id);
                var report = reports.FirstOrDefault();

                result.Add(new TeacherSubmissionDto
                {
                    Id = submission.Id,
                    AssignmentId = submission.AssignmentId,
                    AssignmentTitle = assignment?.Title ?? "Unknown Assignment",
                    StudentId = submission.StudentId,
                    StudentName = student.FullName,
                    StudentEmail = student.Email ?? string.Empty,
                    OriginalFileName = submission.OriginalFileName,
                    Status = submission.Status.ToString(),
                    SubmittedAt = submission.SubmittedAt,
                    Grade = report?.Grade
                });
            }

            return (true, result, null);
        }

        // ── GET /api/teacher/stats ────────────────────────────────────
        public async Task<(bool Succeeded, object? Data, string? Error)> GetStatsAsync(string teacherId)
        {
            var assignments = (await _unitOfWork.Assignments.FindAsync(a => a.TeacherId == teacherId)).ToList();

            var assignmentIds = assignments.Select(a => a.Id).ToHashSet();
            var submissions = (await _unitOfWork.Submissions.FindAsync(s => assignmentIds.Contains(s.AssignmentId))).ToList();

            var submissionIds = submissions.Select(s => s.Id).ToHashSet();
            var reports = (await _unitOfWork.Reports.FindAsync(r => submissionIds.Contains(r.SubmissionId))).ToList();

            var reportBySubmission = reports.ToDictionary(r => r.SubmissionId);

            var distribution = new GradeDistributionDto();
            var perAssignment = new List<AssignmentStatsDto>();
            double totalPercentSum = 0;
            int totalGraded = 0;

            foreach (var assignment in assignments)
            {
                var assignmentSubs = submissions.Where(s => s.AssignmentId == assignment.Id).ToList();

                int gradedForAssignment = 0;
                double percentSumForAssignment = 0;

                foreach (var sub in assignmentSubs)
                {
                    if (sub.Status != SubmissionStatus.Graded) continue;
                    if (!reportBySubmission.TryGetValue(sub.Id, out var report)) continue;
                    if (assignment.MaxGrade <= 0) continue;

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

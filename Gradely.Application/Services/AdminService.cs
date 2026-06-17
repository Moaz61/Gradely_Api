using Gradely.Application.DTOs.Admin;
using Gradely.Application.DTOs.Assignments;
using Gradely.Application.DTOs.Teacher;
using Gradely.Domain.Entities;
using Gradely.Domain.Enums;
using Gradely.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Gradely.Application.Services
{
    /// <summary>
    /// Admin-facing operations: manage users, verify teachers,
    /// delete accounts (with cascade), assign teachers to students,
    /// view all assignments, and view system-wide statistics.
    ///
    /// NOTE: Teachers now self-register via POST /api/auth/register.
    ///       The admin's role is to VERIFY teacher accounts, not create them.
    ///
    /// DEPENDENCIES:
    ///   - UserManager: ASP.NET Identity service to create/find/manage users + roles
    ///   - IUnitOfWork: access to Assignment/Submission/Report repos for cascade deletes + stats
    /// </summary>
    public class AdminService : IAdminService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;

        public AdminService(
            UserManager<ApplicationUser> userManager,
            IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }

        // ── GET /api/admin/users ──────────────────────────────────────
        /// <summary>
        /// Returns all non-admin users in the system with their role.
        /// Admin accounts are excluded from the list.
        /// </summary>
        public async Task<(bool Succeeded, object? Data, string? Error)> GetAllUsersAsync()
        {
            var users = await _userManager.Users.ToListAsync();
            var userDtos = new List<UserDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                // Skip admin users — they should not appear in the list
                if (roles.Contains(UserRole.Admin.ToString()))
                    continue;

                userDtos.Add(new UserDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? string.Empty,
                    Role = roles.FirstOrDefault() ?? "Student",
                    IsVerified = user.IsVerified,
                    CreatedAt = user.CreatedAt
                });
            }

            return (true, userDtos, null);
        }

        // ── DELETE /api/admin/students/{id} ───────────────────────────
        /// <summary>
        /// Deletes a student account.
        /// Programmatically deletes their submissions (and associated reports via cascade)
        /// before deleting the user.
        /// </summary>
        public async Task<(bool Succeeded, string? Error)> DeleteStudentAsync(string studentId)
        {
            var user = await _userManager.FindByIdAsync(studentId);
            if (user == null)
                return (false, "Student not found.");

            // Verify the user is actually a Student
            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains(UserRole.Student.ToString()))
                return (false, "This user is not a student.");

            // Step 1: Delete all submissions and their reports
            var submissions = (await _unitOfWork.Submissions.FindAsync(s => s.StudentId == studentId)).ToList();
            foreach (var submission in submissions)
            {
                // Delete associated report if it exists
                var reports = (await _unitOfWork.Reports.FindAsync(r => r.SubmissionId == submission.Id)).ToList();
                foreach (var report in reports)
                    _unitOfWork.Reports.Delete(report);

                _unitOfWork.Submissions.Delete(submission);
            }
            await _unitOfWork.CompleteAsync();

            // Step 2: Delete the user record
            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return (false, errors);
            }

            return (true, null);
        }

        // ── DELETE /api/admin/teachers/{id} ───────────────────────────
        /// <summary>
        /// Deletes a teacher account.
        /// Steps:
        ///   1. Unassign all students assigned to this teacher (set TeacherId = null)
        ///   2. For each assignment created by the teacher:
        ///      a. Delete all reports for each submission
        ///      b. Delete all submissions
        ///      c. Delete the assignment
        ///   3. Delete the teacher user record
        /// </summary>
        public async Task<(bool Succeeded, string? Error)> DeleteTeacherAsync(string teacherId)
        {
            var user = await _userManager.FindByIdAsync(teacherId);
            if (user == null)
                return (false, "Teacher not found.");

            // Verify the user is actually a Teacher
            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains(UserRole.Teacher.ToString()))
                return (false, "This user is not a teacher.");

            // Step 1: Unassign all students assigned to this teacher
            var assignedStudents = (await _unitOfWork.Users.FindAsync(u => u.TeacherId == teacherId)).ToList();
            foreach (var student in assignedStudents)
            {
                student.TeacherId = null;
                _unitOfWork.Users.Update(student);
            }
            await _unitOfWork.CompleteAsync();

            // Step 2: Delete all assignments (and cascade their submissions/reports)
            var assignments = (await _unitOfWork.Assignments.FindAsync(a => a.TeacherId == teacherId)).ToList();
            foreach (var assignment in assignments)
            {
                var submissions = (await _unitOfWork.Submissions.FindAsync(s => s.AssignmentId == assignment.Id)).ToList();
                foreach (var submission in submissions)
                {
                    var reports = (await _unitOfWork.Reports.FindAsync(r => r.SubmissionId == submission.Id)).ToList();
                    foreach (var report in reports)
                        _unitOfWork.Reports.Delete(report);

                    _unitOfWork.Submissions.Delete(submission);
                }
                _unitOfWork.Assignments.Delete(assignment);
            }
            await _unitOfWork.CompleteAsync();

            // Step 3: Delete the teacher user record
            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return (false, errors);
            }

            return (true, null);
        }

        // ── PUT /api/admin/teachers/{id}/verify ───────────────────────
        /// <summary>
        /// Marks a teacher account as verified (IsVerified = true).
        /// </summary>
        public async Task<(bool Succeeded, object? Data, string? Error)> VerifyTeacherAsync(string teacherId)
        {
            var user = await _userManager.FindByIdAsync(teacherId);
            if (user == null)
                return (false, null, "User not found.");

            // Confirm the user is actually a Teacher
            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains(UserRole.Teacher.ToString()))
                return (false, null, "This user is not a teacher.");

            if (user.IsVerified)
                return (false, null, "This teacher is already verified.");

            user.IsVerified = true;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return (false, null, errors);
            }

            var userDto = new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Role = UserRole.Teacher.ToString(),
                IsVerified = user.IsVerified,
                CreatedAt = user.CreatedAt
            };

            return (true, userDto, null);
        }

        // ── PUT /api/admin/users/{id}/assign-teacher ──────────────────
        /// <summary>
        /// Assigns a teacher to a student.
        /// Pass null teacherId to remove the current assignment.
        /// </summary>
        public async Task<(bool Succeeded, object? Data, string? Error)> AssignTeacherAsync(string studentId, string? teacherId)
        {
            var student = await _userManager.FindByIdAsync(studentId);
            if (student == null)
                return (false, null, "Student not found.");

            var studentRoles = await _userManager.GetRolesAsync(student);
            if (!studentRoles.Contains(UserRole.Student.ToString()))
                return (false, null, "This user is not a student.");

            // If assigning (not removing), validate the teacher exists and is verified
            if (teacherId != null)
            {
                var teacher = await _userManager.FindByIdAsync(teacherId);
                if (teacher == null)
                    return (false, null, "Teacher not found.");

                var teacherRoles = await _userManager.GetRolesAsync(teacher);
                if (!teacherRoles.Contains(UserRole.Teacher.ToString()))
                    return (false, null, "The specified user is not a teacher.");
            }

            student.TeacherId = teacherId;
            var result = await _userManager.UpdateAsync(student);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return (false, null, errors);
            }

            var userDto = new UserDto
            {
                Id = student.Id,
                FullName = student.FullName,
                Email = student.Email ?? string.Empty,
                Role = UserRole.Student.ToString(),
                IsVerified = student.IsVerified,
                CreatedAt = student.CreatedAt
            };

            return (true, userDto, null);
        }

        // ── GET /api/admin/assignments ────────────────────────────────
        /// <summary>
        /// Returns all assignments across the platform, including the teacher's name.
        /// </summary>
        public async Task<(bool Succeeded, object? Data, string? Error)> GetAllAssignmentsAsync()
        {
            var assignments = (await _unitOfWork.Assignments.GetAllAsync()).ToList();
            var dtos = new List<object>();

            foreach (var assignment in assignments)
            {
                string teacherName = "Unknown";
                if (!string.IsNullOrEmpty(assignment.TeacherId))
                {
                    var teacher = await _userManager.FindByIdAsync(assignment.TeacherId);
                    teacherName = teacher?.FullName ?? "Unknown";
                }

                dtos.Add(new
                {
                    assignment.Id,
                    assignment.Title,
                    assignment.Description,
                    assignment.DueDate,
                    assignment.MaxGrade,
                    assignment.CreatedAt,
                    TeacherId = assignment.TeacherId,
                    TeacherName = teacherName
                });
            }

            return (true, dtos, null);
        }

        // ── GET /api/admin/stats ──────────────────────────────────────
        /// <summary>
        /// Aggregates system-wide statistics including chart data:
        ///   - User counts by role
        ///   - Assignment/submission counts + average grade
        ///   - Weekly submissions (last 7 days)
        ///   - Grade distribution (all graded submissions)
        ///   - Monthly user growth (last 6 months)
        /// </summary>
        public async Task<(bool Succeeded, object? Data, string? Error)> GetSystemStatsAsync()
        {
            // ── User counts ──
            var allUsers = await _userManager.Users.ToListAsync();
            var students = await _userManager.GetUsersInRoleAsync(UserRole.Student.ToString());
            var teachers = await _userManager.GetUsersInRoleAsync(UserRole.Teacher.ToString());
            var admins = await _userManager.GetUsersInRoleAsync(UserRole.Admin.ToString());
            var verifiedTeachers = teachers.Count(t => t.IsVerified);

            // ── Assignment & Submission counts ──
            var assignments = (await _unitOfWork.Assignments.GetAllAsync()).ToList();
            var submissions = (await _unitOfWork.Submissions.GetAllAsync()).ToList();
            var reports = (await _unitOfWork.Reports.GetAllAsync()).ToList();

            int gradedCount = submissions.Count(s => s.Status == SubmissionStatus.Graded);
            int pendingCount = submissions.Count - gradedCount;

            // ── Average grade (percentage) ──
            double? overallAverage = null;
            var distribution = new GradeDistributionDto();

            if (reports.Count > 0 && assignments.Count > 0)
            {
                var assignmentMaxGrades = assignments.ToDictionary(a => a.Id, a => a.MaxGrade);
                double totalPercent = 0;
                int counted = 0;

                foreach (var report in reports)
                {
                    var submission = submissions.FirstOrDefault(s => s.Id == report.SubmissionId);
                    if (submission == null) continue;

                    if (assignmentMaxGrades.TryGetValue(submission.AssignmentId, out var maxGrade) && maxGrade > 0)
                    {
                        var percent = (double)report.Grade / maxGrade * 100.0;
                        totalPercent += percent;
                        counted++;

                        // Grade distribution buckets
                        if (percent < 60) distribution.Below60++;
                        else if (percent < 70) distribution.From60To69++;
                        else if (percent < 80) distribution.From70To79++;
                        else if (percent < 90) distribution.From80To89++;
                        else distribution.From90To100++;
                    }
                }

                if (counted > 0)
                    overallAverage = Math.Round(totalPercent / counted, 2);
            }

            // ── Weekly submissions (last 7 days) ──
            var weeklySubmissions = new List<int>();
            var today = DateTime.UtcNow.Date;
            for (int i = 6; i >= 0; i--)
            {
                var day = today.AddDays(-i);
                weeklySubmissions.Add(submissions.Count(s => s.SubmittedAt.Date == day));
            }

            // ── Monthly user growth (last 6 months) ──
            var monthlyGrowth = new List<int>();
            var now = DateTime.UtcNow;
            for (int i = 5; i >= 0; i--)
            {
                var monthStart = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
                var monthEnd = monthStart.AddMonths(1);
                monthlyGrowth.Add(allUsers.Count(u => u.CreatedAt >= monthStart && u.CreatedAt < monthEnd));
            }

            var stats = new AdminStatsDto
            {
                TotalUsers = allUsers.Count,
                TotalStudents = students.Count,
                TotalTeachers = teachers.Count,
                TotalAdmins = admins.Count,
                VerifiedTeachers = verifiedTeachers,
                TotalAssignments = assignments.Count,
                TotalSubmissions = submissions.Count,
                GradedSubmissions = gradedCount,
                PendingSubmissions = pendingCount,
                OverallAverageGrade = overallAverage,
                WeeklySubmissions = weeklySubmissions,
                GradeDistribution = distribution,
                MonthlyUserGrowth = monthlyGrowth
            };

            return (true, stats, null);
        }
    }
}

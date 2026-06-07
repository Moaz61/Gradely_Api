using Gradely.Application.DTOs.Admin;
using Gradely.Domain.Entities;
using Gradely.Domain.Enums;
using Gradely.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Gradely.Application.Services
{
    /// <summary>
    /// Admin-facing operations: manage users, verify teachers,
    /// delete accounts, and view system-wide statistics.
    ///
    /// NOTE: Teachers now self-register via POST /api/auth/register.
    ///       The admin's role is to VERIFY teacher accounts, not create them.
    ///
    /// DEPENDENCIES:
    ///   - UserManager: ASP.NET Identity service to create/find/manage users + roles
    ///   - IUnitOfWork: access to Assignment/Submission/Report repos for stats
    ///
    /// WHY UserManager instead of IUnitOfWork.Users?
    ///   UserManager provides role management (AddToRoleAsync, GetRolesAsync,
    ///   GetUsersInRoleAsync, DeleteAsync, etc.) that the generic repository
    ///   doesn't have. For user operations we need Identity features.
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
        /// Returns all users in the system with their role,
        /// EXCLUDING admin accounts (admins should not appear in the user list).
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

        // ── DELETE /api/admin/users/{id} ────────────────────────────────
        /// <summary>
        /// Deletes a user account (Teacher or Student).
        /// Admin accounts CANNOT be deleted through this endpoint — this
        /// prevents accidentally removing admin access from the system.
        /// </summary>
        public async Task<(bool Succeeded, string? Error)> DeleteTeacherAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return (false, "User not found.");

            // Block deletion of Admin accounts
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains(UserRole.Admin.ToString()))
                return (false, "Cannot delete an admin account.");

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
        /// Marks a teacher account as verified.
        /// Sets IsVerified = true and persists via UserManager.UpdateAsync.
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

        // ── GET /api/admin/stats ──────────────────────────────────────
        /// <summary>
        /// Aggregates system-wide statistics:
        ///   - User counts by role
        ///   - Assignment/submission counts
        ///   - Average grade across all graded submissions
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
            if (reports.Count > 0 && assignments.Count > 0)
            {
                // Build a lookup: assignmentId → maxGrade
                var assignmentMaxGrades = assignments.ToDictionary(a => a.Id, a => a.MaxGrade);

                double totalPercent = 0;
                int counted = 0;

                foreach (var report in reports)
                {
                    // Find the submission to get the assignmentId
                    var submission = submissions.FirstOrDefault(s => s.Id == report.SubmissionId);
                    if (submission == null) continue;

                    if (assignmentMaxGrades.TryGetValue(submission.AssignmentId, out var maxGrade) && maxGrade > 0)
                    {
                        totalPercent += (double)report.Grade / maxGrade * 100.0;
                        counted++;
                    }
                }

                if (counted > 0)
                    overallAverage = Math.Round(totalPercent / counted, 2);
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
                OverallAverageGrade = overallAverage
            };

            return (true, stats, null);
        }
    }
}

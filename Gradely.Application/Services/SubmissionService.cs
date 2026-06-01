using System.Text.Json;
using Gradely.Application.DTOs.Submissions;
using Gradely.Domain.Entities;
using Gradely.Domain.Enums;
using Gradely.Domain.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Gradely.Application.Services
{
    /// <summary>
    /// Implements ISubmissionService — contains the business logic for submissions.
    /// 
    /// THIS IS THE MOST COMPLEX SERVICE because it handles:
    ///   1. File upload (PDF validation, saving to disk)
    ///   2. Duplicate submission prevention
    ///   3. Data isolation (students only see their own submissions)
    ///   4. Entity-to-DTO mapping with JOINed data
    /// 
    /// DEPENDENCIES:
    ///   - IUnitOfWork: access to Submissions, Assignments, Reports repositories
    ///   - IWebHostEnvironment: tells us where wwwroot is (for saving uploaded files)
    /// 
    /// WHY IWebHostEnvironment?
    ///   We need to save uploaded PDF files to a physical folder on the server.
    ///   IWebHostEnvironment.WebRootPath gives us the path to the wwwroot folder.
    ///   We save files to wwwroot/uploads/ so they can be served as static files.
    /// </summary>
    public class SubmissionService : ISubmissionService
    {
        // ── Dependencies ─────────────────────────────────────────────
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _environment;

        // ── Constants ────────────────────────────────────────────────
        // Max file size: 10MB in bytes
        // 10 * 1024 * 1024 = 10,485,760 bytes
        private const long MaxFileSize = 10 * 1024 * 1024;

        // Only allow PDF files
        private const string AllowedExtension = ".pdf";

        // Folder name inside wwwroot where uploads are saved
        private const string UploadsFolder = "uploads";

        public SubmissionService(IUnitOfWork unitOfWork, IWebHostEnvironment environment)
        {
            _unitOfWork = unitOfWork;
            _environment = environment;
        }

        // ══════════════════════════════════════════════════════════════
        //  SUBMIT (Upload PDF)
        // ══════════════════════════════════════════════════════════════
        /// <summary>
        /// Student uploads a PDF file for an assignment.
        /// 
        /// FULL FLOW:
        ///   1. Cast the DTO (because ISubmissionService uses object)
        ///   2. Validate the file (PDF only, max 10MB)
        ///   3. Verify the assignment exists
        ///   4. Check for duplicate submission (same student + same assignment)
        ///   5. Save the file to wwwroot/uploads/ with a unique name
        ///   6. Create a Submission record in the database
        ///   7. Return the submission DTO
        /// 
        /// IMPORTANT SECURITY NOTES:
        ///   - studentId comes from the JWT token, NOT from the request body
        ///   - We rename the file to a GUID to prevent path traversal attacks
        ///   - We check the file extension to prevent uploading malicious files
        /// </summary>
        public async Task<(bool Succeeded, object? Data, string? Error)> SubmitAsync(
            object submitDto, string studentId)
        {
            // ── Step 1: Cast to the concrete DTO ──
            // ISubmissionService uses "object" because Domain can't reference Application DTOs.
            // Here in the Application layer, we cast to the actual type.
            var dto = submitDto as SubmitAssignmentDto;
            if (dto == null)
                return (false, null, "Invalid submission data.");

            // ── Step 2: We need the file — it's passed via the DTO wrapper ──
            // The file is passed through a special wrapper since IFormFile lives in the API layer.
            // We'll handle this by accepting the file info as a separate parameter.
            // See the controller for how the file is passed.

            // ── Step 3: Verify the assignment exists ──
            // We don't want students submitting to non-existent assignments.
            var assignment = await _unitOfWork.Assignments.GetByIdAsync(dto.AssignmentId);
            if (assignment == null)
                return (false, null, "Assignment not found.");

            // ── Step 3b: Check if the due date has passed ──
            // Students cannot submit after the assignment deadline.
            if (DateTime.UtcNow > assignment.DueDate)
                return (false, null, "Cannot submit — the due date for this assignment has passed.");

            // ── Step 4: Check for duplicate submission ──
            // Use FindAsync with a predicate to find any existing submission
            // with the same AssignmentId AND StudentId.
            var existingSubmissions = await _unitOfWork.Submissions
                .FindAsync(s => s.AssignmentId == dto.AssignmentId && s.StudentId == studentId);

            if (existingSubmissions.Any())
                return (false, null, "You have already submitted to this assignment.");

            // If we reach here, the DTO is valid but we need the actual file.
            // The file will be handled by the overloaded method below.
            return (false, null, "File is required. Use the overloaded method with IFormFile.");
        }

        /// <summary>
        /// The actual submission method that includes the file.
        /// Called by the controller which has access to IFormFile.
        /// 
        /// WHY TWO METHODS?
        ///   The interface uses "object" (Domain layer can't reference IFormFile).
        ///   This concrete method accepts the actual types.
        ///   The controller calls this method directly (not through the interface).
        /// </summary>
        public async Task<(bool Succeeded, object? Data, string? Error)> SubmitWithFileAsync(
            SubmitAssignmentDto dto, IFormFile file, string studentId)
        {
            // ── Step 1: Validate the file ──
            if (file == null || file.Length == 0)
                return (false, null, "File is required.");

            // Check file extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != AllowedExtension)
                return (false, null, "Only PDF files are allowed.");

            // Check file size (10MB max)
            if (file.Length > MaxFileSize)
                return (false, null, "File size cannot exceed 10MB.");

            // ── Step 2: Verify the assignment exists ──
            var assignment = await _unitOfWork.Assignments.GetByIdAsync(dto.AssignmentId);
            if (assignment == null)
                return (false, null, "Assignment not found.");

            // ── Step 2b: Check if the due date has passed ──
            // Students cannot submit after the assignment deadline.
            if (DateTime.UtcNow > assignment.DueDate)
                return (false, null, "Cannot submit — the due date for this assignment has passed.");

            // ── Step 3: Check for duplicate submission ──
            var existingSubmissions = await _unitOfWork.Submissions
                .FindAsync(s => s.AssignmentId == dto.AssignmentId && s.StudentId == studentId);

            if (existingSubmissions.Any())
                return (false, null, "You have already submitted to this assignment.");

            // ── Step 4: Save the file to disk ──
            // Generate a unique filename to avoid conflicts.
            // Example: "a1b2c3d4-my-essay.pdf"
            // We prepend a GUID so even if two students name their file the same,
            // they won't overwrite each other.
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var uploadsPath = Path.Combine(_environment.WebRootPath ?? "wwwroot", UploadsFolder);

            // Create the uploads directory if it doesn't exist
            if (!Directory.Exists(uploadsPath))
                Directory.CreateDirectory(uploadsPath);

            var filePath = Path.Combine(uploadsPath, uniqueFileName);

            // Save the file using a FileStream
            // "using" ensures the stream is closed even if an error occurs
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // ── Step 5: Create the Submission record ──
            // The relative path is stored (not absolute) so the app works
            // if deployed to a different server.
            var submission = new Submission
            {
                AssignmentId = dto.AssignmentId,
                StudentId = studentId,
                FilePath = Path.Combine(UploadsFolder, uniqueFileName),
                OriginalFileName = file.FileName,
                Status = SubmissionStatus.Submitted,
                SubmittedAt = DateTime.UtcNow
            };

            // Stage the submission for insertion
            await _unitOfWork.Submissions.AddAsync(submission);

            // Save to database — this is where UnitOfWork shines.
            // If the DB save fails, the file is already saved but the record isn't.
            // In a production app, you'd want to clean up the file on failure.
            await _unitOfWork.CompleteAsync();

            // ── Step 6: Return the DTO ──
            var responseDto = new SubmissionDto
            {
                Id = submission.Id,
                AssignmentId = submission.AssignmentId,
                AssignmentTitle = assignment.Title,
                OriginalFileName = submission.OriginalFileName,
                Status = submission.Status.ToString(),
                SubmittedAt = submission.SubmittedAt
            };

            return (true, responseDto, null);
        }

        // ══════════════════════════════════════════════════════════════
        //  GET STUDENT'S SUBMISSIONS
        // ══════════════════════════════════════════════════════════════
        /// <summary>
        /// Returns all submissions for the current student.
        /// 
        /// DATA ISOLATION:
        ///   We filter by studentId — the student can ONLY see their own submissions.
        ///   The studentId comes from the JWT token (set by the controller),
        ///   so a student cannot fake being someone else.
        /// 
        /// JOINING WITH ASSIGNMENT:
        ///   The Submission entity only has AssignmentId (a Guid).
        ///   But the DTO needs AssignmentTitle (a string).
        ///   So we load the assignment separately for each submission.
        ///   In a production app with many submissions, you'd use Include() 
        ///   for eager loading, but this is clearer for learning.
        /// </summary>
        public async Task<(bool Succeeded, object? Data, string? Error)> GetStudentSubmissionsAsync(
            string studentId)
        {
            // Get all submissions where StudentId matches the logged-in user
            var submissions = await _unitOfWork.Submissions
                .FindAsync(s => s.StudentId == studentId);

            // Map each submission to a DTO, including the assignment title
            var submissionDtos = new List<SubmissionDto>();
            foreach (var submission in submissions)
            {
                // Look up the assignment to get the title
                var assignment = await _unitOfWork.Assignments.GetByIdAsync(submission.AssignmentId);

                submissionDtos.Add(new SubmissionDto
                {
                    Id = submission.Id,
                    AssignmentId = submission.AssignmentId,
                    AssignmentTitle = assignment?.Title ?? "Unknown Assignment",
                    OriginalFileName = submission.OriginalFileName,
                    Status = submission.Status.ToString(),
                    SubmittedAt = submission.SubmittedAt
                });
            }

            return (true, submissionDtos, null);
        }

        // ══════════════════════════════════════════════════════════════
        //  GET SUBMISSION BY ID
        // ══════════════════════════════════════════════════════════════
        /// <summary>
        /// Returns a single submission, but ONLY if it belongs to the requesting student.
        /// 
        /// SECURITY CHECK:
        ///   We verify submission.StudentId == studentId.
        ///   Without this, a student could view another student's submission
        ///   by guessing the GUID (which is possible with enough attempts).
        /// </summary>
        public async Task<(bool Succeeded, object? Data, string? Error)> GetSubmissionByIdAsync(
            Guid id, string studentId)
        {
            var submission = await _unitOfWork.Submissions.GetByIdAsync(id);

            if (submission == null)
                return (false, null, "Submission not found.");

            // Security: students can only view their own submissions
            if (submission.StudentId != studentId)
                return (false, null, "You do not have access to this submission.");

            // Get assignment title for the DTO
            var assignment = await _unitOfWork.Assignments.GetByIdAsync(submission.AssignmentId);

            var dto = new SubmissionDto
            {
                Id = submission.Id,
                AssignmentId = submission.AssignmentId,
                AssignmentTitle = assignment?.Title ?? "Unknown Assignment",
                OriginalFileName = submission.OriginalFileName,
                Status = submission.Status.ToString(),
                SubmittedAt = submission.SubmittedAt
            };

            return (true, dto, null);
        }

        // ══════════════════════════════════════════════════════════════
        //  GET SUBMISSION REPORT
        // ══════════════════════════════════════════════════════════════
        /// <summary>
        /// Returns the ML-generated grading report for a submission.
        /// 
        /// FLOW:
        ///   1. Find the submission and verify ownership
        ///   2. Find the report for this submission
        ///   3. If no report exists → return "No report yet" (ML hasn't graded it)
        ///   4. If report exists → deserialize MistakesJson and return ReportDto
        /// 
        /// JSON DESERIALIZATION:
        ///   The Report entity stores mistakes as a JSON string.
        ///   We use System.Text.Json to deserialize it into a List<MistakeDto>.
        ///   This transforms raw DB data into a proper typed response.
        /// </summary>
        public async Task<(bool Succeeded, object? Data, string? Error)> GetSubmissionReportAsync(
            Guid id, string studentId)
        {
            // Step 1: Find the submission
            var submission = await _unitOfWork.Submissions.GetByIdAsync(id);

            if (submission == null)
                return (false, null, "Submission not found.");

            // Security: students can only view their own reports
            if (submission.StudentId != studentId)
                return (false, null, "You do not have access to this submission.");

            // Step 2: Find the report for this submission
            var reports = await _unitOfWork.Reports
                .FindAsync(r => r.SubmissionId == id);
            var report = reports.FirstOrDefault();

            // Step 3: No report yet (ML system hasn't processed it)
            if (report == null)
                return (false, null, "No report available yet. The submission is still being graded.");

            // Step 4: Deserialize MistakesJson → List<MistakeDto>
            // The ML system sends mistakes as JSON, we stored it as a string.
            // Now we convert it to proper C# objects for the API response.
            var mistakes = new List<MistakeDto>();
            if (!string.IsNullOrEmpty(report.MistakesJson) && report.MistakesJson != "[]")
            {
                try
                {
                    // JsonSerializer options: camelCase to match the ML system's JSON format
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    mistakes = JsonSerializer.Deserialize<List<MistakeDto>>(
                        report.MistakesJson, options) ?? new List<MistakeDto>();
                }
                catch (JsonException)
                {
                    // If the JSON is malformed, return an empty list rather than crash
                    mistakes = new List<MistakeDto>();
                }
            }

            var dto = new ReportDto
            {
                Grade = report.Grade,
                Feedback = report.Feedback,
                Mistakes = mistakes,
                CreatedAt = report.CreatedAt
            };

            return (true, dto, null);
        }
    }
}

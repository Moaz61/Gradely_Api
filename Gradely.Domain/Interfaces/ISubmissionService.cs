namespace Gradely.Domain.Interfaces
{
    /// <summary>
    /// Defines the contract for submission-related operations.
    /// 
    /// OPERATIONS DEFINED:
    ///   1. SubmitAsync       → Student uploads a PDF for an assignment
    ///   2. GetStudentSubmissionsAsync → Student sees their own submissions only
    ///   3. GetSubmissionByIdAsync     → Student views a single submission
    ///   4. GetSubmissionReportAsync   → Student views the ML-generated report
    /// 
    /// WHY studentId IS PASSED IN:
    ///   Every method receives the studentId so we can:
    ///   1. Create submissions linked to the correct student
    ///   2. Filter submissions to only show the current student's data
    ///   3. Prevent students from seeing each other's submissions
    ///   
    ///   The studentId comes from the JWT token (extracted in the Controller),
    ///   NOT from the request body — so students can't fake being someone else.
    /// 
    /// SAME RETURN PATTERN AS IAuthService:
    ///   (bool Succeeded, object? Data, string? Error)
    ///   - Success case: (true, dto, null)
    ///   - Failure case: (false, null, "Error message")
    /// </summary>
    public interface ISubmissionService
    {
        /// <summary>
        /// Submit a PDF file for an assignment.
        /// 
        /// What the implementation does:
        ///   1. Validate file (PDF only, max 10MB)
        ///   2. Check assignment exists
        ///   3. Check for duplicate submission (same student + same assignment)
        ///   4. Save file to server
        ///   5. Create Submission record in database
        /// 
        /// Parameters use "object" because Domain can't reference Application DTOs.
        /// The actual implementation casts to the concrete DTO type.
        /// 
        /// Used by: POST /api/submissions
        /// Auth: Student role
        /// </summary>
        Task<(bool Succeeded, object? Data, string? Error)> SubmitAsync(object submitDto, string studentId);

        /// <summary>
        /// Get all submissions for the current student.
        /// Only returns the logged-in student's submissions — never another student's.
        /// 
        /// Used by: GET /api/submissions
        /// Auth: Student role
        /// </summary>
        Task<(bool Succeeded, object? Data, string? Error)> GetStudentSubmissionsAsync(string studentId);

        /// <summary>
        /// Get a single submission by ID.
        /// Only returns it if the submission belongs to the requesting student.
        /// 
        /// Used by: GET /api/submissions/{id}
        /// Auth: Student role
        /// </summary>
        Task<(bool Succeeded, object? Data, string? Error)> GetSubmissionByIdAsync(Guid id, string studentId);

        /// <summary>
        /// Get the ML-generated grading report for a submission.
        /// Returns the report if it exists, or an error if the submission
        /// hasn't been graded yet (the ML system hasn't processed it).
        /// 
        /// Used by: GET /api/submissions/{id}/report
        /// Auth: Student role
        /// </summary>
        Task<(bool Succeeded, object? Data, string? Error)> GetSubmissionReportAsync(Guid id, string studentId);
    }
}

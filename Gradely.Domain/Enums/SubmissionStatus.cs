namespace Gradely.Domain.Enums
{
    /// <summary>
    /// Tracks the lifecycle of a student's submission.
    /// 
    /// WHY AN ENUM?
    ///   Instead of storing raw strings like "submitted" or "graded" in the database
    ///   (which are prone to typos and hard to validate), we use an enum.
    ///   EF Core stores the integer value (0, 1) in the DB column,
    ///   but in C# code we use the readable names.
    /// 
    /// LIFECYCLE:
    ///   Student uploads PDF  →  Status = Submitted (0)
    ///   ML system grades it  →  Status = Graded (1)
    /// </summary>
    public enum SubmissionStatus
    {
        /// <summary>
        /// The student has uploaded their PDF but it hasn't been graded yet.
        /// This is the initial status when a submission is created.
        /// </summary>
        Submitted = 0,

        /// <summary>
        /// The ML system has processed the submission and generated a report.
        /// The student can now view their grade and detailed feedback.
        /// </summary>
        Graded = 1
    }
}

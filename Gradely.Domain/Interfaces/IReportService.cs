namespace Gradely.Domain.Interfaces
{
    public interface IReportService
    {
        // submissionId = which submission is being graded
        // dto = the grade + feedback + mistakes from ML team
        // Returns: (success, data, errorMessage) — same pattern as all other services
        Task<(bool Succeeded, object? Data, string? Error)> SaveReportAsync(Guid submissionId, object dto);
    }
}

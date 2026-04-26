namespace Gradely.Domain.Interfaces
{
    /// <summary>
    /// Operations exposed to the Student role for the dashboard view (Phase 5).
    /// </summary>
    public interface IStudentService
    {
        Task<(bool Succeeded, object? Data, string? Error)> GetDashboardAsync(string studentId);
    }
}

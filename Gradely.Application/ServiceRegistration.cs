using Gradely.Application.Services;
using Gradely.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Gradely.Application
{
    /// <summary>
    /// Extension method to register all Application layer services with DI.
    /// 
    /// WHY?
    ///   Same idea as InfrastructureServiceRegistration — keeps Program.cs clean.
    ///   Each layer is responsible for registering its own services.
    /// 
    /// WHAT GETS REGISTERED:
    ///   IAuthService       → AuthService
    ///   IAssignmentService → AssignmentService
    ///   ISubmissionService → SubmissionService
    ///   SubmissionService  → SubmissionService (concrete type for SubmitWithFileAsync)
    /// 
    /// ALL ARE SCOPED:
    ///   Scoped = a new instance is created for each HTTP request.
    ///   This matches the lifetime of DbContext and UnitOfWork.
    /// </summary>
    public static class ApplicationServiceRegistration
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Auth service (from Phase 2)
            services.AddScoped<IAuthService, AuthService>();

            // Assignment service — handles listing and retrieving assignments
            services.AddScoped<IAssignmentService, AssignmentService>();

            // Submission service — handles file upload, submission queries, report retrieval
            // Registered as BOTH interface and concrete type:
            //   - ISubmissionService: for the interface-based methods (GetStudentSubmissions, etc.)
            //   - SubmissionService: so the controller can call SubmitWithFileAsync directly
            //     (the interface can't reference IFormFile because it lives in Domain layer)
            services.AddScoped<ISubmissionService, SubmissionService>();
            services.AddScoped<SubmissionService>();

            // Report service — handles saving ML grading results
            services.AddScoped<IReportService, ReportService>();

            return services;
        }
    }
}

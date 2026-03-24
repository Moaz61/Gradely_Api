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
    ///   IAuthService → AuthService (Scoped = one instance per HTTP request)
    ///   
    /// LATER you'll add:
    ///   IAssignmentService → AssignmentService
    ///   ISubmissionService → SubmissionService
    ///   etc.
    /// </summary>
    public static class ApplicationServiceRegistration
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Scoped = a new instance is created for each HTTP request.
            // This means all services within one request share the same AuthService instance.
            services.AddScoped<IAuthService, AuthService>();

            return services;
        }
    }
}

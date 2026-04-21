using Gradely.Domain.Entities;
using Gradely.Domain.Interfaces;
using Gradely.Infrastructure.Data;
using Gradely.Infrastructure.Repositories;
using Gradely.Infrastructure.UnitOfWork;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gradely.Infrastructure
{
    /// <summary>
    /// Extension method to register all Infrastructure services with DI.
    /// 
    /// WHY an extension method?
    ///   Instead of putting 20 lines of DI config in Program.cs, we encapsulate it here.
    ///   Program.cs just calls: builder.Services.AddInfrastructureServices(config);
    ///   This keeps Program.cs clean and each layer responsible for its own DI.
    /// 
    /// WHAT GETS REGISTERED:
    ///   1. AppDbContext → talks to SQL Server using the connection string
    ///   2. ASP.NET Identity → manages users, passwords, roles
    ///   3. GenericRepository → the generic CRUD repo
    ///   4. UnitOfWork → wraps repos in a single transaction
    /// </summary>
    public static class InfrastructureServiceRegistration
    {
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // ── 1. Register AppDbContext ──────────────────────────────
            // Tells EF Core: "Use SQL Server with this connection string."
            // The connection string comes from appsettings.json → "ConnectionStrings:DefaultConnection"
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)
                )
            );

            // ── 2. Register ASP.NET Identity ─────────────────────────
            // AddIdentity<TUser, TRole> sets up the full Identity system:
            //   - UserManager<ApplicationUser> → create users, find by email, check passwords
            //   - SignInManager<ApplicationUser> → handle login/logout
            //   - RoleManager<IdentityRole> → manage roles
            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                // Password rules — customize as you like
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;  // no special chars required
                options.Password.RequiredLength = 6;

                // User settings
                options.User.RequireUniqueEmail = true;  // no duplicate emails
            })
            .AddEntityFrameworkStores<AppDbContext>()  // tells Identity to use our DbContext
            .AddDefaultTokenProviders();               // for password reset tokens, etc.

            // ── 3. Register Generic Repository ───────────────────────
            // Scoped = one instance per HTTP request (shared across the request pipeline)
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

            // ── 4. Register Unit of Work ─────────────────────────────
            services.AddScoped<IUnitOfWork, UnitOfWork.UnitOfWork>();

            // ── 5. Register DbContext base type ──────────────────────
            // AuthService (in Application layer) injects DbContext (base class)
            // because it can't reference AppDbContext (that would break Clean Architecture).
            // This registration tells DI: "When someone asks for DbContext, give them AppDbContext."
            services.AddScoped<Microsoft.EntityFrameworkCore.DbContext>(
                sp => sp.GetRequiredService<AppDbContext>()
            );

            return services;
        }
    }
}

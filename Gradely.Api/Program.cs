using System.Text;
using Gradely.Application;
using Gradely.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace Gradely.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ══════════════════════════════════════════════════════════
            //  1. REGISTER SERVICES (Dependency Injection Container)
            // ══════════════════════════════════════════════════════════
            // Each layer registers its own services via extension methods.
            // This keeps Program.cs clean — you don't see 100 lines of DI here.

            // ── Infrastructure layer services ──
            // Registers: DbContext, Identity (UserManager, SignInManager), Repositories, UnitOfWork
            builder.Services.AddInfrastructureServices(builder.Configuration);

            // ── Application layer services ──
            // Registers: AuthService (and future services like AssignmentService, etc.)
            builder.Services.AddApplicationServices();

            // ── API layer services ──
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();

            // ── Swagger with JWT support ──
            // This configures Swagger UI to show an "Authorize" button
            // so you can paste your JWT token and test [Authorize] endpoints.
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Gradely API",
                    Version = "v1",
                    Description = "Gradely — AI-Powered Assignment Grading System"
                });

                // Add JWT auth to Swagger
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter your JWT token. Example: eyJhbGciOiJIUzI1NiIs..."
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            // ══════════════════════════════════════════════════════════
            //  2. CONFIGURE JWT AUTHENTICATION
            // ══════════════════════════════════════════════════════════
            // This tells ASP.NET: "When a request comes in with an Authorization header,
            // validate the JWT token using these rules."

            builder.Services.AddAuthentication(options =>
            {
                // Default scheme = JWT Bearer (not cookies, not Windows auth)
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                // These are the validation rules for incoming tokens
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    // Validate that the token was signed with our secret key
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!)
                    ),
                    // Validate that the token's issuer matches our API
                    ValidateIssuer = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    // Validate that the token's audience matches our client
                    ValidateAudience = true,
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    // Validate that the token hasn't expired
                    ValidateLifetime = true,
                    // No clock skew — token expires exactly when it says
                    ClockSkew = TimeSpan.Zero
                };
            });

            // ══════════════════════════════════════════════════════════
            //  3. CONFIGURE CORS
            // ══════════════════════════════════════════════════════════
            // CORS (Cross-Origin Resource Sharing) allows your frontend
            // (running on localhost:3000 or a different domain) to call this API.
            // Without CORS, browsers block cross-origin requests.

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()    // any domain can call us
                          .AllowAnyMethod()    // GET, POST, PUT, DELETE, etc.
                          .AllowAnyHeader();   // any HTTP headers
                });
            });

            // ══════════════════════════════════════════════════════════
            //  4. BUILD THE APP & CONFIGURE MIDDLEWARE PIPELINE
            // ══════════════════════════════════════════════════════════
            var app = builder.Build();

            // Swagger UI — only in development
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            // ── Serve static files from wwwroot/ ──
            // This enables serving uploaded PDF files from wwwroot/uploads/.
            // Without this, files saved to wwwroot won't be accessible via HTTP.
            // Example: a file saved to wwwroot/uploads/abc.pdf is served at /uploads/abc.pdf
            app.UseStaticFiles();

            // CORS must come before auth
            app.UseCors("AllowAll");

            // ── ORDER MATTERS! ──
            // UseAuthentication = "Read the JWT token and figure out WHO the user is"
            // UseAuthorization  = "Check if that user is ALLOWED to access this endpoint"
            // Authentication MUST come before Authorization.
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}

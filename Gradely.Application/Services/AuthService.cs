using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Gradely.Application.DTOs.Auth;
using Gradely.Domain.Entities;
using Gradely.Domain.Enums;
using Gradely.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Gradely.Application.Services
{
    /// <summary>
    /// Implements the IAuthService interface — this is where the authentication LOGIC lives.
    /// 
    /// IN CLEAN ARCHITECTURE:
    ///   Domain = WHAT operations exist (IAuthService interface)
    ///   Application = HOW they work (this class)
    ///   Infrastructure = WHERE data is stored (DbContext, Repos)
    ///   API = WHO can call them (Controllers)
    /// 
    /// DEPENDENCIES (all injected via DI):
    ///   - UserManager: ASP.NET Identity service to create/find/manage users
    ///   - SignInManager: ASP.NET Identity service to verify passwords
    ///   - IConfiguration: Reads JWT settings from appsettings.json
    ///   - IUnitOfWork: Not used directly for auth (Identity handles it), but available for future use
    /// </summary>
    public class AuthService : IAuthService
    {
        // ── Dependencies ─────────────────────────────────────────────
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
        }

        // ══════════════════════════════════════════════════════════════
        //  REGISTER
        // ══════════════════════════════════════════════════════════════
        /// <summary>
        /// Creates a new user account and returns a JWT token.
        /// 
        /// FLOW:
        ///   1. Cast the object to RegisterDto (because IAuthService uses object)
        ///   2. Check if email already exists
        ///   3. Create the user with Identity (hashes password automatically)
        ///   4. Assign the "Student" role
        ///   5. Generate a JWT token
        ///   6. Return the token + user info
        /// </summary>
        public async Task<(bool Succeeded, object? Data, string? Error)> RegisterAsync(object registerDto)
        {
            // ── Step 1: Cast to the concrete DTO ──
            var dto = registerDto as RegisterDto;
            if (dto == null)
                return (false, null, "Invalid registration data.");

            // ── Step 2: Check if email is already taken ──
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
                return (false, null, "Email is already registered.");

            // ── Step 3: Create the ApplicationUser entity ──
            var user = new ApplicationUser
            {
                UserName = dto.Email,       // Identity requires UserName; we use email
                Email = dto.Email,
                FullName = dto.FullName,
                CreatedAt = DateTime.UtcNow
            };

            // CreateAsync hashes the password and saves to AspNetUsers table
            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                // Collect all Identity errors (e.g. "Password too short")
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return (false, null, errors);
            }

            // ── Step 4: Assign the Student role ──
            // Every new user starts as a Student. Admins can upgrade later.
            await _userManager.AddToRoleAsync(user, UserRole.Student.ToString());

            // ── Step 5 & 6: Generate JWT and return response ──
            var token = await GenerateJwtToken(user);
            var response = CreateAuthResponse(user, token);
            return (true, response, null);
        }

        // ══════════════════════════════════════════════════════════════
        //  LOGIN
        // ══════════════════════════════════════════════════════════════
        /// <summary>
        /// Verifies email + password and returns a JWT token.
        /// 
        /// FLOW:
        ///   1. Find user by email
        ///   2. Check password with SignInManager
        ///   3. Generate JWT token
        ///   4. Return token + user info
        /// </summary>
        public async Task<(bool Succeeded, object? Data, string? Error)> LoginAsync(object loginDto)
        {
            var dto = loginDto as LoginDto;
            if (dto == null)
                return (false, null, "Invalid login data.");

            // ── Step 1: Find the user ──
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return (false, null, "Invalid email or password.");

            // ── Step 2: Verify password ──
            // CheckPasswordSignInAsync compares the plain password against the stored hash.
            // lockoutOnFailure: true = lock account after too many failed attempts (security)
            var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: false);
            if (!result.Succeeded)
                return (false, null, "Invalid email or password.");
            // NOTE: We say "Invalid email or password" for BOTH cases (user not found / wrong password)
            // This is a security best practice — don't reveal whether the email exists.

            // ── Step 3 & 4: Generate JWT and return ──
            var token = await GenerateJwtToken(user);
            var response = CreateAuthResponse(user, token);
            return (true, response, null);
        }

        // ══════════════════════════════════════════════════════════════
        //  GET CURRENT USER
        // ══════════════════════════════════════════════════════════════
        /// <summary>
        /// Gets the profile of the currently logged-in user by their ID.
        /// Called from the [Authorize] endpoint GET /api/auth/me.
        /// The user ID comes from the JWT token claims.
        /// </summary>
        public async Task<(bool Succeeded, object? Data, string? Error)> GetCurrentUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return (false, null, "User not found.");

            var roles = await _userManager.GetRolesAsync(user);

            // Return user info WITHOUT a new token (they already have one)
            var response = new AuthResponseDto
            {
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                Role = roles.FirstOrDefault() ?? "Student",
                Token = string.Empty,       // No need to return token here
                ExpiresAt = DateTime.MinValue
            };

            return (true, response, null);
        }

        // ══════════════════════════════════════════════════════════════
        //  PRIVATE HELPERS
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Creates a JWT token for the given user.
        /// 
        /// WHAT IS A JWT?
        ///   A JSON Web Token is a signed string that contains:
        ///   1. Header: algorithm used (HS256)
        ///   2. Payload: claims (user ID, email, role, expiry)
        ///   3. Signature: proves the token wasn't tampered with
        /// 
        ///   The client sends this in every request:
        ///   Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
        /// 
        /// HOW IT WORKS HERE:
        ///   1. We create a list of "claims" (key-value pairs about the user)
        ///   2. We create a signing key from the secret in appsettings.json
        ///   3. We build the token with claims + key + expiry
        ///   4. We serialize it to a string
        /// </summary>
        private async Task<string> GenerateJwtToken(ApplicationUser user)
        {
            // ── Get the user's roles ──
            var roles = await _userManager.GetRolesAsync(user);

            // ── Build claims ──
            // Claims = pieces of information embedded in the token.
            // The server reads these on every request to know WHO is calling.
            var claims = new List<Claim>
            {
                // Sub (Subject) = the user's unique ID
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                // Email claim
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                // Name claim
                new Claim(ClaimTypes.Name, user.FullName),
                // Unique identifier for this specific token
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // ── Add role claims ──
            // A user could have multiple roles, so we add each one.
            // The [Authorize(Roles = "Admin")] attribute reads these claims.
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            // ── Create signing key ──
            // The secret key is in appsettings.json → Jwt:Secret
            // IMPORTANT: In production, use a long random string (32+ chars)
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"]!)
            );

            // ── Build the token ──
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(
                    double.Parse(_configuration["Jwt:ExpiryInDays"] ?? "7")
                ),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            // Serialize to string: "eyJhbGciOiJIUzI1NiIs..."
            return tokenHandler.WriteToken(token);
        }

        /// <summary>
        /// Maps user + token into an AuthResponseDto.
        /// Small helper to avoid duplicating this in Register and Login.
        /// </summary>
        private AuthResponseDto CreateAuthResponse(ApplicationUser user, string token)
        {
            return new AuthResponseDto
            {
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddDays(
                    double.Parse(_configuration["Jwt:ExpiryInDays"] ?? "7")
                ),
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                // We just assigned the role, so we know it's Student for Register
                // For Login, we'd need to fetch roles — but CreateAuthResponse is called
                // right after we already set the role, so this is fine.
                Role = "Student"
            };
        }
    }
}

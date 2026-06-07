using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Gradely.Application.DTOs.Auth;
using Gradely.Domain.Entities;
using Gradely.Domain.Enums;
using Gradely.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
    ///   - DbContext: Used to store/retrieve refresh tokens
    /// </summary>
    public class AuthService : IAuthService
    {
        // ── Dependencies ─────────────────────────────────────────────
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly DbContext _dbContext;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration,
            DbContext dbContext)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _dbContext = dbContext;
        }

        // ══════════════════════════════════════════════════════════════
        //  REGISTER
        // ══════════════════════════════════════════════════════════════
        /// <summary>
        /// Creates a new user account. Users can register as Student or Teacher.
        /// 
        /// FLOW:
        ///   1. Cast the object to RegisterDto (because IAuthService uses object)
        ///   2. Validate the chosen role (only Student or Teacher allowed)
        ///   3. Check if email already exists
        ///   4. Create the user with Identity (hashes password automatically)
        ///   5. Assign the chosen role
        ///   6. For Students: generate tokens and return them immediately
        ///   7. For Teachers: set IsVerified = false, return a message to wait for admin approval
        /// </summary>
        public async Task<(bool Succeeded, object? Data, string? Error)> RegisterAsync(object registerDto)
        {
            // ── Step 1: Cast to the concrete DTO ──
            var dto = registerDto as RegisterDto;
            if (dto == null)
                return (false, null, "Invalid registration data.");

            // ── Step 2: Validate the chosen role ──
            // Only Student and Teacher can self-register. Admin accounts are seeded.
            var role = dto.Role?.Trim();
            if (string.IsNullOrEmpty(role) ||
                (!role.Equals(UserRole.Student.ToString(), StringComparison.OrdinalIgnoreCase) &&
                 !role.Equals(UserRole.Teacher.ToString(), StringComparison.OrdinalIgnoreCase)))
            {
                return (false, null, "Invalid role. Please choose 'Student' or 'Teacher'.");
            }

            // Normalize the role string (e.g. "student" → "Student")
            bool isTeacher = role.Equals(UserRole.Teacher.ToString(), StringComparison.OrdinalIgnoreCase);
            var normalizedRole = isTeacher ? UserRole.Teacher.ToString() : UserRole.Student.ToString();

            // ── Step 3: Check if email is already taken ──
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
                return (false, null, "Email is already registered.");

            // ── Step 4: Create the ApplicationUser entity ──
            var user = new ApplicationUser
            {
                UserName = dto.Email,       // Identity requires UserName; we use email
                Email = dto.Email,
                FullName = dto.FullName,
                CreatedAt = DateTime.UtcNow,
                IsVerified = !isTeacher      // Students are verified by default, teachers need admin approval
            };

            // CreateAsync hashes the password and saves to AspNetUsers table
            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                // Collect all Identity errors (e.g. "Password too short")
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return (false, null, errors);
            }

            // ── Step 5: Assign the chosen role ──
            await _userManager.AddToRoleAsync(user, normalizedRole);

            // ── Step 6 & 7: Handle response based on role ──
            if (isTeacher)
            {
                // Teachers must wait for admin verification before they can login.
                // Don't issue tokens — they can't use the system yet.
                return (true, new
                {
                    message = "Registration successful! Your teacher account is pending admin verification. You will be able to login once an administrator approves your account.",
                    email = user.Email,
                    fullName = user.FullName,
                    role = normalizedRole,
                    isVerified = false
                }, null);
            }

            // Students get tokens immediately
            var accessToken = await GenerateJwtToken(user);
            var refreshToken = await CreateRefreshTokenAsync(user);
            var roles = await _userManager.GetRolesAsync(user);
            var response = CreateAuthResponse(user, accessToken, refreshToken, roles);
            return (true, response, null);
        }

        // ══════════════════════════════════════════════════════════════
        //  LOGIN
        // ══════════════════════════════════════════════════════════════
        /// <summary>
        /// Verifies email + password and returns a JWT access token + refresh token.
        /// 
        /// FLOW:
        ///   1. Find user by email
        ///   2. Check password with SignInManager
        ///   3. Check if user is a teacher and verify they are approved
        ///   4. Generate JWT access token + refresh token
        ///   5. Return both tokens + user info
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

            // ── Step 3: Block unverified teachers ──
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains(UserRole.Teacher.ToString()) && !user.IsVerified)
            {
                return (false, null, "Your account has not been verified yet. Please wait for an administrator to approve your account.");
            }

            // ── Step 4 & 5: Generate tokens and return ──
            var accessToken = await GenerateJwtToken(user);
            var refreshToken = await CreateRefreshTokenAsync(user);
            var response = CreateAuthResponse(user, accessToken, refreshToken, roles);
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
        //  REFRESH TOKEN
        // ══════════════════════════════════════════════════════════════
        /// <summary>
        /// Validates an existing refresh token and issues a new access token + refresh token pair.
        /// 
        /// THIS IS CALLED "TOKEN ROTATION":
        ///   1. Client sends the old refresh token
        ///   2. We verify it exists, is not expired, and is not revoked
        ///   3. We revoke the old refresh token (can't be reused)
        ///   4. We create a NEW refresh token
        ///   5. We generate a NEW access token
        ///   6. Return both new tokens
        /// 
        /// WHY ROTATION?
        ///   If someone steals a refresh token and tries to use it AFTER the real user
        ///   already used it, it will be revoked → the attacker gets nothing.
        /// </summary>
        public async Task<(bool Succeeded, object? Data, string? Error)> RefreshTokenAsync(object refreshTokenDto)
        {
            var dto = refreshTokenDto as RefreshTokenDto;
            if (dto == null)
                return (false, null, "Invalid refresh token data.");

            // ── Step 1: Find the refresh token in the database ──
            var existingToken = await _dbContext.Set<RefreshToken>()
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == dto.RefreshToken);

            if (existingToken == null)
                return (false, null, "Invalid refresh token.");

            // ── Step 2: Check if it's still valid ──
            if (!existingToken.IsActive)
            {
                // Token is either expired or already revoked
                if (existingToken.IsExpired)
                    return (false, null, "Refresh token has expired. Please login again.");
                if (existingToken.IsRevoked)
                    return (false, null, "Refresh token has been revoked. Please login again.");
            }

            // ── Step 3: Revoke the old refresh token (token rotation) ──
            existingToken.RevokedAt = DateTime.UtcNow;

            // ── Step 4 & 5: Generate new tokens ──
            var user = existingToken.User;
            var newAccessToken = await GenerateJwtToken(user);
            var newRefreshToken = await CreateRefreshTokenAsync(user);

            // Save the revocation of the old token
            await _dbContext.SaveChangesAsync();

            // ── Step 6: Return the new tokens ──
            var roles = await _userManager.GetRolesAsync(user);
            var response = CreateAuthResponse(user, newAccessToken, newRefreshToken, roles);
            return (true, response, null);
        }

        // ══════════════════════════════════════════════════════════════
        //  REVOKE REFRESH TOKEN (LOGOUT)
        // ══════════════════════════════════════════════════════════════
        /// <summary>
        /// Revokes ALL active refresh tokens for a user.
        /// Called during logout to ensure the user is fully signed out on all devices.
        /// </summary>
        public async Task<(bool Succeeded, string? Error)> RevokeRefreshTokenAsync(string userId)
        {
            // Find all active (non-revoked, non-expired) refresh tokens for this user
            var activeTokens = await _dbContext.Set<RefreshToken>()
                .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
                .ToListAsync();

            // Mark them all as revoked
            foreach (var token in activeTokens)
            {
                token.RevokedAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync();
            return (true, null);
        }

        // ══════════════════════════════════════════════════════════════
        //  PRIVATE HELPERS
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Creates a JWT access token for the given user.
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
        /// ACCESS TOKEN EXPIRY:
        ///   Short-lived (30 minutes by default) for security.
        ///   When it expires, the client uses the refresh token to get a new one.
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
            // Access token expires in minutes (short-lived for security)
            var expiryMinutes = double.Parse(_configuration["Jwt:AccessTokenExpiryInMinutes"] ?? "30");

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(expiryMinutes),
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
        /// Creates a new refresh token, saves it to the database, and returns it.
        /// 
        /// HOW REFRESH TOKENS ARE GENERATED:
        ///   1. Generate 32 random bytes using a cryptographic RNG
        ///   2. Convert to Base64 string (URL-safe, unique, unpredictable)
        ///   3. Save to the RefreshTokens table with the user's ID and expiry
        /// 
        /// WHY CRYPTOGRAPHIC RANDOM?
        ///   Regular Random is predictable. An attacker could guess the next token.
        ///   RandomNumberGenerator uses the OS's crypto provider — truly unpredictable.
        /// </summary>
        private async Task<RefreshToken> CreateRefreshTokenAsync(ApplicationUser user)
        {
            // Generate a cryptographically secure random token
            var randomBytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);

            var refreshTokenExpiryDays = double.Parse(
                _configuration["Jwt:RefreshTokenExpiryInDays"] ?? "30"
            );

            var refreshToken = new RefreshToken
            {
                Token = Convert.ToBase64String(randomBytes),
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpiryDays),
                CreatedAt = DateTime.UtcNow
            };

            // Save to database
            _dbContext.Set<RefreshToken>().Add(refreshToken);
            await _dbContext.SaveChangesAsync();

            return refreshToken;
        }

        /// <summary>
        /// Maps user + tokens into an AuthResponseDto.
        /// Small helper to avoid duplicating this in Register, Login, and Refresh.
        /// </summary>
        private AuthResponseDto CreateAuthResponse(
            ApplicationUser user,
            string accessToken,
            RefreshToken refreshToken,
            IList<string> roles)
        {
            var expiryMinutes = double.Parse(_configuration["Jwt:AccessTokenExpiryInMinutes"] ?? "30");

            return new AuthResponseDto
            {
                Token = accessToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes),
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                Role = roles.FirstOrDefault() ?? "Student",
                RefreshToken = refreshToken.Token,
                RefreshTokenExpiresAt = refreshToken.ExpiresAt
            };
        }
    }
}

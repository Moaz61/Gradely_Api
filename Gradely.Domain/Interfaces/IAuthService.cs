namespace Gradely.Domain.Interfaces
{
    /// <summary>
    /// Defines the authentication operations contract.
    /// 
    /// WHY is this in Domain and not Application?
    /// — In Clean Architecture, the Domain defines WHAT operations exist (interfaces).
    ///   The Application layer defines HOW they work (implementations).
    ///   This way, Domain stays independent of any framework or library.
    /// 
    /// NOTE: We use "object" return types here because the DTOs live in the Application layer,
    /// and Domain cannot reference Application (that would break the dependency rule).
    /// The actual implementation in Application will use the concrete DTO types.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Register a new user and return an auth response (token + user info).
        /// Returns (success, data-or-null, errorMessage-or-null).
        /// </summary>
        Task<(bool Succeeded, object? Data, string? Error)> RegisterAsync(object registerDto);

        /// <summary>
        /// Login with credentials and return an auth response (token + user info).
        /// </summary>
        Task<(bool Succeeded, object? Data, string? Error)> LoginAsync(object loginDto);

        /// <summary>
        /// Get the current logged-in user's profile by their user ID.
        /// </summary>
        Task<(bool Succeeded, object? Data, string? Error)> GetCurrentUserAsync(string userId);

        /// <summary>
        /// Validate a refresh token and issue a new access token + refresh token pair.
        /// Returns (success, data-or-null, errorMessage-or-null).
        /// </summary>
        Task<(bool Succeeded, object? Data, string? Error)> RefreshTokenAsync(object refreshTokenDto);

        /// <summary>
        /// Revoke all active refresh tokens for a user (used during logout).
        /// </summary>
        Task<(bool Succeeded, string? Error)> RevokeRefreshTokenAsync(string userId);
    }
}

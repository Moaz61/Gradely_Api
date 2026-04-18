using System.ComponentModel.DataAnnotations;

namespace Gradely.Application.DTOs.Auth
{
    /// <summary>
    /// Data Transfer Object for the refresh token request.
    /// 
    /// The client sends this to POST /api/auth/refresh when the access token expires.
    /// It contains the refresh token that was returned during login/register.
    /// 
    /// EXAMPLE JSON sent by client:
    ///   {
    ///     "refreshToken": "a1b2c3d4e5f6..."
    ///   }
    /// </summary>
    public class RefreshTokenDto
    {
        /// <summary>
        /// The refresh token string that was issued during login or a previous refresh.
        /// </summary>
        [Required(ErrorMessage = "Refresh token is required.")]
        public string RefreshToken { get; set; } = string.Empty;
    }
}

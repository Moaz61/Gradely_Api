using System.ComponentModel.DataAnnotations;

namespace Gradely.Application.DTOs.Auth
{
    /// <summary>
    /// Data Transfer Object for user login.
    /// 
    /// The client sends email + password, and we verify them against the database.
    /// 
    /// EXAMPLE JSON:
    ///   {
    ///     "email": "moaaz@gradely.com",
    ///     "password": "Abc123"
    ///   }
    /// </summary>
    public class LoginDto
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; } = string.Empty;
    }
}

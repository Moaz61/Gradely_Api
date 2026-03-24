using System.ComponentModel.DataAnnotations;

namespace Gradely.Application.DTOs.Auth
{
    /// <summary>
    /// Data Transfer Object for user registration.
    /// 
    /// WHAT IS A DTO?
    ///   A DTO is a simple class that carries data between layers.
    ///   It defines EXACTLY what the client sends to us — nothing more, nothing less.
    ///   The client sends JSON → ASP.NET deserializes it into this object.
    /// 
    /// WHY not use the ApplicationUser entity directly?
    ///   1. Security: The entity has fields we don't want the client to set (Id, CreatedAt, PasswordHash)
    ///   2. Validation: DTOs can have [Required] attributes specific to the operation
    ///   3. Decoupling: If the entity changes, the API contract stays the same
    /// 
    /// EXAMPLE JSON the client sends:
    ///   {
    ///     "fullName": "Moaaz Ahmed",
    ///     "email": "moaaz@gradely.com",
    ///     "password": "Abc123",
    ///     "confirmPassword": "Abc123"
    ///   }
    /// </summary>
    public class RegisterDto
    {
        [Required(ErrorMessage = "Full name is required.")]
        [MaxLength(100, ErrorMessage = "Full name cannot exceed 100 characters.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm password is required.")]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}

using System.ComponentModel.DataAnnotations;

namespace Gradely.Application.DTOs.Admin
{
    /// <summary>
    /// DTO for creating a teacher account from the admin panel.
    /// Used by POST /api/admin/teachers.
    ///
    /// Same validation rules as RegisterDto — the admin provides
    /// the teacher's name, email, and password.
    ///
    /// EXAMPLE JSON the admin sends:
    ///   {
    ///     "fullName": "Dr. Ahmed Hassan",
    ///     "email": "ahmed.hassan@gradely.com",
    ///     "password": "Abc123",
    ///     "confirmPassword": "Abc123"
    ///   }
    /// </summary>
    public class CreateTeacherDto
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

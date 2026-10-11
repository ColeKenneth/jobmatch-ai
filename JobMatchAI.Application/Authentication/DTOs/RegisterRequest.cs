using JobMatchAI.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace JobMatchAI.Application.Authentication.DTOs
{
    public record RegisterRequest
    {
        [Required(ErrorMessage = "First name is required.")]
        [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters.")]
        public string FirstName { get; init; } = string.Empty;

        [StringLength(100, ErrorMessage = "Middle name cannot exceed 100 characters.")]
        public string? MiddleName { get; init; }

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters.")]
        public string LastName { get; init; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters.")]
        public string Email { get; init; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 100 characters.")]
        public string Password { get; init; } = string.Empty;

        [EnumDataType(typeof(UserRole), ErrorMessage = "Invalid role.")]
        public UserRole Role { get; init; }
    }
}
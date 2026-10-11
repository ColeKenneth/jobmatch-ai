using System.ComponentModel.DataAnnotations;

namespace JobMatchAI.Application.Authentication.DTOs
{
    public record LoginRequest
    {
        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; init; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; init; } = string.Empty;
    }
}

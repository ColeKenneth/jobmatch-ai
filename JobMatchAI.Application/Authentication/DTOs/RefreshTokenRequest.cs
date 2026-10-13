using System.ComponentModel.DataAnnotations;

namespace JobMatchAI.Application.Authentication.DTOs
{
    public record RefreshTokenRequest
    {
        [Required(ErrorMessage = "Refresh token is required.")]
        public string RefreshToken { get; init; } = string.Empty;
    }
}

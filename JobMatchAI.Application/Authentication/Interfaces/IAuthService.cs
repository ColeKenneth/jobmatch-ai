using JobMatchAI.Application.Authentication.DTOs;

namespace JobMatchAI.Application.Authentication.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
    }
}

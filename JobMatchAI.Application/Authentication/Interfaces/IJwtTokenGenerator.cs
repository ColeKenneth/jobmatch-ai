using JobMatchAI.Domain.Entities;

namespace JobMatchAI.Application.Authentication.Interfaces
{
    public interface IJwtTokenGenerator
    {
        (string Token, DateTime ExpiresAt) GenerateToken(User user);
    }
}

namespace JobMatchAI.Application.Authentication.DTOs
{
    public record AuthResponse
    {
        public Guid UserId { get; init; }
        public string Email { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public string Role { get; init; } = string.Empty;
        public string Token { get; init; } = string.Empty;
        public DateTime ExpiresAt { get; init; }
        
    }
}

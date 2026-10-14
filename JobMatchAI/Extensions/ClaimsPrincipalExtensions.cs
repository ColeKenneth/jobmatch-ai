using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace JobMatchAI.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static Guid GetUserId(this ClaimsPrincipal user)
        {
            var sub = user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);

            return Guid.TryParse(sub, out var id) ? id : throw new UnauthorizedAccessException("User ID not found in token.");
        }

        public static string GetRole(this ClaimsPrincipal user)
        {
            return user.FindFirstValue(ClaimTypes.Role)
                ?? throw new UnauthorizedAccessException("Role not found in token.");
        }
    }
}

using System.Security.Claims;

namespace ArtemisBankingPro.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static string GetUserId(this ClaimsPrincipal principal)
            => principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    }
}

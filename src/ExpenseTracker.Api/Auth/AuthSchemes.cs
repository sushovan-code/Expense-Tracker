using System.Security.Claims;
using ExpenseTracker.Application.Features.Identity;
using ExpenseTracker.Application.Security;

namespace ExpenseTracker.Api.Auth;

internal static class AuthSchemes
{
    /// <summary>Default scheme: forwards to <see cref="ApiKey"/> when an <c>x-api-key</c> header is present, else to <see cref="Bearer"/>.</summary>
    public const string Default = "JwtOrApiKey";
    public const string Bearer = "Bearer";
    public const string ApiKey = "ApiKey";
    public const string ApiKeyHeader = "x-api-key";
}

using System.Security.Claims;
using ExpenseTracker.Api.Auth;
using ExpenseTracker.Api.Contracts;
using ExpenseTracker.Api.Http;
using ExpenseTracker.Application.Features.Identity;
using ExpenseTracker.Application.Security;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ExpenseTracker.Api.Endpoints;

/// <summary>
/// The refresh token lives only in an HttpOnly cookie (out of reach of page scripts), SameSite=Strict
/// (never sent on cross-site requests, so it can't be used for CSRF) and scoped to the auth endpoints.
/// </summary>
internal static class RefreshTokenCookie
{
    public const string Name = "et_refresh";
    private const string Path = "/api/auth";

    public static string? Read(HttpContext http) => http.Request.Cookies[Name];

    public static void Write(HttpContext http, string token, DateTimeOffset expiresAt) =>
        http.Response.Cookies.Append(Name, token, Options(http, expiresAt));

    public static void Clear(HttpContext http) => http.Response.Cookies.Delete(Name, Options(http, expiresAt: null));

    private static CookieOptions Options(HttpContext http, DateTimeOffset? expiresAt) => new()
    {
        HttpOnly = true,
        SameSite = SameSiteMode.Strict,
        Path = Path,
        Expires = expiresAt,
        IsEssential = true,
        // Always Secure, except plain-HTTP local development.
        Secure = http.Request.IsHttps || !http.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment(),
    };
}

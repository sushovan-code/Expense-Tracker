using System.Security.Claims;
using ExpenseTracker.Api.Auth;
using ExpenseTracker.Api.Contracts;
using ExpenseTracker.Api.Http;
using ExpenseTracker.Application.Features.Identity;
using ExpenseTracker.Application.Security;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ExpenseTracker.Api.Endpoints;

internal static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/auth").WithTags("Authentication");

        group.MapPost("/login", Login)
            .AllowAnonymous()
            .RequireRateLimiting(AuthSetup.AuthRateLimitPolicy)
            .WithName("Login")
            .WithSummary("Sign in with email and password")
            .WithDescription("Returns a short-lived access token and sets the refresh token as an HttpOnly, SameSite=Strict cookie. "
                + "Five failed attempts lock the account for 15 minutes.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        group.MapPost("/refresh", Refresh)
            .AllowAnonymous()
            .RequireRateLimiting(AuthSetup.AuthRateLimitPolicy)
            .WithName("RefreshSession")
            .WithSummary("Renew the access token")
            .WithDescription("Uses (and rotates) the refresh-token cookie. Re-using an already rotated token ends the session.")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        group.MapPost("/logout", Logout)
            .AllowAnonymous() // must work with an expired access token
            .WithName("Logout")
            .WithSummary("Sign out")
            .WithDescription("Revokes the session behind the refresh-token cookie and clears the cookie.");

        group.MapGet("/me", Me)
            .RequireAuthorization()
            .WithName("GetCurrentUser")
            .WithSummary("The signed-in user")
            .WithDescription("Identity, roles and effective permissions of the caller (for an API key: the permissions the key is limited to).")
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/change-password", ChangePassword)
            .RequireAuthorization(AuthPolicies.Interactive)
            .WithName("ChangePassword")
            .WithSummary("Change your password")
            .WithDescription("Signs out every other session.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        var keys = group.MapGroup("/api-keys")
            .WithTags("API keys")
            .RequireAuthorization(AuthPolicies.Interactive)
            .RequirePermission(Permissions.ApiKeys.Manage);

        keys.MapGet("/", ListApiKeys)
            .WithName("ListApiKeys")
            .WithSummary("Your API keys");

        keys.MapPost("/", CreateApiKey)
            .WithName("CreateApiKey")
            .WithSummary("Create an API key")
            .WithDescription($"The key is returned once; send it in the {AuthSchemes.ApiKeyHeader} header. Only its hash is stored.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);

        keys.MapDelete("/{id:guid}", RevokeApiKey)
            .WithName("RevokeApiKey")
            .WithSummary("Revoke an API key")
            .ProducesProblem(StatusCodes.Status404NotFound);

        return routes;
    }

    private static async Task<Results<Ok<AuthResponse>, ProblemHttpResult>> Login(
        LoginRequest request, IAuthService auth, HttpContext http, TimeProvider clock, CancellationToken ct)
    {
        var result = await auth.LoginAsync(request.ToCommand(), http.Connection.RemoteIpAddress?.ToString(), ct);
        return result.ToHttpResult(session =>
        {
            RefreshTokenCookie.Write(http, session.RefreshToken, session.RefreshTokenExpiresAt);
            return TypedResults.Ok(AuthResponse.From(session, clock.GetUtcNow()));
        });
    }

    private static async Task<Results<Ok<AuthResponse>, ProblemHttpResult>> Refresh(
        IAuthService auth, HttpContext http, TimeProvider clock, CancellationToken ct)
    {
        var token = RefreshTokenCookie.Read(http);
        if (string.IsNullOrEmpty(token))
            return TypedResults.Problem(title: "Not signed in", detail: "There is no session to refresh.", statusCode: StatusCodes.Status401Unauthorized);

        var result = await auth.RefreshAsync(token, http.Connection.RemoteIpAddress?.ToString(), ct);
        if (!result.IsSuccess) RefreshTokenCookie.Clear(http);

        return result.ToHttpResult(session =>
        {
            RefreshTokenCookie.Write(http, session.RefreshToken, session.RefreshTokenExpiresAt);
            return TypedResults.Ok(AuthResponse.From(session, clock.GetUtcNow()));
        });
    }

    private static async Task<NoContent> Logout(IAuthService auth, HttpContext http, CancellationToken ct)
    {
        await auth.LogoutAsync(RefreshTokenCookie.Read(http), ct);
        RefreshTokenCookie.Clear(http);
        return TypedResults.NoContent();
    }

    private static Ok<CurrentUserDto> Me(ClaimsPrincipal user) => TypedResults.Ok(user.ToCurrentUser());

    private static async Task<Results<NoContent, ProblemHttpResult>> ChangePassword(
        ChangePasswordRequest request, ClaimsPrincipal user, IAuthService auth, HttpContext http, CancellationToken ct) =>
        (await auth.ChangePasswordAsync(user.GetUserId(), request.ToCommand(), RefreshTokenCookie.Read(http), ct)).ToHttpResult();

    private static async Task<Ok<IReadOnlyList<ApiKeyDto>>> ListApiKeys(ClaimsPrincipal user, IApiKeyService apiKeys, CancellationToken ct) =>
        TypedResults.Ok(await apiKeys.ListAsync(user.GetUserId(), ct));

    private static async Task<Results<Created<CreatedApiKeyDto>, ProblemHttpResult>> CreateApiKey(
        CreateApiKeyRequest request, ClaimsPrincipal user, IApiKeyService apiKeys, CancellationToken ct) =>
        (await apiKeys.CreateAsync(user.ToActor(), request.ToCommand(), ct))
            .ToHttpResult(created => TypedResults.Created($"/api/auth/api-keys/{created.ApiKey.Id}", created));

    private static async Task<Results<NoContent, ProblemHttpResult>> RevokeApiKey(
        Guid id, ClaimsPrincipal user, IApiKeyService apiKeys, CancellationToken ct) =>
        (await apiKeys.RevokeAsync(user.GetUserId(), id, ct)).ToHttpResult();
}

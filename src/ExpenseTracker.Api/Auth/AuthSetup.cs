using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using ExpenseTracker.Api.Realtime;
using ExpenseTracker.Application.Abstractions.Identity;
using ExpenseTracker.Application.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ExpenseTracker.Api.Auth;

internal static class AuthSetup
{
    public const string AuthRateLimitPolicy = "auth";

    /// <summary>
    /// JWT bearer + API key authentication, permission/role-rank authorization that is deny-by-default,
    /// and a per-IP rate limit for the credential endpoints.
    /// </summary>
    public static IServiceCollection AddAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.Section))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();

        services.AddAuthentication(AuthSchemes.Default)
            .AddPolicyScheme(AuthSchemes.Default, "JWT bearer or API key", options =>
                options.ForwardDefaultSelector = context =>
                    context.Request.Headers.ContainsKey(AuthSchemes.ApiKeyHeader) ? AuthSchemes.ApiKey : AuthSchemes.Bearer)
            .AddJwtBearer(AuthSchemes.Bearer)
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(AuthSchemes.ApiKey, displayName: "API key", null);

        services.AddOptions<JwtBearerOptions>(AuthSchemes.Bearer).Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
        {
            var jwt = jwtOptions.Value;
            bearer.MapInboundClaims = false; // keep "sub", "role", ... instead of long WS-* claim URIs
            bearer.TokenValidationParameters = new TokenValidationParameters
            {
                ValidIssuer = jwt.Issuer,
                ValidAudience = jwt.Audience,
                IssuerSigningKey = jwt.CreateSigningKey(),
                ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                NameClaimType = AppClaimTypes.Name,
                RoleClaimType = AppClaimTypes.Role,
                ClockSkew = TimeSpan.FromSeconds(30),
            };
            bearer.Events = new JwtBearerEvents
            {
                OnMessageReceived = AcceptHubAccessToken,
                OnTokenValidated = AttachCurrentAccess,
            };
        });

        services.AddAuthorizationBuilder()
            // Secure by default: an endpoint without its own rule still requires a signed-in caller.
            // Public endpoints must opt out explicitly with AllowAnonymous().
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(AuthPolicies.AdminOrAbove, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new MinimumRoleRequirement(AppRoles.Admin)))
            .AddPolicy(AuthPolicies.Interactive, policy => policy
                .AddAuthenticationSchemes(AuthSchemes.Bearer)
                .RequireAuthenticatedUser());
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddSingleton<IAuthorizationHandler, MinimumRoleAuthorizationHandler>();

        // Brute-force and credential-stuffing brake on sign-in and refresh.
        var permitLimit = configuration.GetValue("Authentication:RateLimitPerMinute", 10);
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(AuthRateLimitPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = permitLimit, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
            options.OnRejected = (context, _) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
                }
                return ValueTask.CompletedTask;
            };
        });

        return services;
    }

    /// <summary>Browsers can't set headers on WebSocket requests, so SignalR sends the token in the query string.</summary>
    private static Task AcceptHubAccessToken(MessageReceivedContext context)
    {
        var token = context.Request.Query["access_token"].ToString();
        if (token.Length > 0 && context.HttpContext.Request.Path.StartsWithSegments(ExpenseHub.Path))
            context.Token = token;
        return Task.CompletedTask;
    }

    /// <summary>
    /// A valid signature isn't enough: the user must still exist and be active, and the token's security
    /// stamp must still match (it changes on password change/reset, deactivation and "sign out everywhere").
    /// Roles and permissions are then attached from the current, cached access snapshot.
    /// </summary>
    private static async Task AttachCurrentAccess(TokenValidatedContext context)
    {
        var principal = context.Principal!;
        if (!Guid.TryParse(principal.FindFirstValue(AppClaimTypes.Subject), out var userId))
        {
            context.Fail("The access token has no subject.");
            return;
        }

        var provider = context.HttpContext.RequestServices.GetRequiredService<IUserAccessProvider>();
        var access = await provider.GetAsync(userId, context.HttpContext.RequestAborted);
        if (access is null || !access.IsActive ||
            !string.Equals(access.SecurityStamp, principal.FindFirstValue(AppClaimTypes.SecurityStamp), StringComparison.Ordinal))
        {
            context.Fail("The session is no longer valid.");
            return;
        }

        AuthPrincipal.AddAccessClaims((ClaimsIdentity)principal.Identity!, access, access.Permissions);
    }
}

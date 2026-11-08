using System.Text.Encodings.Web;
using ExpenseTracker.Application.Abstractions.Identity;
using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Features.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace ExpenseTracker.Api.Auth;

/// <summary>Authenticates <c>x-api-key</c> requests as the key's owner, limited to the key's scopes.</summary>
internal sealed partial class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder,
    IApiKeyService apiKeys,
    IUserAccessProvider access) : AuthenticationHandler<AuthenticationSchemeOptions>(options, loggerFactory, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(AuthSchemes.ApiKeyHeader, out var header))
            return AuthenticateResult.NoResult();

        var presented = header.ToString().Trim();
        var key = presented.Length == 0 ? null : await apiKeys.ValidateAsync(presented, Context.RequestAborted);
        if (key is null)
        {
            LogApiKeyRejected(Logger, "unknown, revoked or expired key", null);
            return AuthenticateResult.Fail("Invalid, expired or revoked API key.");
        }

        var owner = await access.GetAsync(key.UserId, Context.RequestAborted);
        if (owner is null || !owner.IsActive)
        {
            LogApiKeyRejected(Logger, "owner is disabled or deleted", key.KeyId);
            return AuthenticateResult.Fail("The API key's owner is disabled.");
        }

        return AuthenticateResult.Success(new AuthenticationTicket(AuthPrincipal.ForApiKey(owner, key, Scheme.Name), Scheme.Name));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers.WWWAuthenticate = $"ApiKey header=\"{AuthSchemes.ApiKeyHeader}\"";
        return base.HandleChallengeAsync(properties);
    }

    [LoggerMessage(EventId = LogEvents.ApiKeyRejected, Level = LogLevel.Warning,
        Message = "Rejected API key {ApiKeyId}: {Reason}")]
    private static partial void LogApiKeyRejected(ILogger logger, string reason, Guid? apiKeyId);
}

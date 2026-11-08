using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Features.Identity;
using ExpenseTracker.Application.Security;

namespace ExpenseTracker.Api.Contracts;

/// <param name="AccessToken">Send as <c>Authorization: Bearer &lt;token&gt;</c>. The refresh token is set as an HttpOnly cookie.</param>
/// <param name="ExpiresIn">Access token lifetime in seconds.</param>
public sealed record AuthResponse(string AccessToken, string TokenType, int ExpiresIn, DateTimeOffset ExpiresAt, CurrentUserDto User)
{
    public static AuthResponse From(AuthSession session, DateTimeOffset now) => new(
        session.AccessToken, "Bearer", (int)Math.Max(0, (session.AccessTokenExpiresAt - now).TotalSeconds),
        session.AccessTokenExpiresAt, session.User);
}

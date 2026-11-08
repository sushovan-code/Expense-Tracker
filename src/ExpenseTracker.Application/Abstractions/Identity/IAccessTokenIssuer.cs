using ExpenseTracker.Application.Features.Identity;

namespace ExpenseTracker.Application.Abstractions.Identity;

/// <summary>Issues signed access tokens. Implemented by the API, which also validates them.</summary>
public interface IAccessTokenIssuer
{
    IssuedAccessToken Issue(AccessTokenSubject subject);
}

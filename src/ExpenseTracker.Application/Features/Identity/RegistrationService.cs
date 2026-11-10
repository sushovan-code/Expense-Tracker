using ExpenseTracker.Application.Abstractions.Identity;
using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Security;
using ExpenseTracker.Domain.Common;
using Microsoft.Extensions.Logging;

namespace ExpenseTracker.Application.Features.Identity;

/// <summary>Self-registration: anyone may create their own account.</summary>
public interface IRegistrationService
{
    /// <summary>Creates the account with the default app role. An email that is already registered is refused.</summary>
    Task<Result> RegisterAsync(RegisterCommand command, CancellationToken cancellationToken);
}

internal sealed partial class RegistrationService(
    IUserAccountStore accounts,
    TimeProvider clock,
    ILogger<RegistrationService> logger) : IRegistrationService
{
    private static readonly Error EmailTaken = Error.Validation("email", "An account with this email already exists.");

    public async Task<Result> RegisterAsync(RegisterCommand command, CancellationToken cancellationToken)
    {
        var email = command.Email.Trim();
        if (await accounts.FindByEmailAsync(email, cancellationToken) is not null) return EmailTaken;

        // Same default role as any other new user; registering never grants more than that.
        var created = await accounts.CreateAsync(
            new NewUserAccount(email, command.DisplayName.Trim(), clock.GetUtcNow()), command.Password, [AppRoles.User], cancellationToken);
        if (!created.IsSuccess) return created.Error;

        LogRegistered(logger, created.Value);
        return Result.Success();
    }

    [LoggerMessage(EventId = LogEvents.UserRegistered, Level = LogLevel.Information, Message = "User {UserId} registered an account")]
    private static partial void LogRegistered(ILogger logger, Guid userId);
}

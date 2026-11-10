using ExpenseTracker.Application.Abstractions.Identity;
using ExpenseTracker.Application.Features.Identity;
using ExpenseTracker.Application.Security;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ExpenseTracker.Application.Tests;

public class RegistrationServiceTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;
    private readonly IUserAccountStore _accounts = Substitute.For<IUserAccountStore>();
    private readonly RegistrationService _service;

    public RegistrationServiceTests() =>
        _service = new RegistrationService(_accounts, new FixedTimeProvider(), NullLogger<RegistrationService>.Instance);

    [Fact]
    public async Task Registering_creates_an_account_with_only_the_default_user_role()
    {
        _accounts.FindByEmailAsync("meera@example.com", Ct).Returns((UserAccount?)null);
        _accounts.CreateAsync(Arg.Any<NewUserAccount>(), "Password123", Arg.Any<IReadOnlyCollection<string>>(), Ct).Returns(TestIds.Of(7));

        var result = await _service.RegisterAsync(new RegisterCommand("  meera@example.com ", " Meera ", "Password123"), Ct);

        result.IsSuccess.ShouldBeTrue();
        await _accounts.Received(1).CreateAsync(
            Arg.Is<NewUserAccount>(a => a.Email == "meera@example.com" && a.DisplayName == "Meera"), "Password123",
            Arg.Is<IReadOnlyCollection<string>>(roles => roles.Single() == AppRoles.User), Ct);
    }

    [Fact]
    public async Task An_email_that_is_already_registered_is_refused_as_an_email_error()
    {
        _accounts.FindByEmailAsync("meera@example.com", Ct).Returns(new UserAccount(TestIds.Of(9), "meera@example.com", "Meera", true, null, 0, DateTimeOffset.UtcNow, null));

        var result = await _service.RegisterAsync(new RegisterCommand("meera@example.com", "Meera", "Password123"), Ct);

        result.Error!.Field.ShouldBe("email");
        await _accounts.DidNotReceiveWithAnyArgs().CreateAsync(default!, default!, default!, Ct);
    }
}

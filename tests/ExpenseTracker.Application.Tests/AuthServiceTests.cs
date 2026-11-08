using ExpenseTracker.Application.Abstractions.Identity;
using ExpenseTracker.Application.Features.Identity;
using ExpenseTracker.Application.Security;
using ExpenseTracker.Domain.Common;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ExpenseTracker.Application.Tests;

public class AuthServiceTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;
    private static readonly DateTimeOffset Now = new FixedTimeProvider().GetUtcNow();

    private readonly IUserAccountStore _accounts = Substitute.For<IUserAccountStore>();
    private readonly IUserCredentialStore _credentials = Substitute.For<IUserCredentialStore>();
    private readonly IRefreshTokenStore _refreshTokens = Substitute.For<IRefreshTokenStore>();
    private readonly IAccessTokenIssuer _tokens = Substitute.For<IAccessTokenIssuer>();
    private readonly IUserAccessProvider _access = Substitute.For<IUserAccessProvider>();
    private readonly AuthService _service;

    private static readonly UserAccount Alice =
        new(TestIds.Of(1), "alice@example.test", "Alice", IsActive: true, LockoutEnd: null, AccessFailedCount: 0, Now, LastLoginAt: null);

    public AuthServiceTests()
    {
        _service = new AuthService(_accounts, _credentials, _refreshTokens, _tokens, _access,
            new SessionPolicy(TimeSpan.FromDays(7)), new FixedTimeProvider(), NullLogger<AuthService>.Instance);
        _accounts.FindByEmailAsync(Alice.Email, Ct).Returns(Alice);
        _credentials.GetLockoutAsync(Alice.Id, Ct).Returns(new LockoutStatus(false, null));
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_SpendsAPasswordCheckAndFailsLikeAWrongPassword()
    {
        var result = await _service.LoginAsync(new LoginCommand("nobody@example.test", "whatever-123"), "127.0.0.1", Ct);

        result.Error!.Detail.ShouldBe("Invalid email or password.");
        _credentials.Received(1).SimulatePasswordCheck("whatever-123");
    }

    [Fact]
    public async Task LoginAsync_WrongPasswordThatTriggersLockout_ReportsTheLockout()
    {
        _credentials.CheckPasswordAsync(Alice.Id, "wrong-password", Ct).Returns(false);
        _credentials.GetLockoutAsync(Alice.Id, Ct).Returns(new LockoutStatus(false, null), new LockoutStatus(true, Now.AddMinutes(15)));

        var result = await _service.LoginAsync(new LoginCommand(Alice.Email, "wrong-password"), null, Ct);

        result.Error!.Title.ShouldBe("Account locked");
        await _credentials.Received(1).RecordFailedSignInAsync(Alice.Id, Ct);
        await _refreshTokens.DidNotReceiveWithAnyArgs().AddAsync(default!, Ct);
    }

    [Fact]
    public async Task LoginAsync_DisabledAccountWithRightPassword_IsRefusedWithoutASession()
    {
        _accounts.FindByEmailAsync(Alice.Email, Ct).Returns(Alice with { IsActive = false });
        _credentials.CheckPasswordAsync(Alice.Id, "right-password", Ct).Returns(true);

        var result = await _service.LoginAsync(new LoginCommand(Alice.Email, "right-password"), null, Ct);

        result.Error!.Title.ShouldBe("Account disabled");
        await _refreshTokens.DidNotReceiveWithAnyArgs().AddAsync(default!, Ct);
    }

    [Fact]
    public async Task RefreshAsync_RotatedTokenPresentedAgain_RevokesTheWholeSession()
    {
        var sessionId = TestIds.Of(50);
        _refreshTokens.FindByHashAsync(SecureTokens.Hash("stolen"), Ct).Returns(new RefreshTokenRecord(
            TestIds.Of(10), Alice.Id, sessionId, "hash", Now.AddHours(-1), Now.AddDays(6),
            RevokedAt: Now.AddMinutes(-5), ReplacedById: TestIds.Of(11), CreatedByIp: null));

        var result = await _service.RefreshAsync("stolen", "10.0.0.9", Ct);

        result.Error!.Kind.ShouldBe(ErrorKind.Unauthorized);
        await _refreshTokens.Received(1).RevokeSessionAsync(sessionId, Now, Ct);
        await _refreshTokens.DidNotReceiveWithAnyArgs().TryRotateAsync(default, default, default, Ct);
    }

    [Fact]
    public async Task RefreshAsync_TokenAlreadyClaimedByAConcurrentRefresh_IsRefused()
    {
        _refreshTokens.FindByHashAsync(SecureTokens.Hash("current"), Ct).Returns(new RefreshTokenRecord(
            TestIds.Of(10), Alice.Id, TestIds.Of(50), "hash", Now.AddHours(-1), Now.AddDays(6), null, null, null));
        _refreshTokens.TryRotateAsync(TestIds.Of(10), Arg.Any<Guid>(), Now, Ct).Returns(false);

        var result = await _service.RefreshAsync("current", null, Ct);

        result.Error!.Kind.ShouldBe(ErrorKind.Unauthorized);
        await _refreshTokens.DidNotReceiveWithAnyArgs().AddAsync(default!, Ct);
    }
}

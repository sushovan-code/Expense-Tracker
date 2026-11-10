using ExpenseTracker.Application.Abstractions.Families;
using ExpenseTracker.Application.Abstractions.Identity;
using ExpenseTracker.Application.Abstractions.Persistence;
using ExpenseTracker.Application.Features.Families;
using ExpenseTracker.Application.Security;
using ExpenseTracker.Domain.Categories;
using ExpenseTracker.Domain.Common;
using ExpenseTracker.Domain.Families;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ExpenseTracker.Application.Tests;

public class FamilyServiceTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;
    private static readonly Guid FamilyId = TestIds.Of(500);
    private static readonly Guid UserId = TestIds.Of(600);

    private readonly ICurrentFamily _current = Substitute.For<ICurrentFamily>();
    private readonly IFamilyRepository _families = Substitute.For<IFamilyRepository>();
    private readonly IFamilyQueries _queries = Substitute.For<IFamilyQueries>();
    private readonly ICategoryRepository _categories = Substitute.For<ICategoryRepository>();
    private readonly IUserAccountStore _accounts = Substitute.For<IUserAccountStore>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FamilyService _service;

    public FamilyServiceTests()
    {
        _current.UserId.Returns(UserId);
        _current.FamilyId.Returns(FamilyId);
        _current.Role.Returns(FamilyRole.Owner);
        _service = new FamilyService(_current, _families, _queries, _categories, _accounts, _unitOfWork,
            new FixedTimeProvider(), NullLogger<FamilyService>.Instance);
    }

    private void GivenNoFamily()
    {
        _current.FamilyId.Returns((Guid?)null);
        _current.Role.Returns((FamilyRole?)null);
    }

    private void GivenFamilyIsLoadable() =>
        _queries.GetAsync(Arg.Any<Guid>(), Arg.Any<DateTimeOffset>(), Ct).Returns(call => new FamilyDto(
            call.Arg<Guid>(), "Sharma", FamilyRole.Viewer,
            [new FamilyMemberDto(UserId, "Asha", "asha@tests.local", FamilyRole.Owner, DateTimeOffset.UtcNow)],
            [new InviteDto(TestIds.Of(1), FamilyRole.Member, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(48))]));

    [Fact]
    public async Task Creating_a_family_makes_the_creator_owner_and_seeds_the_default_categories()
    {
        GivenNoFamily();
        GivenFamilyIsLoadable();

        var result = await _service.CreateAsync("Sharma", Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.MyRole.ShouldBe(FamilyRole.Owner);
        _families.Received(1).Add(Arg.Is<FamilyMember>(m => m.UserId == UserId && m.Role == FamilyRole.Owner));
        _categories.Received(DefaultCategories.All.Count).Add(Arg.Any<Category>());
        await _unitOfWork.Received(1).SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task Someone_already_in_a_family_cannot_create_another()
    {
        var result = await _service.CreateAsync("Second", Ct);

        result.Error.ShouldBe(FamilyErrors.AlreadyInFamily);
    }

    [Theory]
    [InlineData(FamilyRole.Member)]
    [InlineData(FamilyRole.Viewer)]
    public async Task Only_owners_can_create_invites_or_add_members(FamilyRole role)
    {
        _current.Role.Returns(role);

        (await _service.CreateInviteAsync(FamilyRole.Member, Ct)).Error.ShouldBe(FamilyErrors.NotAllowed);
        (await _service.AddMemberAsync(new AddMemberCommand("a@b.c", "A", "Password123", FamilyRole.Member), Ct))
            .Error.ShouldBe(FamilyErrors.NotAllowed);
        await _accounts.DidNotReceiveWithAnyArgs().CreateAsync(default!, default!, default!, Ct);
    }

    [Fact]
    public async Task An_invite_stores_only_the_hash_of_its_token()
    {
        var result = await _service.CreateInviteAsync(FamilyRole.Viewer, Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Token.ShouldNotBeNullOrWhiteSpace();
        _families.Received(1).Add(Arg.Is<FamilyInvite>(i =>
            i.TokenHash == InviteTokens.Hash(result.Value.Token) && i.TokenHash != result.Value.Token));
    }

    [Fact]
    public async Task Adding_a_member_creates_an_account_with_the_default_app_role()
    {
        GivenFamilyIsLoadable();
        _accounts.CreateAsync(Arg.Any<NewUserAccount>(), "Password123", Arg.Any<IReadOnlyCollection<string>>(), Ct)
            .Returns(TestIds.Of(7));

        var result = await _service.AddMemberAsync(new AddMemberCommand("kid@tests.local", "Kid", "Password123", FamilyRole.Viewer), Ct);

        result.IsSuccess.ShouldBeTrue();
        await _accounts.Received(1).CreateAsync(Arg.Any<NewUserAccount>(), "Password123",
            Arg.Is<IReadOnlyCollection<string>>(roles => roles.Single() == AppRoles.User), Ct);
        _families.Received(1).Add(Arg.Is<FamilyMember>(m => m.UserId == TestIds.Of(7) && m.Role == FamilyRole.Viewer));
    }

    [Fact]
    public async Task Accepting_an_unknown_invite_creates_no_account()
    {
        _families.FindInviteByHashAsync(Arg.Any<string>(), Ct).Returns((FamilyInvite?)null);

        var result = await _service.AcceptInviteAsync(new AcceptInviteCommand("nope", "a@b.c", "A", "Password123"), Ct);

        result.Error.ShouldBe(FamilyErrors.InvalidInvite);
        await _accounts.DidNotReceiveWithAnyArgs().CreateAsync(default!, default!, default!, Ct);
    }

    [Fact]
    public async Task Accepting_an_invite_creates_the_account_joins_the_family_and_uses_up_the_invite()
    {
        var now = new FixedTimeProvider().GetUtcNow();
        var invite = FamilyInvite.Create(FamilyId, FamilyRole.Member, InviteTokens.Hash("secret"), UserId, now);
        _families.FindInviteByHashAsync(InviteTokens.Hash("secret"), Ct).Returns(invite);
        _accounts.CreateAsync(Arg.Any<NewUserAccount>(), Arg.Any<string>(), Arg.Any<IReadOnlyCollection<string>>(), Ct)
            .Returns(TestIds.Of(8));

        var first = await _service.AcceptInviteAsync(new AcceptInviteCommand("secret", "new@tests.local", "New", "Password123"), Ct);
        var second = await _service.AcceptInviteAsync(new AcceptInviteCommand("secret", "other@tests.local", "Other", "Password123"), Ct);

        first.IsSuccess.ShouldBeTrue();
        _families.Received(1).Add(Arg.Is<FamilyMember>(m => m.UserId == TestIds.Of(8) && m.Role == FamilyRole.Member));
        second.Error.ShouldBe(FamilyErrors.InvalidInvite);
    }

    [Fact]
    public async Task The_last_owner_cannot_be_demoted_or_leave()
    {
        var owner = FamilyMember.Create(FamilyId, UserId, FamilyRole.Owner, DateTimeOffset.UtcNow);
        _families.GetMemberAsync(FamilyId, UserId, Ct).Returns(owner);
        _families.CountOwnersAsync(FamilyId, Ct).Returns(1);

        (await _service.ChangeRoleAsync(UserId, FamilyRole.Member, Ct)).Error.ShouldBe(FamilyErrors.LastOwner);
        (await _service.LeaveAsync(Ct)).Error.ShouldBe(FamilyErrors.LastOwner);
        _families.DidNotReceiveWithAnyArgs().Remove(default!);
    }

    [Fact]
    public async Task A_member_can_leave_but_cannot_remove_someone_else()
    {
        _current.Role.Returns(FamilyRole.Member);
        var me = FamilyMember.Create(FamilyId, UserId, FamilyRole.Member, DateTimeOffset.UtcNow);
        _families.GetMemberAsync(FamilyId, UserId, Ct).Returns(me);

        (await _service.RemoveMemberAsync(TestIds.Of(9), Ct)).Error.ShouldBe(FamilyErrors.NotAllowed);
        (await _service.LeaveAsync(Ct)).IsSuccess.ShouldBeTrue();
        _families.Received(1).Remove(me);
    }
}

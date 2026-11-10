using ExpenseTracker.Domain.Families;

namespace ExpenseTracker.Domain.Tests;

public class FamilyTests
{
    private static FamilyInvite NewInvite(DateTimeOffset now) =>
        FamilyInvite.Create(Guid.NewGuid(), FamilyRole.Member, "hash", Guid.NewGuid(), now);

    [Fact]
    public void Family_name_is_trimmed_and_required()
    {
        Family.Create("  Sharma  ", DateTimeOffset.UtcNow).Value!.Name.ShouldBe("Sharma");
        Family.Create("   ", DateTimeOffset.UtcNow).Error!.Field.ShouldBe("name");
        Family.Create(new string('x', Family.NameMaxLength + 1), DateTimeOffset.UtcNow).IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public void An_invite_is_valid_for_48_hours_and_not_a_minute_longer()
    {
        var created = new DateTimeOffset(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);
        var invite = NewInvite(created);

        invite.IsUsable(created + TimeSpan.FromHours(47) + TimeSpan.FromMinutes(59)).ShouldBeTrue();

        var expired = created + TimeSpan.FromHours(48);
        invite.IsUsable(expired).ShouldBeFalse();
        invite.Accept(expired).Error.ShouldBe(FamilyErrors.InvalidInvite);
    }

    [Fact]
    public void An_invite_works_once()
    {
        var now = DateTimeOffset.UtcNow;
        var invite = NewInvite(now);

        invite.Accept(now).IsSuccess.ShouldBeTrue();
        invite.Accept(now).Error.ShouldBe(FamilyErrors.InvalidInvite);
    }

    [Fact]
    public void A_revoked_invite_cannot_be_accepted()
    {
        var now = DateTimeOffset.UtcNow;
        var invite = NewInvite(now);

        invite.Revoke(now);

        invite.Accept(now).Error.ShouldBe(FamilyErrors.InvalidInvite);
    }
}

public class FamilyPolicyTests
{
    [Theory]
    [InlineData(FamilyRole.Owner, true)]
    [InlineData(FamilyRole.Member, false)]
    [InlineData(FamilyRole.Viewer, false)]
    public void Only_owners_manage_the_family(FamilyRole role, bool expected) =>
        FamilyPolicy.CanManageFamily(role).ShouldBe(expected);

    [Theory]
    [InlineData(FamilyRole.Owner, true)]
    [InlineData(FamilyRole.Member, true)]
    [InlineData(FamilyRole.Viewer, false)]
    public void Owners_and_members_add_expenses(FamilyRole role, bool expected) =>
        FamilyPolicy.CanAddExpense(role).ShouldBe(expected);

    [Theory]
    [InlineData(FamilyRole.Owner, true, true)]
    [InlineData(FamilyRole.Owner, false, true)]
    [InlineData(FamilyRole.Member, true, true)]
    [InlineData(FamilyRole.Member, false, false)]
    [InlineData(FamilyRole.Viewer, true, false)]
    [InlineData(FamilyRole.Viewer, false, false)]
    public void Changing_an_expense_depends_on_role_and_ownership(FamilyRole role, bool isOwn, bool expected) =>
        FamilyPolicy.CanChangeExpense(role, isOwn).ShouldBe(expected);

    [Theory]
    [InlineData(FamilyRole.Owner, FamilyRole.Member, 1, true)]
    [InlineData(FamilyRole.Owner, null, 1, true)]
    [InlineData(FamilyRole.Owner, FamilyRole.Member, 2, false)]
    [InlineData(FamilyRole.Owner, FamilyRole.Owner, 1, false)]
    [InlineData(FamilyRole.Member, null, 1, false)]
    public void The_last_owner_cannot_leave_or_be_demoted(FamilyRole current, FamilyRole? newRole, int owners, bool expected) =>
        FamilyPolicy.WouldLeaveNoOwner(current, newRole, owners).ShouldBe(expected);
}

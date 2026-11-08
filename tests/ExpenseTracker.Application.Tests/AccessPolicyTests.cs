using ExpenseTracker.Application.Security;
using ExpenseTracker.Domain.Common;

namespace ExpenseTracker.Application.Tests;

public class AccessPolicyTests
{
    private static Actor ActorWith(string role, params string[] permissions) =>
        new(Guid.NewGuid(), [role], (permissions.Length > 0 ? permissions : AppRoles.Find(role)!.DefaultPermissions).ToHashSet());

    [Fact]
    public void Roles_are_ranked_SuperAdmin_Admin_User_Reader()
    {
        AppRoles.All.Select(r => r.Name).ShouldBe([AppRoles.SuperAdmin, AppRoles.Admin, AppRoles.User, AppRoles.Reader]);
        AppRoles.All.Select(r => r.Rank).ShouldBeInOrder(SortDirection.Descending);
        AppRoles.HighestRank([AppRoles.Reader, AppRoles.Admin]).ShouldBe(AppRoles.RankOf(AppRoles.Admin));
        AppRoles.HighestRank([]).ShouldBe(0);
    }

    [Fact]
    public void Every_default_permission_is_in_the_catalog()
    {
        AppRoles.All.SelectMany(r => r.DefaultPermissions).ShouldAllBe(p => Permissions.IsKnown(p));
    }

    [Fact]
    public void Readers_only_read()
    {
        var reader = AppRoles.Find(AppRoles.Reader)!.DefaultPermissions;
        reader.ShouldContain(Permissions.Expenses.View);
        reader.ShouldNotContain(Permissions.Expenses.Add);
        reader.ShouldNotContain(Permissions.Expenses.Delete);
        reader.ShouldNotContain(Permissions.Users.View);
    }

    [Fact]
    public void Nobody_manages_their_own_account()
    {
        var superAdmin = ActorWith(AppRoles.SuperAdmin);

        AccessPolicy.CanManageUser(superAdmin, superAdmin.UserId, [AppRoles.SuperAdmin])!.Kind.ShouldBe(ErrorKind.Forbidden);
    }

    [Theory]
    [InlineData(AppRoles.User, true)]
    [InlineData(AppRoles.Reader, true)]
    [InlineData(AppRoles.Admin, false)]
    [InlineData(AppRoles.SuperAdmin, false)]
    public void Admins_manage_only_users_ranked_below_them(string targetRole, bool allowed)
    {
        var admin = ActorWith(AppRoles.Admin);

        (AccessPolicy.CanManageUser(admin, Guid.NewGuid(), [targetRole]) is null).ShouldBe(allowed);
    }

    [Fact]
    public void SuperAdmins_manage_everyone_else()
    {
        var superAdmin = ActorWith(AppRoles.SuperAdmin);

        AccessPolicy.CanManageUser(superAdmin, Guid.NewGuid(), [AppRoles.SuperAdmin]).ShouldBeNull();
    }

    [Theory]
    [InlineData(AppRoles.Reader, true)]
    [InlineData(AppRoles.User, true)]
    [InlineData(AppRoles.Admin, false)]
    [InlineData(AppRoles.SuperAdmin, false)]
    public void Admins_assign_only_roles_below_their_own(string role, bool allowed)
    {
        var admin = ActorWith(AppRoles.Admin);

        (AccessPolicy.CanAssignRoles(admin, [role]) is null).ShouldBe(allowed);
    }

    [Fact]
    public void SuperAdmins_assign_any_role_and_unknown_roles_are_invalid()
    {
        var superAdmin = ActorWith(AppRoles.SuperAdmin);

        AccessPolicy.CanAssignRoles(superAdmin, [AppRoles.SuperAdmin, AppRoles.Admin]).ShouldBeNull();
        AccessPolicy.CanAssignRoles(superAdmin, ["Owner"])!.Kind.ShouldBe(ErrorKind.Validation);
    }

    [Fact]
    public void Permissions_can_only_be_granted_by_someone_who_holds_them()
    {
        var limitedAdmin = ActorWith(AppRoles.Admin, Permissions.Expenses.View, Permissions.Users.Manage);

        AccessPolicy.CanGrantPermissions(limitedAdmin, [Permissions.Expenses.View]).ShouldBeNull();
        AccessPolicy.CanGrantPermissions(limitedAdmin, [Permissions.Expenses.Delete])!.Kind.ShouldBe(ErrorKind.Forbidden);
        AccessPolicy.CanGrantPermissions(limitedAdmin, ["expense.fly"])!.Kind.ShouldBe(ErrorKind.Validation);
    }

    [Theory]
    [InlineData(AppRoles.Admin, AppRoles.User, true)]
    [InlineData(AppRoles.Admin, AppRoles.Reader, true)]
    [InlineData(AppRoles.Admin, AppRoles.Admin, false)]
    [InlineData(AppRoles.SuperAdmin, AppRoles.Admin, true)]
    [InlineData(AppRoles.SuperAdmin, AppRoles.SuperAdmin, false)]
    public void Roles_are_editable_only_below_your_rank_and_SuperAdmin_never(string actorRole, string role, bool allowed)
    {
        (AccessPolicy.CanEditRole(ActorWith(actorRole), role) is null).ShouldBe(allowed);
    }
}

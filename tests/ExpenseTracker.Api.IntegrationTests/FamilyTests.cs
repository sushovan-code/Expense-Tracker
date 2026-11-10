using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ExpenseTracker.Api.IntegrationTests;

/// <summary>
/// Family isolation end to end: family A (user@ Owner, admin@ Member, reader@ Viewer) and family B
/// (superadmin@ Owner) must never see each other's expenses or categories, and roles limit what each person may do.
/// </summary>
public sealed class FamilyTests(ApiFactory factory) : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private static readonly CancellationToken Ct = CancellationToken.None;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private HttpClient _ownerA = null!;
    private HttpClient _memberA = null!;
    private HttpClient _viewerA = null!;
    private HttpClient _ownerB = null!;

    private sealed record ExpenseDto(Guid Id, string Description, decimal Amount, Guid CategoryId, bool IsPrivate, Guid CreatedByUserId);
    private sealed record ExpensePage(List<ExpenseDto> Items, int TotalCount, decimal TotalAmount);
    private sealed record Summary(decimal Total, int Count);
    private sealed record BudgetOverview(List<object> Categories);
    private sealed record Lookup(Guid Id, string Name);
    private sealed record Member(Guid UserId, string Email, string Role);
    private sealed record Family(Guid Id, string Name, string MyRole, List<Member> Members, List<object> Invites);
    private sealed record Invite(Guid Id, string Token, string Role);
    private sealed record BulkResult(int Deleted);

    public async ValueTask InitializeAsync()
    {
        await factory.EnsureFamiliesAsync();
        _ownerA = await factory.CreateClientForAsync(ApiFactory.FamilyAOwnerEmail);
        _memberA = await factory.CreateClientForAsync(ApiFactory.AdminEmail);
        _viewerA = await factory.CreateClientForAsync(ApiFactory.ReaderEmail);
        _ownerB = await factory.CreateClientForAsync(ApiFactory.FamilyBOwnerEmail, ApiFactory.SuperAdminPassword);
    }

    public ValueTask DisposeAsync()
    {
        _ownerA.Dispose();
        _memberA.Dispose();
        _viewerA.Dispose();
        _ownerB.Dispose();
        return ValueTask.CompletedTask;
    }

    private async Task<ExpenseDto> AddExpenseAsync(HttpClient client, Guid categoryId, string description, bool isPrivate = false, decimal amount = 25m)
    {
        var response = await client.PostAsJsonAsync("/api/v1/expenses",
            new { description, amount, date = "2026-10-05", categoryId, isPrivate }, Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<ExpenseDto>(Json, Ct))!;
    }

    private static async Task<List<ExpenseDto>> ListAsync(HttpClient client, string search) =>
        (await client.GetFromJsonAsync<ExpensePage>($"/api/v1/expenses?search={search}&pageSize=100", Json, Ct))!.Items;

    [Fact]
    public async Task Another_family_sees_nothing_of_my_expenses()
    {
        var expense = await AddExpenseAsync(_ownerA, factory.FamilyAFoodCategoryId, "iso-a-secret", amount: 1234m);

        (await _ownerB.GetAsync($"/api/v1/expenses/{expense.Id}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await ListAsync(_ownerB, "iso-a-secret")).ShouldBeEmpty();

        var update = new { description = "hijacked", amount = 1m, date = "2026-10-05", categoryId = factory.FamilyBFoodCategoryId };
        (await _ownerB.PutAsJsonAsync($"/api/v1/expenses/{expense.Id}", update, Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await _ownerB.DeleteAsync($"/api/v1/expenses/{expense.Id}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var bulk = await _ownerB.PostAsJsonAsync("/api/v1/expenses/bulk-delete", new { ids = new[] { expense.Id } }, Ct);
        (await bulk.Content.ReadFromJsonAsync<BulkResult>(Json, Ct))!.Deleted.ShouldBe(0);

        var summary = await _ownerB.GetFromJsonAsync<Summary>("/api/v1/expenses/summary", Json, Ct);
        summary!.Total.ShouldBeLessThan(1234m);
        (await _ownerB.GetStringAsync("/api/v1/expenses/export?search=iso-a-secret", Ct)).ShouldNotContain("iso-a-secret");

        // Still there for family A.
        (await ListAsync(_ownerA, "iso-a-secret")).Count.ShouldBe(1);
    }

    [Fact]
    public async Task Another_family_sees_nothing_of_my_categories_or_budgets()
    {
        var created = await _ownerA.PostAsJsonAsync("/api/v1/categories",
            new { name = "Iso-A-Only", color = "#112233", monthlyBudget = 500m }, Ct);
        created.StatusCode.ShouldBe(HttpStatusCode.Created);
        var category = (await created.Content.ReadFromJsonAsync<Lookup>(Json, Ct))!;

        var seenByB = await _ownerB.GetFromJsonAsync<List<Lookup>>("/api/v1/categories/lookup", Json, Ct);
        seenByB!.ShouldNotContain(c => c.Id == category.Id);

        (await _ownerB.PutAsJsonAsync($"/api/v1/categories/{category.Id}", new { name = "Taken", color = "#112233" }, Ct))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await _ownerB.DeleteAsync($"/api/v1/categories/{category.Id}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var budgets = await _ownerB.GetFromJsonAsync<BudgetOverview>("/api/v1/budgets?year=2026&month=10", Json, Ct);
        budgets!.Categories.ShouldNotContain(c => c.ToString()!.Contains(category.Id.ToString(), StringComparison.OrdinalIgnoreCase));

        // Family B can't file an expense under family A's category either.
        var foreign = await _ownerB.PostAsJsonAsync("/api/v1/expenses",
            new { description = "x", amount = 1m, date = "2026-10-05", categoryId = category.Id }, Ct);
        foreign.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Both_families_can_use_the_same_category_name()
    {
        var names = await _ownerB.GetFromJsonAsync<List<Lookup>>("/api/v1/categories/lookup", Json, Ct);

        names!.Select(c => c.Name).ShouldContain("Food");
        factory.FamilyBFoodCategoryId.ShouldNotBe(factory.FamilyAFoodCategoryId);
    }

    [Fact]
    public async Task A_private_expense_is_visible_only_to_its_creator_even_for_the_owner()
    {
        var secret = await AddExpenseAsync(_memberA, factory.FamilyAFoodCategoryId, "iso-private-gift", isPrivate: true);

        (await ListAsync(_memberA, "iso-private-gift")).Count.ShouldBe(1);
        (await ListAsync(_ownerA, "iso-private-gift")).ShouldBeEmpty();
        (await ListAsync(_viewerA, "iso-private-gift")).ShouldBeEmpty();
        (await _ownerA.GetAsync($"/api/v1/expenses/{secret.Id}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await _ownerA.DeleteAsync($"/api/v1/expenses/{secret.Id}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Roles_limit_what_people_can_do_inside_the_family()
    {
        var ownersExpense = await AddExpenseAsync(_ownerA, factory.FamilyAFoodCategoryId, "iso-role-owner");
        var update = new { description = "changed", amount = 2m, date = "2026-10-05", categoryId = factory.FamilyAFoodCategoryId };

        // Viewer: read only.
        (await ListAsync(_viewerA, "iso-role-owner")).Count.ShouldBe(1);
        (await _viewerA.PostAsJsonAsync("/api/v1/expenses",
            new { description = "no", amount = 1m, date = "2026-10-05", categoryId = factory.FamilyAFoodCategoryId }, Ct))
            .StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        // Member: adds, but can't change the owner's expense or manage categories.
        (await _memberA.PutAsJsonAsync($"/api/v1/expenses/{ownersExpense.Id}", update, Ct)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await _memberA.DeleteAsync($"/api/v1/expenses/{ownersExpense.Id}", Ct)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await _memberA.PostAsJsonAsync("/api/v1/categories", new { name = "Nope", color = "#112233" }, Ct)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        // Member edits their own; the owner may edit anyone's.
        var membersExpense = await AddExpenseAsync(_memberA, factory.FamilyAFoodCategoryId, "iso-role-member");
        (await _memberA.PutAsJsonAsync($"/api/v1/expenses/{membersExpense.Id}", update, Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await _ownerA.DeleteAsync($"/api/v1/expenses/{membersExpense.Id}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task The_member_filter_narrows_the_list_to_one_person()
    {
        var mine = await AddExpenseAsync(_ownerA, factory.FamilyAFoodCategoryId, "iso-filter-owner");
        var theirs = await AddExpenseAsync(_memberA, factory.FamilyAFoodCategoryId, "iso-filter-member");

        var onlyMember = (await _ownerA.GetFromJsonAsync<ExpensePage>(
            $"/api/v1/expenses?search=iso-filter&memberId={theirs.CreatedByUserId}", Json, Ct))!.Items;

        onlyMember.Select(e => e.Id).ShouldBe([theirs.Id]);
        (await ListAsync(_ownerA, "iso-filter")).Select(e => e.Id).ShouldContain(mine.Id);
    }

    [Fact]
    public async Task Someone_without_a_family_sees_nothing_and_must_create_or_join_one()
    {
        using var admin = await factory.CreateClientForAsync(ApiFactory.SuperAdminEmail, ApiFactory.SuperAdminPassword);
        var email = $"nofamily-{Guid.NewGuid():N}@tests.local";
        (await admin.PostAsJsonAsync("/api/v1/admin/users",
            new { email, displayName = "Loner", password = ApiFactory.DemoPassword, roles = new[] { "User" } }, Ct)).EnsureSuccessStatusCode();
        using var loner = await factory.CreateClientForAsync(email);

        (await loner.GetFromJsonAsync<ExpensePage>("/api/v1/expenses", Json, Ct))!.TotalCount.ShouldBe(0);
        (await loner.GetFromJsonAsync<List<Lookup>>("/api/v1/categories/lookup", Json, Ct))!.ShouldBeEmpty();
        (await loner.GetAsync("/api/v1/families/mine", Ct)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await loner.PostAsJsonAsync("/api/v1/expenses",
            new { description = "x", amount = 1m, date = "2026-10-05", categoryId = factory.FamilyAFoodCategoryId }, Ct))
            .StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        var created = await loner.PostAsJsonAsync("/api/v1/families", new { name = "Loner family" }, Ct);
        created.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await created.Content.ReadFromJsonAsync<Family>(Json, Ct))!.MyRole.ShouldBe("owner");
        (await loner.GetFromJsonAsync<List<Lookup>>("/api/v1/categories/lookup", Json, Ct))!.Count.ShouldBe(8);
    }

    [Fact]
    public async Task An_invite_creates_an_account_joins_the_family_and_works_only_once()
    {
        var invite = await CreateInviteAsync("viewer");
        var email = $"invited-{Guid.NewGuid():N}@tests.local";
        var accept = new { token = invite.Token, email, displayName = "Invited", password = ApiFactory.DemoPassword };

        using var anonymous = factory.CreateAnonymousClient();
        (await anonymous.PostAsJsonAsync("/api/v1/families/invites/accept", accept, Ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var invited = await factory.CreateClientForAsync(email);
        var family = await invited.GetFromJsonAsync<Family>("/api/v1/families/mine", Json, Ct);
        family!.Name.ShouldBe("Family A");
        family.MyRole.ShouldBe("viewer");
        family.Invites.ShouldBeEmpty(); // only owners see open invites
        (await invited.GetFromJsonAsync<List<Lookup>>("/api/v1/categories/lookup", Json, Ct))!.ShouldContain(c => c.Id == factory.FamilyAFoodCategoryId);

        var again = new { token = invite.Token, email = $"again-{Guid.NewGuid():N}@tests.local", displayName = "Again", password = ApiFactory.DemoPassword };
        (await anonymous.PostAsJsonAsync("/api/v1/families/invites/accept", again, Ct)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task A_revoked_or_unknown_invite_is_refused()
    {
        var invite = await CreateInviteAsync("member");
        (await _ownerA.DeleteAsync($"/api/v1/families/invites/{invite.Id}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var anonymous = factory.CreateAnonymousClient();
        foreach (var token in new[] { invite.Token, "not-a-real-token" })
        {
            var body = new { token, email = $"x-{Guid.NewGuid():N}@tests.local", displayName = "X", password = ApiFactory.DemoPassword };
            (await anonymous.PostAsJsonAsync("/api/v1/families/invites/accept", body, Ct)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        }
    }

    [Fact]
    public async Task Only_owners_manage_members_and_invites()
    {
        (await _memberA.PostAsJsonAsync("/api/v1/families/invites", new { role = "member" }, Ct)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await _viewerA.PutAsJsonAsync("/api/v1/families/mine", new { name = "Mine now" }, Ct)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        var family = await _ownerA.GetFromJsonAsync<Family>("/api/v1/families/mine", Json, Ct);
        var owner = family!.Members.Single(m => m.Email == ApiFactory.FamilyAOwnerEmail);
        (await _ownerA.PutAsJsonAsync($"/api/v1/families/members/{owner.UserId}/role", new { role = "member" }, Ct))
            .StatusCode.ShouldBe(HttpStatusCode.Conflict); // the last owner can't be demoted
    }

    [Fact]
    public async Task Every_family_endpoint_requires_sign_in_except_accepting_an_invite()
    {
        using var anonymous = factory.CreateAnonymousClient();
        var id = Guid.NewGuid();

        (await anonymous.GetAsync("/api/v1/families/mine", Ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await anonymous.PostAsJsonAsync("/api/v1/families", new { name = "x" }, Ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await anonymous.PutAsJsonAsync("/api/v1/families/mine", new { name = "x" }, Ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await anonymous.PostAsJsonAsync("/api/v1/families/members", new { }, Ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await anonymous.PutAsJsonAsync($"/api/v1/families/members/{id}/role", new { role = "member" }, Ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await anonymous.DeleteAsync($"/api/v1/families/members/{id}", Ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await anonymous.PostAsync("/api/v1/families/leave", null, Ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await anonymous.PostAsJsonAsync("/api/v1/families/invites", new { role = "member" }, Ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await anonymous.DeleteAsync($"/api/v1/families/invites/{id}", Ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await anonymous.PostAsJsonAsync("/api/v1/families/invites/join", new { token = "x" }, Ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await anonymous.PostAsJsonAsync("/api/v1/families/invites/accept", new { token = "x", email = "a@b.co", displayName = "A", password = "Password123" }, Ct))
            .StatusCode.ShouldBe(HttpStatusCode.Conflict); // reachable without signing in, but the token is checked
    }

    private async Task<Invite> CreateInviteAsync(string role)
    {
        var response = await _ownerA.PostAsJsonAsync("/api/v1/families/invites", new { role }, Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<Invite>(Json, Ct))!;
    }
}

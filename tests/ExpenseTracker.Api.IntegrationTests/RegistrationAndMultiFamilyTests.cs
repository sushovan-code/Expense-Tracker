using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ExpenseTracker.Api.IntegrationTests;

/// <summary>
/// Self-registration, and one person owning several families with separate data in each. A person may be a
/// Member or Viewer in only one family.
/// </summary>
public sealed class RegistrationAndMultiFamilyTests(ApiFactory factory) : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private static readonly CancellationToken Ct = CancellationToken.None;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private const string FamilyHeader = "X-Family-Id";

    private sealed record FamilySummary(Guid Id, string Name, string Role);
    private sealed record Family(Guid Id, string Name, string MyRole);
    private sealed record Lookup(Guid Id, string Name);
    private sealed record ExpenseDto(Guid Id, string Description);
    private sealed record ExpensePage(List<ExpenseDto> Items, int TotalCount);
    private sealed record Invite(Guid Id, string Token);
    private sealed record Problem(int Status, Dictionary<string, string[]>? Errors);

    public ValueTask InitializeAsync() => new(factory.EnsureFamiliesAsync());

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static string NewEmail(string prefix) => $"{prefix}-{Guid.NewGuid():N}@tests.local";

    /// <summary>Registers through the public endpoint, then signs in like a real person.</summary>
    private async Task<HttpClient> RegisterAndSignInAsync(string email)
    {
        using var anonymous = factory.CreateAnonymousClient();
        var response = await anonymous.PostAsJsonAsync("/api/v1/auth/register",
            new { email, displayName = "New Person", password = ApiFactory.DemoPassword }, Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        return await factory.CreateClientForAsync(email);
    }

    private static async Task<Family> CreateFamilyAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/v1/families", new { name }, Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<Family>(Json, Ct))!;
    }

    private static HttpRequestMessage In(Guid familyId, HttpMethod method, string url, object? body = null) =>
        new(method, url)
        {
            Headers = { { FamilyHeader, familyId.ToString() } },
            Content = body is null ? null : JsonContent.Create(body),
        };

    private static async Task<T> SendAsync<T>(HttpClient client, HttpRequestMessage request)
    {
        var response = await client.SendAsync(request, Ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>(Json, Ct))!;
    }

    private async Task<Invite> InviteAsync(HttpClient owner, Guid familyId, string role)
    {
        var response = await owner.SendAsync(In(familyId, HttpMethod.Post, "/api/v1/families/invites", new { role }), Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<Invite>(Json, Ct))!;
    }

    [Fact]
    public async Task Anyone_can_register_and_then_sign_in()
    {
        var email = NewEmail("self");
        using var client = await RegisterAndSignInAsync(email);

        var me = await client.GetFromJsonAsync<JsonElement>("/api/v1/auth/me", Json, Ct);

        me.GetProperty("email").GetString().ShouldBe(email);
        me.GetProperty("roles").EnumerateArray().Select(r => r.GetString()).ShouldBe(["User"]);
    }

    [Fact]
    public async Task Registering_with_an_email_that_exists_is_refused()
    {
        using var anonymous = factory.CreateAnonymousClient();
        var body = new { email = ApiFactory.UserEmail, displayName = "Copycat", password = ApiFactory.DemoPassword };

        var response = await anonymous.PostAsJsonAsync("/api/v1/auth/register", body, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<Problem>(Json, Ct))!.Errors!.ShouldContainKey("email");
    }

    [Fact]
    public async Task Registering_twice_with_the_same_new_email_only_works_once()
    {
        var email = NewEmail("twice");
        using var first = await RegisterAndSignInAsync(email);
        using var anonymous = factory.CreateAnonymousClient();

        var again = await anonymous.PostAsJsonAsync("/api/v1/auth/register",
            new { email = email.ToUpperInvariant(), displayName = "Again", password = ApiFactory.DemoPassword }, Ct);

        again.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Registering_enforces_the_password_rules_and_a_valid_email()
    {
        using var anonymous = factory.CreateAnonymousClient();

        var weak = await anonymous.PostAsJsonAsync("/api/v1/auth/register",
            new { email = NewEmail("weak"), displayName = "Weak", password = "short" }, Ct);
        var badEmail = await anonymous.PostAsJsonAsync("/api/v1/auth/register",
            new { email = "not-an-email", displayName = "Bad", password = ApiFactory.DemoPassword }, Ct);

        weak.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        badEmail.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task A_new_person_can_own_several_families_with_separate_expenses()
    {
        using var person = await RegisterAndSignInAsync(NewEmail("owner"));
        var home = await CreateFamilyAsync(person, "Me and my wife");
        var parents = await CreateFamilyAsync(person, "My parents");

        var mine = await person.GetFromJsonAsync<List<FamilySummary>>("/api/v1/families", Json, Ct);
        mine!.Select(f => (f.Name, f.Role)).ShouldBe([("Me and my wife", "owner"), ("My parents", "owner")], ignoreOrder: true);

        // Each family has its own categories and expenses.
        var homeFood = (await SendAsync<List<Lookup>>(person, In(home.Id, HttpMethod.Get, "/api/v1/categories/lookup"))).Single(c => c.Name == "Food").Id;
        var parentsFood = (await SendAsync<List<Lookup>>(person, In(parents.Id, HttpMethod.Get, "/api/v1/categories/lookup"))).Single(c => c.Name == "Food").Id;
        parentsFood.ShouldNotBe(homeFood);

        (await person.SendAsync(In(home.Id, HttpMethod.Post, "/api/v1/expenses",
            new { description = "multi-home-rent", amount = 100m, date = "2026-10-05", categoryId = homeFood }), Ct))
            .StatusCode.ShouldBe(HttpStatusCode.Created);
        (await person.SendAsync(In(parents.Id, HttpMethod.Post, "/api/v1/expenses",
            new { description = "multi-parents-medicine", amount = 50m, date = "2026-10-05", categoryId = parentsFood }), Ct))
            .StatusCode.ShouldBe(HttpStatusCode.Created);

        var inHome = await SendAsync<ExpensePage>(person, In(home.Id, HttpMethod.Get, "/api/v1/expenses?search=multi-"));
        var inParents = await SendAsync<ExpensePage>(person, In(parents.Id, HttpMethod.Get, "/api/v1/expenses?search=multi-"));

        inHome.Items.Select(e => e.Description).ShouldBe(["multi-home-rent"]);
        inParents.Items.Select(e => e.Description).ShouldBe(["multi-parents-medicine"]);

        // A category from the other family can't be used.
        (await person.SendAsync(In(home.Id, HttpMethod.Post, "/api/v1/expenses",
            new { description = "wrong", amount = 1m, date = "2026-10-05", categoryId = parentsFood }), Ct))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Without_a_header_the_earliest_joined_family_is_used()
    {
        using var person = await RegisterAndSignInAsync(NewEmail("default"));
        var first = await CreateFamilyAsync(person, "First");
        await CreateFamilyAsync(person, "Second");

        var active = await person.GetFromJsonAsync<Family>("/api/v1/families/mine", Json, Ct);

        active!.Id.ShouldBe(first.Id);
    }

    [Fact]
    public async Task A_header_naming_someone_elses_family_never_widens_access()
    {
        using var person = await RegisterAndSignInAsync(NewEmail("probe"));
        var mine = await CreateFamilyAsync(person, "Mine");
        using var ownerA = await factory.CreateClientForAsync(ApiFactory.FamilyAOwnerEmail);
        var familyA = await ownerA.GetFromJsonAsync<Family>("/api/v1/families/mine", Json, Ct);

        var active = await SendAsync<Family>(person, In(familyA!.Id, HttpMethod.Get, "/api/v1/families/mine"));

        active.Id.ShouldBe(mine.Id); // ignored: the person's own family is used instead
        var expenses = await SendAsync<ExpensePage>(person, In(familyA.Id, HttpMethod.Get, "/api/v1/expenses"));
        expenses.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task A_person_is_a_member_or_viewer_in_only_one_family_but_can_still_own_others()
    {
        using var person = await RegisterAndSignInAsync(NewEmail("member"));
        using var ownerA = await factory.CreateClientForAsync(ApiFactory.FamilyAOwnerEmail);
        var familyA = (await ownerA.GetFromJsonAsync<Family>("/api/v1/families/mine", Json, Ct))!;
        using var ownerB = await factory.CreateClientForAsync(ApiFactory.FamilyBOwnerEmail, ApiFactory.SuperAdminPassword);
        var familyB = (await ownerB.GetFromJsonAsync<Family>("/api/v1/families/mine", Json, Ct))!;

        // Joins family A as a Member.
        var inviteA = await InviteAsync(ownerA, familyA.Id, "member");
        (await person.PostAsJsonAsync("/api/v1/families/invites/join", new { inviteA.Token }, Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);

        // Can't also be a Viewer in family B, and can't join A twice.
        var inviteB = await InviteAsync(ownerB, familyB.Id, "viewer");
        (await person.PostAsJsonAsync("/api/v1/families/invites/join", new { inviteB.Token }, Ct)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var inviteA2 = await InviteAsync(ownerA, familyA.Id, "member");
        (await person.PostAsJsonAsync("/api/v1/families/invites/join", new { inviteA2.Token }, Ct)).StatusCode.ShouldBe(HttpStatusCode.Conflict);

        // But they can own as many families as they like.
        await CreateFamilyAsync(person, "My own family");
        await CreateFamilyAsync(person, "My second family");
        var mine = await person.GetFromJsonAsync<List<FamilySummary>>("/api/v1/families", Json, Ct);
        mine!.Count(f => f.Role == "member").ShouldBe(1);
        mine.Count(f => f.Role == "owner").ShouldBe(2);

        // Joining family B as an Owner is fine: only Member and Viewer are exclusive.
        var inviteBOwner = await InviteAsync(ownerB, familyB.Id, "owner");
        (await person.PostAsJsonAsync("/api/v1/families/invites/join", new { inviteBOwner.Token }, Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task The_last_owner_rule_applies_to_each_family_separately()
    {
        using var person = await RegisterAndSignInAsync(NewEmail("lastowner"));
        var first = await CreateFamilyAsync(person, "First");
        var second = await CreateFamilyAsync(person, "Second");

        (await person.SendAsync(In(first.Id, HttpMethod.Post, "/api/v1/families/leave"), Ct)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await person.SendAsync(In(second.Id, HttpMethod.Post, "/api/v1/families/leave"), Ct)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Listing_families_needs_sign_in_and_registering_does_not()
    {
        using var anonymous = factory.CreateAnonymousClient();

        (await anonymous.GetAsync("/api/v1/families", Ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await anonymous.PostAsJsonAsync("/api/v1/auth/register",
            new { email = NewEmail("anon"), displayName = "Anon", password = ApiFactory.DemoPassword }, Ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ExpenseTracker.Api.IntegrationTests;

/// <summary>Authentication, sessions, API keys and RBAC, end to end through HTTP.</summary>
public sealed class AuthTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly CancellationToken Ct = CancellationToken.None;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private sealed record CurrentUser(Guid Id, string Email, string DisplayName, List<string> Roles, List<string> Permissions);
    private sealed record AuthResponse(string AccessToken, string TokenType, int ExpiresIn, CurrentUser User);
    private sealed record Problem(int Status, string? Title, string? Detail);
    private sealed record UserDetail(Guid Id, string Email, List<string> Roles, bool IsActive, List<string> EffectivePermissions);
    private sealed record UserSummary(Guid Id, string Email, List<string> Roles);
    private sealed record UserPage(List<UserSummary> Items, int TotalCount);
    private sealed record Role(string Name, List<string> Permissions, bool CanEdit);
    private sealed record ApiKey(Guid Id, string Prefix, List<string> Scopes);
    private sealed record CreatedApiKey(ApiKey ApiKey, string Key);

    private static object NewExpense => new { description = "auth-test", amount = 10m, date = "2026-10-05", categoryId = 1 };

    private async Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password) =>
        await client.PostAsJsonAsync("/api/auth/login", new { email, password }, Ct);

    private async Task<UserDetail> CreateUserAsync(string role, string? password = null)
    {
        using var superAdmin = await factory.CreateClientForAsync(ApiFactory.SuperAdminEmail, ApiFactory.SuperAdminPassword);
        var response = await superAdmin.PostAsJsonAsync("/api/admin/users", new
        {
            email = $"{Guid.NewGuid():N}@tests.local",
            displayName = $"Test {role}",
            password = password ?? ApiFactory.DemoPassword,
            roles = new[] { role },
        }, Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<UserDetail>(Json, Ct))!;
    }

    private static string RefreshCookieFrom(HttpResponseMessage response) =>
        response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("et_refresh=", StringComparison.Ordinal)).Split(';')[0];

    private static HttpRequestMessage RefreshRequest(string cookie)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", cookie);
        return request;
    }

    // ---- Authentication ----

    [Fact]
    public async Task Api_is_secure_by_default()
    {
        using var anonymous = factory.CreateAnonymousClient();

        (await anonymous.GetAsync("/api/expenses", Ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await anonymous.GetAsync("/api/categories/lookup", Ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await anonymous.GetAsync("/api/auth/me", Ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await anonymous.GetAsync("/openapi/v1.json", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Failed_sign_in_does_not_reveal_whether_the_account_exists()
    {
        using var anonymous = factory.CreateAnonymousClient();

        var wrongPassword = await LoginAsync(anonymous, ApiFactory.ReaderEmail, "not-the-password");
        var unknownUser = await LoginAsync(anonymous, "nobody@tests.local", "not-the-password");

        wrongPassword.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        unknownUser.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await wrongPassword.Content.ReadFromJsonAsync<Problem>(Json, Ct))!.Detail
            .ShouldBe((await unknownUser.Content.ReadFromJsonAsync<Problem>(Json, Ct))!.Detail);
    }

    [Fact]
    public async Task Sign_in_returns_a_bearer_token_and_a_hardened_refresh_cookie()
    {
        using var anonymous = factory.CreateAnonymousClient(handleCookies: false);

        var response = await LoginAsync(anonymous, ApiFactory.ReaderEmail, ApiFactory.DemoPassword);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = (await response.Content.ReadFromJsonAsync<AuthResponse>(Json, Ct))!;
        body.TokenType.ShouldBe("Bearer");
        body.ExpiresIn.ShouldBeInRange(800, 900);
        body.User.Roles.ShouldBe(["Reader"]);

        var cookie = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("et_refresh=", StringComparison.Ordinal)).ToLowerInvariant();
        cookie.ShouldContain("httponly");
        cookie.ShouldContain("secure");
        cookie.ShouldContain("samesite=strict");
        cookie.ShouldContain("path=/api/auth");
    }

    [Fact]
    public async Task Me_describes_the_caller_and_their_effective_permissions()
    {
        using var reader = await factory.CreateClientForAsync(ApiFactory.ReaderEmail);

        var me = await reader.GetFromJsonAsync<CurrentUser>("/api/auth/me", Json, Ct);

        me!.Email.ShouldBe(ApiFactory.ReaderEmail);
        me.Roles.ShouldBe(["Reader"]);
        me.Permissions.ShouldContain("expense.view");
        me.Permissions.ShouldNotContain("expense.add");
    }

    [Fact]
    public async Task Refresh_rotates_the_token_and_replaying_an_old_one_ends_the_session()
    {
        using var client = factory.CreateAnonymousClient(handleCookies: false);
        var first = RefreshCookieFrom(await LoginAsync(client, ApiFactory.ReaderEmail, ApiFactory.DemoPassword));

        var refreshed = await client.SendAsync(RefreshRequest(first), Ct);
        refreshed.StatusCode.ShouldBe(HttpStatusCode.OK);
        var second = RefreshCookieFrom(refreshed);
        second.ShouldNotBe(first);

        // The first token was already rotated: replaying it is treated as theft.
        (await client.SendAsync(RefreshRequest(first), Ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        // ...which also revokes the legitimate successor.
        (await client.SendAsync(RefreshRequest(second), Ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_revokes_the_session()
    {
        using var client = factory.CreateAnonymousClient(handleCookies: false);
        var cookie = RefreshCookieFrom(await LoginAsync(client, ApiFactory.ReaderEmail, ApiFactory.DemoPassword));

        var logout = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        logout.Headers.Add("Cookie", cookie);
        (await client.SendAsync(logout, Ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await client.SendAsync(RefreshRequest(cookie), Ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Accounts_lock_after_five_failed_sign_ins()
    {
        var user = await CreateUserAsync("Reader");
        using var anonymous = factory.CreateAnonymousClient();

        for (var i = 0; i < 5; i++) await LoginAsync(anonymous, user.Email, "wrong-password");
        var correct = await LoginAsync(anonymous, user.Email, ApiFactory.DemoPassword);

        correct.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await correct.Content.ReadFromJsonAsync<Problem>(Json, Ct))!.Title.ShouldBe("Account locked");
    }

    [Fact]
    public async Task Disabling_a_user_revokes_their_access_immediately()
    {
        var user = await CreateUserAsync("User");
        using var client = factory.CreateAnonymousClient();
        var token = (await (await LoginAsync(client, user.Email, ApiFactory.DemoPassword)).Content.ReadFromJsonAsync<AuthResponse>(Json, Ct))!.AccessToken;
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        (await client.GetAsync("/api/expenses", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);

        using var superAdmin = await factory.CreateClientForAsync(ApiFactory.SuperAdminEmail, ApiFactory.SuperAdminPassword);
        (await superAdmin.PutAsJsonAsync($"/api/admin/users/{user.Id}", new { email = user.Email, displayName = "Disabled", isActive = false }, Ct))
            .StatusCode.ShouldBe(HttpStatusCode.OK);

        // Same, still unexpired access token.
        (await client.GetAsync("/api/expenses", Ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await LoginAsync(factory.CreateAnonymousClient(), user.Email, ApiFactory.DemoPassword)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ---- Authorization ----

    [Fact]
    public async Task Readers_can_read_but_not_write()
    {
        using var reader = await factory.CreateClientForAsync(ApiFactory.ReaderEmail);

        (await reader.GetAsync("/api/expenses", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await reader.GetAsync("/api/budgets", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await reader.PostAsJsonAsync("/api/expenses", NewExpense, Ct)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await reader.DeleteAsync("/api/categories/1", Ct)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Administration_requires_Admin_or_above()
    {
        using var user = await factory.CreateClientForAsync(ApiFactory.UserEmail);
        using var admin = await factory.CreateClientForAsync(ApiFactory.AdminEmail);

        (await user.GetAsync("/api/admin/users", Ct)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await user.GetAsync("/api/admin/roles", Ct)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await admin.GetAsync("/api/admin/users", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task A_direct_permission_claim_takes_effect_on_the_next_request()
    {
        var created = await CreateUserAsync("Reader");
        using var client = factory.CreateAnonymousClient();
        var token = (await (await LoginAsync(client, created.Email, ApiFactory.DemoPassword)).Content.ReadFromJsonAsync<AuthResponse>(Json, Ct))!.AccessToken;
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        (await client.PostAsJsonAsync("/api/expenses", NewExpense, Ct)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        using var admin = await factory.CreateClientForAsync(ApiFactory.AdminEmail);
        var granted = await admin.PostAsJsonAsync($"/api/admin/users/{created.Id}/claims", new { type = "permission", value = "expense.add" }, Ct);
        granted.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await granted.Content.ReadFromJsonAsync<UserDetail>(Json, Ct))!.EffectivePermissions.ShouldContain("expense.add");

        // Same token: permissions are resolved per request, not baked into the token.
        (await client.PostAsJsonAsync("/api/expenses", NewExpense, Ct)).StatusCode.ShouldBe(HttpStatusCode.Created);

        (await admin.DeleteAsync($"/api/admin/users/{created.Id}/claims?type=permission&value=expense.add", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.PostAsJsonAsync("/api/expenses", NewExpense, Ct)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admins_cannot_escalate()
    {
        using var admin = await factory.CreateClientForAsync(ApiFactory.AdminEmail);
        var users = await admin.GetFromJsonAsync<UserPage>("/api/admin/users?pageSize=100", Json, Ct);
        var superAdmin = users!.Items.Single(u => u.Email == ApiFactory.SuperAdminEmail);
        var self = users.Items.Single(u => u.Email == ApiFactory.AdminEmail);
        var reader = await CreateUserAsync("Reader");

        // No creating or promoting anyone to their own rank or above.
        (await admin.PostAsJsonAsync("/api/admin/users", new { email = "new-admin@tests.local", displayName = "X", password = ApiFactory.DemoPassword, roles = new[] { "Admin" } }, Ct))
            .StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await admin.PutAsJsonAsync($"/api/admin/users/{reader.Id}/roles", new { roles = new[] { "SuperAdmin" } }, Ct))
            .StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        // No touching users at or above their rank, or themselves.
        (await admin.PostAsync($"/api/admin/users/{superAdmin.Id}/revoke-sessions", null, Ct)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await admin.PutAsJsonAsync($"/api/admin/users/{self.Id}/roles", new { roles = new[] { "SuperAdmin" } }, Ct))
            .StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        // No widening their own role.
        (await admin.PutAsJsonAsync("/api/admin/roles/Admin/permissions", new { permissions = new[] { "expense.view" } }, Ct))
            .StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        // But managing a lower-ranked user works.
        (await admin.PutAsJsonAsync($"/api/admin/users/{reader.Id}/roles", new { roles = new[] { "User" } }, Ct))
            .StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Roles_list_shows_which_roles_the_caller_may_edit()
    {
        using var admin = await factory.CreateClientForAsync(ApiFactory.AdminEmail);

        var roles = (await admin.GetFromJsonAsync<List<Role>>("/api/admin/roles", Json, Ct))!;

        roles.Select(r => r.Name).ShouldBe(["SuperAdmin", "Admin", "User", "Reader"]);
        roles.Where(r => r.CanEdit).Select(r => r.Name).ShouldBe(["User", "Reader"]);
        roles.Single(r => r.Name == "Reader").Permissions.ShouldNotContain("expense.add");
    }

    // ---- API keys ----

    [Fact]
    public async Task Api_keys_authenticate_within_their_scopes_until_revoked()
    {
        using var user = await factory.CreateClientForAsync(ApiFactory.UserEmail);
        var created = await user.PostAsJsonAsync("/api/auth/api-keys", new { name = "read-only integration", scopes = new[] { "expense.view" }, expiresInDays = 30 }, Ct);
        created.StatusCode.ShouldBe(HttpStatusCode.Created);
        var key = (await created.Content.ReadFromJsonAsync<CreatedApiKey>(Json, Ct))!;
        key.Key.ShouldStartWith(key.ApiKey.Prefix);

        using var integration = factory.CreateAnonymousClient();
        integration.DefaultRequestHeaders.Add("x-api-key", key.Key);

        (await integration.GetAsync("/api/expenses", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await integration.PostAsJsonAsync("/api/expenses", NewExpense, Ct)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await integration.GetFromJsonAsync<CurrentUser>("/api/auth/me", Json, Ct))!.Permissions.ShouldBe(["expense.view"]);
        // Keys can't mint more keys.
        (await integration.GetAsync("/api/auth/api-keys", Ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        (await user.DeleteAsync($"/api/auth/api-keys/{key.ApiKey.Id}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await integration.GetAsync("/api/expenses", Ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Api_keys_cannot_exceed_their_owner()
    {
        using var reader = await factory.CreateClientForAsync(ApiFactory.ReaderEmail);

        var response = await reader.PostAsJsonAsync("/api/auth/api-keys", new { name = "sneaky", scopes = new[] { "expense.delete" } }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Unknown_api_keys_are_rejected()
    {
        using var client = factory.CreateAnonymousClient();
        client.DefaultRequestHeaders.Add("x-api-key", "etk_not_a_real_key");

        (await client.GetAsync("/api/expenses", Ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}

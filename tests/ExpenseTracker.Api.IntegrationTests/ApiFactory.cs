using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseTracker.Api.IntegrationTests;

/// <summary>
/// Hosts the real API (all layers) in memory against its own SQLite database file in the temp folder, which is
/// created by the app's migrations on startup and deleted when the factory is disposed. Never touches the development database.
/// Seeds a SuperAdmin plus one demo user per lower role, all signing in over HTTPS like a real client.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string SuperAdminEmail = "superadmin@tests.local";
    public const string SuperAdminPassword = "SuperAdmin123";
    public const string AdminEmail = "admin@tests.local";
    public const string UserEmail = "user@tests.local";
    public const string ReaderEmail = "reader@tests.local";
    public const string DemoPassword = "Password123";

    /// <summary>
    /// Family A: user@ is the Owner, admin@ a Member and reader@ a Viewer. Family B: superadmin@ is the Owner.
    /// Created through the API by <see cref="EnsureFamiliesAsync"/>.
    /// </summary>
    public const string FamilyAOwnerEmail = UserEmail;
    public const string FamilyBOwnerEmail = SuperAdminEmail;

    private readonly SemaphoreSlim _familySetup = new(1, 1);
    private bool _familiesReady;

    /// <summary>The "Food" category of family A / family B (each family gets its own copy of the defaults).</summary>
    public Guid FamilyAFoodCategoryId { get; private set; }
    public Guid FamilyBFoodCategoryId { get; private set; }

    private readonly string _connectionString =
        $"Data Source={Path.Combine(Path.GetTempPath(), $"expense-tracker-tests-{Guid.NewGuid():N}.db")}";

    private readonly ConcurrentDictionary<string, string> _accessTokens = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", _connectionString);
        builder.UseSetting("Features:MigrateDatabaseOnStartup", "true");
        builder.UseSetting("Features:ApiDocs", "true");
        builder.UseSetting("Authentication:Jwt:SigningKey", "integration-tests-signing-key-0123456789abcdef");
        builder.UseSetting("Identity:Seed:AdminEmail", SuperAdminEmail);
        builder.UseSetting("Identity:Seed:AdminPassword", SuperAdminPassword);
        builder.UseSetting("Identity:Seed:DemoUsers", "true");
        builder.UseSetting("Identity:Seed:DemoPassword", DemoPassword);
    }

    /// <summary>A client with no credentials. HTTPS, so the Secure refresh cookie round-trips.</summary>
    public HttpClient CreateAnonymousClient(bool handleCookies = true) =>
        CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), HandleCookies = handleCookies });

    /// <summary>A client that sends a bearer token for the given account (tokens are cached per account).</summary>
    public async Task<HttpClient> CreateClientForAsync(string email, string password = DemoPassword)
    {
        var client = CreateAnonymousClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetAccessTokenAsync(email, password));
        return client;
    }

    public async Task<string> GetAccessTokenAsync(string email, string password = DemoPassword)
    {
        if (_accessTokens.TryGetValue(email, out var cached)) return cached;

        using var client = CreateAnonymousClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        var token = (await response.Content.ReadFromJsonAsync<TokenResponse>())!.AccessToken;
        return _accessTokens[email] = token;
    }

    private sealed record TokenResponse(string AccessToken);
    private sealed record InviteResponse(Guid Id, string Token);
    private sealed record CategoryLookup(Guid Id, string Name);

    /// <summary>Creates the two test families once (idempotent, safe to call from every test class).</summary>
    public async Task EnsureFamiliesAsync()
    {
        await _familySetup.WaitAsync();
        try
        {
            if (_familiesReady) return;

            using var ownerA = await CreateClientForAsync(FamilyAOwnerEmail);
            (await ownerA.PostAsJsonAsync("/api/v1/families", new { name = "Family A" })).EnsureSuccessStatusCode();
            using var admin = await CreateClientForAsync(AdminEmail);
            await JoinFamilyAAsync(admin, "member");
            using var reader = await CreateClientForAsync(ReaderEmail);
            await JoinFamilyAAsync(reader, "viewer");

            using var ownerB = await CreateClientForAsync(FamilyBOwnerEmail, SuperAdminPassword);
            (await ownerB.PostAsJsonAsync("/api/v1/families", new { name = "Family B" })).EnsureSuccessStatusCode();

            FamilyAFoodCategoryId = await FoodCategoryIdAsync(ownerA);
            FamilyBFoodCategoryId = await FoodCategoryIdAsync(ownerB);
            _familiesReady = true;
        }
        finally
        {
            _familySetup.Release();
        }
    }

    /// <summary>Family A's owner invites the signed-in client, who joins with the invite token.</summary>
    public async Task JoinFamilyAAsync(HttpClient client, string role)
    {
        using var ownerA = await CreateClientForAsync(FamilyAOwnerEmail);
        var invite = await ownerA.PostAsJsonAsync("/api/v1/families/invites", new { role });
        invite.EnsureSuccessStatusCode();
        var token = (await invite.Content.ReadFromJsonAsync<InviteResponse>())!.Token;
        (await client.PostAsJsonAsync("/api/v1/families/invites/join", new { token })).EnsureSuccessStatusCode();
    }

    private static async Task<Guid> FoodCategoryIdAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<List<CategoryLookup>>("/api/v1/categories/lookup"))!.Single(c => c.Name == "Food").Id;

    public override async ValueTask DisposeAsync()
    {
        await using (var scope = Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().DeleteAsync();
        }
        await base.DisposeAsync();
    }
}

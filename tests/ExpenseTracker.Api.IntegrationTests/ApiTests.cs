using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExpenseTracker.Api.IntegrationTests;

/// <summary>End-to-end through HTTP: routing, validation, use cases, EF Core/SQLite and problem details.</summary>
public sealed class ApiTests(ApiFactory factory) : IClassFixture<ApiFactory>, IAsyncLifetime
{
    // Signed in as a regular User: holds every expense/category permission, and nothing administrative.
    private HttpClient _client = null!;
    private static readonly CancellationToken Ct = CancellationToken.None;

    public async ValueTask InitializeAsync() => _client = await factory.CreateClientForAsync(ApiFactory.UserEmail);

    public ValueTask DisposeAsync()
    {
        _client.Dispose();
        return ValueTask.CompletedTask;
    }
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private sealed record Lookup(Guid Id, string Name, string Color);
    private sealed record ExpenseDto(Guid Id, string Description, decimal Amount, DateOnly Date, Guid CategoryId, string CategoryName);
    private sealed record ExpensePage(List<ExpenseDto> Items, int Page, int PageSize, int TotalCount, int TotalPages, decimal TotalAmount);
    private sealed record Problem(int Status, string? Title, string? Detail, Dictionary<string, string[]>? Errors);
    private sealed record CategoryDto(Guid Id, string Name, decimal? MonthlyBudget, int ExpenseCount);
    private sealed record BudgetOverview(List<BudgetStatus> Categories);
    private sealed record BudgetStatus(Guid CategoryId, decimal Budget, decimal Spent, [property: JsonPropertyName("state")] string State);

    private async Task<Guid> CategoryId(string name) =>
        (await _client.GetFromJsonAsync<List<Lookup>>("/api/v1/categories/lookup", Ct))!.Single(c => c.Name == name).Id;

    private async Task<ExpenseDto> CreateExpense(string description, decimal amount, Guid categoryId, string date = "2026-10-05")
    {
        var response = await _client.PostAsJsonAsync("/api/v1/expenses", new { description, amount, date, categoryId }, Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<ExpenseDto>(Json, Ct))!;
    }

    [Fact]
    public async Task Seeded_categories_are_available()
    {
        var categories = await _client.GetFromJsonAsync<List<Lookup>>("/api/v1/categories/lookup", Ct);
        categories.ShouldNotBeNull();
        categories.Select(c => c.Name).ShouldContain("Food");
        categories.Count.ShouldBeGreaterThanOrEqualTo(8);
    }

    [Fact]
    public async Task Expenses_are_paged_sorted_and_totalled_on_the_server()
    {
        var categoryId = await CategoryId("Transport");
        for (var i = 1; i <= 5; i++) await CreateExpense($"paging-test {i}", i * 10m, categoryId);

        var page = await _client.GetFromJsonAsync<ExpensePage>(
            "/api/v1/expenses?search=paging-test&pageSize=2&page=2&sortBy=amount&sortDir=desc", Json, Ct);

        page!.TotalCount.ShouldBe(5);
        page.TotalPages.ShouldBe(3);
        page.TotalAmount.ShouldBe(150m);
        page.Items.Select(e => e.Amount).ShouldBe([30m, 20m]);
    }

    [Fact]
    public async Task Pages_past_the_end_are_clamped()
    {
        var categoryId = await CategoryId("Transport");
        await CreateExpense("clamp-test", 5m, categoryId);

        var page = await _client.GetFromJsonAsync<ExpensePage>("/api/v1/expenses?search=clamp-test&page=99", Json, Ct);

        page!.Page.ShouldBe(1);
        page.Items.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Unknown_sort_fields_are_rejected_with_a_validation_problem()
    {
        var response = await _client.GetAsync("/api/v1/expenses?sortBy=password", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<Problem>(Json, Ct);
        problem!.Errors!.ShouldContainKey("sortBy");
    }

    [Fact]
    public async Task Request_validation_reports_each_invalid_field()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/expenses", new { description = "", amount = 0, date = "2026-10-05", categoryId = ApiFactory.SeededFoodCategoryId }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<Problem>(Json, Ct);
        problem!.Errors!.Keys.ShouldBe(["Description", "Amount"], ignoreOrder: true);
    }

    [Fact]
    public async Task Domain_rules_are_enforced_through_the_api()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/expenses",
            new { description = "too precise", amount = 10.005m, date = "2026-10-05", categoryId = await CategoryId("Food") }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<Problem>(Json, Ct))!.Errors!.ShouldContainKey("amount");
    }

    [Fact]
    public async Task ListExpenses_SearchInDifferentCase_FindsTheExpense()
    {
        var expense = await CreateExpense("Weekly SHOP 50%_off", 20m, await CategoryId("Food"));

        var page = await _client.GetFromJsonAsync<ExpensePage>("/api/v1/expenses?search=shop%2050%25_OFF", Json, Ct);

        page!.Items.Select(e => e.Id).ShouldBe([expense.Id]);
    }

    [Fact]
    public async Task Unknown_ids_return_not_found_problems()
    {
        var response = await _client.GetAsync($"/api/v1/expenses/{Guid.NewGuid()}", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.Content.ReadFromJsonAsync<Problem>(Json, Ct))!.Title.ShouldBe("Expense not found");
    }

    [Fact]
    public async Task A_category_in_use_cannot_be_deleted()
    {
        var categoryId = await CategoryId("Health");
        await CreateExpense("conflict-test", 12m, categoryId);

        var response = await _client.DeleteAsync($"/api/v1/categories/{categoryId}", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Budgets_report_spending_against_the_limit()
    {
        var created = await _client.PostAsJsonAsync("/api/v1/categories", new { name = "Budget test", color = "#123456", monthlyBudget = 100m }, Ct);
        var category = (await created.Content.ReadFromJsonAsync<CategoryDto>(Json, Ct))!;
        await CreateExpense("budget-test", 150m, category.Id, "2026-10-10");

        var overview = await _client.GetFromJsonAsync<BudgetOverview>("/api/v1/budgets?year=2026&month=10", Json, Ct);

        var status = overview!.Categories.Single(c => c.CategoryId == category.Id);
        status.Spent.ShouldBe(150m);
        status.State.ShouldBe("exceeded");
    }

    [Fact]
    public async Task Export_returns_a_csv_file()
    {
        await CreateExpense("export-test", 42m, await CategoryId("Other"));

        var response = await _client.GetAsync("/api/v1/expenses/export?search=export-test", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("text/csv");
        var csv = await response.Content.ReadAsStringAsync(Ct);
        csv.ShouldContain("Date,Description,Category,Amount");
        csv.ShouldContain("export-test,Other,42.00");
    }

    [Fact]
    public async Task Bulk_delete_removes_existing_ids_and_ignores_missing_ones()
    {
        var categoryId = await CategoryId("Shopping");
        var a = await CreateExpense("bulk-test a", 1m, categoryId);
        var b = await CreateExpense("bulk-test b", 2m, categoryId);

        var response = await _client.PostAsJsonAsync("/api/v1/expenses/bulk-delete", new { ids = new[] { a.Id, b.Id, Guid.NewGuid() } }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var remaining = await _client.GetFromJsonAsync<ExpensePage>("/api/v1/expenses?search=bulk-test", Json, Ct);
        remaining!.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task OpenApi_document_describes_the_api()
    {
        var response = await _client.GetAsync("/openapi/v1.json", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync(Ct)).ShouldContain("ListExpenses");
    }

    [Fact]
    public async Task ApiResponses_Always_CarrySecurityHeaders()
    {
        var response = await _client.GetAsync("/api/v1/categories/lookup", Ct);

        response.Headers.GetValues("X-Content-Type-Options").ShouldBe(["nosniff"]);
        response.Headers.GetValues("X-Frame-Options").ShouldBe(["DENY"]);
        response.Headers.GetValues("Referrer-Policy").ShouldBe(["no-referrer"]);
        response.Headers.GetValues("Content-Security-Policy").Single().ShouldContain("default-src 'none'");
    }
}

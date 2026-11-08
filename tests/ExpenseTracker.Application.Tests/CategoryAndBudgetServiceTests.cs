using ExpenseTracker.Application.Abstractions.Persistence;
using ExpenseTracker.Application.Abstractions.Queries;
using ExpenseTracker.Application.Abstractions.Realtime;
using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Features.Budgets;
using ExpenseTracker.Application.Features.Categories;
using ExpenseTracker.Domain.Budgets;
using ExpenseTracker.Domain.Categories;
using ExpenseTracker.Domain.Common;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ExpenseTracker.Application.Tests;

public class CategoryServiceTests
{
    private readonly ICategoryRepository _categories = Substitute.For<ICategoryRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IChangeNotifier _notifier = Substitute.For<IChangeNotifier>();
    private readonly CategoryService _service;
    private static readonly CancellationToken Ct = CancellationToken.None;

    public CategoryServiceTests() =>
        _service = new CategoryService(_categories, _unitOfWork, Substitute.For<ICategoryQueries>(), _notifier,
            NullLogger<CategoryService>.Instance);

    [Fact]
    public async Task Create_rejects_a_duplicate_name()
    {
        _categories.NameExistsAsync("Food", null, Ct).Returns(true);

        var result = await _service.CreateAsync(new CategoryCommand("  Food ", "#f97316", null), Ct);

        result.Error.ShouldBe(CategoryErrors.DuplicateName);
        _categories.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [Fact]
    public async Task Delete_is_a_conflict_while_expenses_use_the_category()
    {
        var food = Category.Create("Food", "#f97316", null).Value!;
        _categories.GetByIdAsync(TestIds.Of(1), Ct).Returns(food);
        _categories.CountExpensesAsync(TestIds.Of(1), Ct).Returns(3);

        var result = await _service.DeleteAsync(TestIds.Of(1), Ct);

        result.Error!.Kind.ShouldBe(ErrorKind.Conflict);
        _categories.DidNotReceiveWithAnyArgs().Remove(default!);
    }

    [Fact]
    public async Task Delete_removes_an_unused_category_and_notifies()
    {
        var food = Category.Create("Food", "#f97316", null).Value!;
        _categories.GetByIdAsync(TestIds.Of(1), Ct).Returns(food);

        var result = await _service.DeleteAsync(TestIds.Of(1), Ct);

        result.IsSuccess.ShouldBeTrue();
        _categories.Received(1).Remove(food);
        await _notifier.Received(1).CategoriesChangedAsync(ChangeAction.Deleted, Arg.Any<IReadOnlyCollection<Guid>>(), Ct);
    }
}

public class BudgetServiceTests
{
    private readonly IBudgetQueries _queries = Substitute.For<IBudgetQueries>();
    private readonly BudgetService _service;
    private static readonly CancellationToken Ct = CancellationToken.None;
    private static readonly DateOnly October5 = new(2026, 10, 5);

    public BudgetServiceTests() =>
        _service = new BudgetService(_queries, new FixedTimeProvider(), NullLogger<BudgetService>.Instance);

    private void Spent(decimal? budget, decimal spent) =>
        _queries.GetCategorySpendingAsync(TestIds.Of(1), new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31), Ct)
            .Returns(new CategorySpending(TestIds.Of(1), "Food", "#f97316", budget, spent));

    [Fact]
    public async Task Alerts_when_an_addition_crosses_the_budget()
    {
        Spent(budget: 100m, spent: 130m);

        var alert = await _service.DetectOverrunAsync(TestIds.Of(1), October5, addedAmount: 50m, Ct);

        alert.ShouldNotBeNull();
        alert.Spent.ShouldBe(130m);
    }

    [Fact]
    public async Task Does_not_alert_again_when_already_over_budget()
    {
        Spent(budget: 100m, spent: 180m);

        (await _service.DetectOverrunAsync(TestIds.Of(1), October5, addedAmount: 50m, Ct)).ShouldBeNull();
    }

    [Fact]
    public async Task Does_not_alert_for_categories_without_a_budget()
    {
        Spent(budget: null, spent: 1_000m);

        (await _service.DetectOverrunAsync(TestIds.Of(1), October5, addedAmount: 1_000m, Ct)).ShouldBeNull();
    }

    [Fact]
    public async Task Overview_maps_spending_to_states()
    {
        _queries.GetBudgetedSpendingAsync(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31), Ct).Returns(
        [
            new CategorySpending(TestIds.Of(1), "Food", "#f97316", 100m, 50m),
            new CategorySpending(TestIds.Of(2), "Rent", "#8b5cf6", 100m, 90m),
            new CategorySpending(TestIds.Of(3), "Fun", "#ec4899", 100m, 150m),
        ]);

        var overview = await _service.GetOverviewAsync(null, null, Ct);

        overview.Categories.Select(c => c.State).ShouldBe([BudgetState.Ok, BudgetState.Warning, BudgetState.Exceeded]);
        overview.TotalBudget.ShouldBe(300m);
        overview.TotalSpent.ShouldBe(290m);
    }
}

public class PagingTests
{
    [Theory]
    [InlineData(null, null, 0, 1, Paging.DefaultPageSize)]
    [InlineData(5, 10, 23, 3, 10)]      // clamped to the last page
    [InlineData(0, 1000, 50, 1, 100)]   // page and size clamped
    public void Resolve_normalises_and_clamps(int? page, int? size, int total, int expectedPage, int expectedSize)
    {
        Paging.Resolve(new PageRequest(page, size), total).ShouldBe((expectedPage, expectedSize));
    }

    [Fact]
    public void ResolveSort_defaults_direction_per_field()
    {
        string[] allowed = ["date", "amount"];
        Paging.ResolveSort(null, null, "date", true, allowed).Value.ShouldBe(new SortSpec("date", true));
        Paging.ResolveSort("AMOUNT", null, "date", true, allowed).Value.ShouldBe(new SortSpec("amount", false));
    }
}

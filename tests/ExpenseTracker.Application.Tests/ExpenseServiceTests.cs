using ExpenseTracker.Application.Abstractions.Persistence;
using ExpenseTracker.Application.Abstractions.Queries;
using ExpenseTracker.Application.Abstractions.Realtime;
using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Features.Budgets;
using ExpenseTracker.Application.Features.Expenses;
using ExpenseTracker.Domain.Categories;
using ExpenseTracker.Domain.Common;
using ExpenseTracker.Domain.Expenses;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ExpenseTracker.Application.Tests;

public class ExpenseServiceTests
{
    private readonly IExpenseRepository _expenses = Substitute.For<IExpenseRepository>();
    private readonly ICategoryRepository _categories = Substitute.For<ICategoryRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IExpenseQueries _queries = Substitute.For<IExpenseQueries>();
    private readonly IBudgetMonitor _budgets = Substitute.For<IBudgetMonitor>();
    private readonly IChangeNotifier _notifier = Substitute.For<IChangeNotifier>();
    private readonly ExpenseService _service;

    private static readonly Category Food = Category.Create("Food", "#f97316", null).Value!;
    private static readonly CancellationToken Ct = CancellationToken.None;
    private static readonly Guid FoodId = TestIds.Of(1);

    public ExpenseServiceTests()
    {
        _categories.GetByIdAsync(FoodId, Arg.Any<CancellationToken>()).Returns(Food);
        _service = new ExpenseService(_expenses, _categories, _unitOfWork, _queries, _budgets, _notifier,
            new FixedTimeProvider(), NullLogger<ExpenseService>.Instance);
    }

    private static ExpenseCommand Command(decimal amount = 100m, Guid? categoryId = null) =>
        new("Groceries", amount, new DateOnly(2026, 10, 5), categoryId ?? FoodId, null);

    [Fact]
    public async Task Create_saves_the_expense_and_notifies_clients()
    {
        var result = await _service.CreateAsync(Command(), Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.CategoryName.ShouldBe("Food");
        _expenses.Received(1).Add(Arg.Is<Expense>(e => e.Amount == 100m));
        await _unitOfWork.Received(1).SaveChangesAsync(Ct);
        await _notifier.Received(1).ExpensesChangedAsync(ChangeAction.Created, Arg.Any<IReadOnlyCollection<Guid>>(), Ct);
    }

    [Fact]
    public async Task Create_with_unknown_category_is_a_validation_error_and_saves_nothing()
    {
        var result = await _service.CreateAsync(Command(categoryId: TestIds.Of(42)), Ct);

        result.Error.ShouldBe(ExpenseErrors.UnknownCategory);
        _expenses.DidNotReceiveWithAnyArgs().Add(default!);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task Create_enforces_domain_rules()
    {
        var result = await _service.CreateAsync(Command(amount: 0), Ct);

        result.Error!.Field.ShouldBe("amount");
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task Create_broadcasts_a_budget_alert_when_the_monitor_reports_an_overrun()
    {
        var alert = new BudgetAlert(FoodId, "Food", 50m, 100m, 2026, 10);
        _budgets.DetectOverrunAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), 100m, Ct).Returns(alert);

        await _service.CreateAsync(Command(), Ct);

        await _notifier.Received(1).BudgetExceededAsync(alert, Ct);
    }

    [Fact]
    public async Task Update_checks_the_budget_with_only_the_added_amount()
    {
        var existing = Expense.Create(new ExpenseDetails("Groceries", 100m, new DateOnly(2026, 10, 1), FoodId, null), DateTimeOffset.UtcNow).Value!;
        _expenses.GetByIdAsync(TestIds.Of(7), Ct).Returns(existing);

        await _service.UpdateAsync(TestIds.Of(7), Command(amount: 130m), Ct);

        await _budgets.Received(1).DetectOverrunAsync(FoodId, new DateOnly(2026, 10, 5), 30m, Ct);
    }

    [Fact]
    public async Task Delete_of_a_missing_expense_is_not_found_and_notifies_nobody()
    {
        _expenses.DeleteAsync(TestIds.Of(99), Ct).Returns(false);

        var result = await _service.DeleteAsync(TestIds.Of(99), Ct);

        result.Error!.Kind.ShouldBe(ErrorKind.NotFound);
        await _notifier.DidNotReceiveWithAnyArgs().ExpensesChangedAsync(default, default!, Ct);
    }

    [Fact]
    public async Task Bulk_delete_reports_what_was_actually_deleted()
    {
        _expenses.DeleteManyAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Ct).Returns([TestIds.Of(1), TestIds.Of(2)]);

        var result = await _service.BulkDeleteAsync([TestIds.Of(1), TestIds.Of(2), TestIds.Of(2), TestIds.Of(3)], Ct);

        result.Value!.Deleted.ShouldBe(2);
        await _expenses.Received(1).DeleteManyAsync(Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 3), Ct);
    }

    [Theory]
    [InlineData("password", null, "sortBy")]
    [InlineData("amount", "sideways", "sortDir")]
    public async Task List_rejects_unknown_sort_options(string sortBy, string? sortDir, string field)
    {
        var result = await _service.ListAsync(new ExpenseListRequest(ExpenseFilter.None, default, sortBy, sortDir), Ct);

        result.Error!.Field.ShouldBe(field);
        await _queries.DidNotReceiveWithAnyArgs().ListAsync(default!, default, default, Ct);
    }

    [Fact]
    public async Task List_rejects_an_inverted_date_range()
    {
        var filter = new ExpenseFilter(new DateOnly(2026, 10, 31), new DateOnly(2026, 10, 1), null, null);

        var result = await _service.ListAsync(new ExpenseListRequest(filter, default, null, null), Ct);

        result.Error.ShouldBe(ExpenseErrors.InvalidDateRange);
    }

    [Fact]
    public async Task Export_refuses_more_rows_than_the_limit()
    {
        _queries.ListAllAsync(Arg.Any<ExpenseFilter>(), Arg.Any<SortSpec>(), Arg.Any<int>(), Ct)
            .Returns(Enumerable.Repeat(SampleDto(), ExpenseExport.MaxRows + 1).ToList());

        var result = await _service.ExportAsync(ExpenseFilter.None, null, null, Ct);

        result.IsSuccess.ShouldBeFalse();
    }

    private static ExpenseDto SampleDto() =>
        new(TestIds.Of(1), "x", 1m, new DateOnly(2026, 1, 1), FoodId, "Food", "#f97316", null, DateTimeOffset.UtcNow);
}

internal sealed class FixedTimeProvider : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => new(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);
    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}

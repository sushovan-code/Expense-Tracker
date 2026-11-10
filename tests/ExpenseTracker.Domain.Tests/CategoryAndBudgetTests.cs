using ExpenseTracker.Domain.Budgets;
using ExpenseTracker.Domain.Categories;

namespace ExpenseTracker.Domain.Tests;

public class CategoryTests
{
    [Fact]
    public void Create_normalises_name_and_color()
    {
        var result = Category.Create("  Travel  ", "#4F46E5", 1500m, Guid.NewGuid());

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Travel");
        result.Value.Color.ShouldBe("#4f46e5");
        result.Value.MonthlyBudget.ShouldBe(1500m);
    }

    [Theory]
    [InlineData("", "#4f46e5", null, "name")]
    [InlineData("Travel", "blue", null, "color")]
    [InlineData("Travel", "#4f46e", null, "color")]
    [InlineData("Travel", "#4f46e5", -1, "monthlyBudget")]
    public void Create_rejects_invalid_input(string name, string color, int? budget, string field)
    {
        var result = Category.Create(name, color, budget, Guid.NewGuid());

        result.IsSuccess.ShouldBeFalse();
        result.Error.Field.ShouldBe(field);
    }

    [Fact]
    public void A_budget_of_zero_is_allowed_and_null_means_no_limit()
    {
        Category.Create("A", "#000000", 0, Guid.NewGuid()).IsSuccess.ShouldBeTrue();
        Category.Create("B", "#000000", null, Guid.NewGuid()).Value!.MonthlyBudget.ShouldBeNull();
    }
}

public class BudgetPolicyTests
{
    [Theory]
    [InlineData(1000, 0, BudgetState.Ok)]
    [InlineData(1000, 799, BudgetState.Ok)]
    [InlineData(1000, 800, BudgetState.Warning)]
    [InlineData(1000, 1000, BudgetState.Warning)]
    [InlineData(1000, 1000.01, BudgetState.Exceeded)]
    [InlineData(0, 0, BudgetState.Ok)]
    [InlineData(0, 1, BudgetState.Exceeded)]
    public void StateOf_follows_the_thresholds(double budget, double spent, BudgetState expected)
    {
        BudgetPolicy.StateOf((decimal)budget, (decimal)spent).ShouldBe(expected);
    }

    [Theory]
    [InlineData(100, 90, 150, true)]   // crossed now
    [InlineData(100, 100, 101, true)]  // exactly at the limit before
    [InlineData(100, 120, 150, false)] // was already over
    [InlineData(100, 10, 50, false)]   // still within
    public void CrossedLimit_only_fires_on_the_crossing(double budget, double before, double after, bool expected)
    {
        BudgetPolicy.CrossedLimit((decimal)budget, (decimal)before, (decimal)after).ShouldBe(expected);
    }

    [Fact]
    public void MonthOf_returns_the_inclusive_calendar_month()
    {
        BudgetPolicy.MonthOf(new DateOnly(2026, 2, 14)).ShouldBe((new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 28)));
    }
}

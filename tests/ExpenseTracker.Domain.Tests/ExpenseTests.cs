using ExpenseTracker.Domain.Common;
using ExpenseTracker.Domain.Expenses;

namespace ExpenseTracker.Domain.Tests;

public class ExpenseTests
{
    private static readonly Guid FamilyId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    private static readonly DateTimeOffset CreatedAt = new(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Food = Guid.CreateVersion7();
    private static readonly Guid Housing = Guid.CreateVersion7();

    private static ExpenseDetails Valid(decimal amount = 250m, string description = "Groceries", string? notes = null) =>
        new(description, amount, new DateOnly(2026, 10, 5), CategoryId: Food, notes);

    [Fact]
    public void Create_with_valid_details_trims_text_and_sets_values()
    {
        var result = Expense.Create(Valid(description: "  Groceries  ", notes: "  weekly  "), CreatedAt, FamilyId, UserId);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Description.ShouldBe("Groceries");
        result.Value.Notes.ShouldBe("weekly");
        result.Value.Amount.ShouldBe(250m);
        result.Value.CreatedAt.ShouldBe(CreatedAt);
    }

    [Fact]
    public void Blank_notes_are_stored_as_null()
    {
        Expense.Create(Valid(notes: "   "), CreatedAt, FamilyId, UserId).Value!.Notes.ShouldBeNull();
    }

    [Theory]
    [InlineData("", "description")]
    [InlineData("   ", "description")]
    public void Create_rejects_missing_description(string description, string field)
    {
        var result = Expense.Create(Valid(description: description), CreatedAt, FamilyId, UserId);

        result.IsSuccess.ShouldBeFalse();
        result.Error.Kind.ShouldBe(ErrorKind.Validation);
        result.Error.Field.ShouldBe(field);
    }

    [Fact]
    public void Create_rejects_overlong_description()
    {
        var result = Expense.Create(Valid(description: new string('x', Expense.DescriptionMaxLength + 1)), CreatedAt, FamilyId, UserId);
        result.Error!.Field.ShouldBe("description");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(1_000_000_000)]
    [InlineData(10.005)]
    public void Create_rejects_invalid_amounts(double amount)
    {
        var result = Expense.Create(Valid(amount: (decimal)amount), CreatedAt, FamilyId, UserId);

        result.IsSuccess.ShouldBeFalse();
        result.Error.Field.ShouldBe("amount");
    }

    [Fact]
    public void Update_changes_details_but_keeps_creation_time()
    {
        var expense = Expense.Create(Valid(), CreatedAt, FamilyId, UserId).Value!;

        var result = expense.Update(new ExpenseDetails("Dinner", 999.99m, new DateOnly(2026, 9, 1), Housing, null));

        result.IsSuccess.ShouldBeTrue();
        expense.Description.ShouldBe("Dinner");
        expense.Amount.ShouldBe(999.99m);
        expense.CategoryId.ShouldBe(Housing);
        expense.CreatedAt.ShouldBe(CreatedAt);
    }

    [Fact]
    public void Failed_update_leaves_the_expense_unchanged()
    {
        var expense = Expense.Create(Valid(), CreatedAt, FamilyId, UserId).Value!;

        var result = expense.Update(Valid(amount: 0) with { Description = "Changed" });

        result.IsSuccess.ShouldBeFalse();
        expense.Description.ShouldBe("Groceries");
        expense.Amount.ShouldBe(250m);
    }
}

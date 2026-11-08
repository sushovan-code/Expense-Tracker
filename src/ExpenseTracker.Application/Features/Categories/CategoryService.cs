using ExpenseTracker.Application.Abstractions.Persistence;
using ExpenseTracker.Application.Abstractions.Queries;
using ExpenseTracker.Application.Abstractions.Realtime;
using ExpenseTracker.Application.Common;
using ExpenseTracker.Domain.Categories;
using ExpenseTracker.Domain.Common;
using Microsoft.Extensions.Logging;

namespace ExpenseTracker.Application.Features.Categories;

public interface ICategoryService
{
    Task<Result<PagedResult<CategoryDto>>> ListAsync(CategoryListRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<CategoryLookupDto>> LookupAsync(CancellationToken cancellationToken);
    Task<Result<CategoryDto>> CreateAsync(CategoryCommand command, CancellationToken cancellationToken);
    Task<Result<CategoryDto>> UpdateAsync(int id, CategoryCommand command, CancellationToken cancellationToken);
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken);
}

internal sealed partial class CategoryService(
    ICategoryRepository categories,
    IUnitOfWork unitOfWork,
    ICategoryQueries queries,
    IChangeNotifier notifier,
    ILogger<CategoryService> logger) : ICategoryService
{
    public async Task<Result<PagedResult<CategoryDto>>> ListAsync(CategoryListRequest request, CancellationToken cancellationToken)
    {
        var sort = Paging.ResolveSort(request.SortBy, request.SortDir, CategorySorting.Default, defaultDescending: false, CategorySorting.Fields);
        if (!sort.IsSuccess) return sort.Error;

        var page = await queries.ListAsync(request.Search?.Trim(), request.Page, sort.Value, cancellationToken);
        LogCategoriesListed(logger, page.Items.Count, page.TotalCount, page.Page, page.PageSize, sort.Value.Field);
        return page;
    }

    public Task<IReadOnlyList<CategoryLookupDto>> LookupAsync(CancellationToken cancellationToken) => queries.LookupAsync(cancellationToken);

    public async Task<Result<CategoryDto>> CreateAsync(CategoryCommand command, CancellationToken cancellationToken)
    {
        if (await categories.NameExistsAsync(Category.NormalizeName(command.Name), excludeId: null, cancellationToken))
            return CategoryErrors.DuplicateName;

        var created = Category.Create(command.Name, command.Color, command.MonthlyBudget);
        if (!created.IsSuccess) return created.Error;
        var category = created.Value;

        categories.Add(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        LogCategoryCreated(logger, category.Id, category.Name, category.MonthlyBudget);
        await notifier.CategoriesChangedAsync(ChangeAction.Created, [category.Id], cancellationToken);
        return ToDto(category, expenseCount: 0);
    }

    public async Task<Result<CategoryDto>> UpdateAsync(int id, CategoryCommand command, CancellationToken cancellationToken)
    {
        var category = await categories.GetByIdAsync(id, cancellationToken);
        if (category is null)
        {
            LogCategoryNotFound(logger, id);
            return CategoryErrors.NotFound(id);
        }

        if (await categories.NameExistsAsync(Category.NormalizeName(command.Name), excludeId: id, cancellationToken))
            return CategoryErrors.DuplicateName;

        var previous = (category.Name, category.MonthlyBudget);
        var updated = category.Update(command.Name, command.Color, command.MonthlyBudget);
        if (!updated.IsSuccess) return updated.Error;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        LogCategoryUpdated(logger, category.Id, previous.Name, category.Name, previous.MonthlyBudget, category.MonthlyBudget);
        await notifier.CategoriesChangedAsync(ChangeAction.Updated, [category.Id], cancellationToken);

        return ToDto(category, await categories.CountExpensesAsync(id, cancellationToken));
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var category = await categories.GetByIdAsync(id, cancellationToken);
        if (category is null)
        {
            LogCategoryNotFound(logger, id);
            return CategoryErrors.NotFound(id);
        }

        var expenseCount = await categories.CountExpensesAsync(id, cancellationToken);
        if (expenseCount > 0)
        {
            LogCategoryInUse(logger, category.Id, category.Name, expenseCount);
            return CategoryErrors.InUse(category.Name, expenseCount);
        }

        categories.Remove(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        LogCategoryDeleted(logger, category.Id, category.Name);
        await notifier.CategoriesChangedAsync(ChangeAction.Deleted, [category.Id], cancellationToken);
        return Result.Success();
    }

    private static CategoryDto ToDto(Category c, int expenseCount) => new(c.Id, c.Name, c.Color, c.MonthlyBudget, expenseCount);

    [LoggerMessage(EventId = LogEvents.CategoryCreated, Level = LogLevel.Information,
        Message = "Created category {CategoryId} '{CategoryName}' with monthly budget {MonthlyBudget}")]
    private static partial void LogCategoryCreated(ILogger logger, int categoryId, string categoryName, decimal? monthlyBudget);

    [LoggerMessage(EventId = LogEvents.CategoryUpdated, Level = LogLevel.Information,
        Message = "Updated category {CategoryId}: name '{PreviousName}' → '{CategoryName}', budget {PreviousBudget} → {MonthlyBudget}")]
    private static partial void LogCategoryUpdated(
        ILogger logger, int categoryId, string previousName, string categoryName, decimal? previousBudget, decimal? monthlyBudget);

    [LoggerMessage(EventId = LogEvents.CategoryDeleted, Level = LogLevel.Information,
        Message = "Deleted category {CategoryId} '{CategoryName}'")]
    private static partial void LogCategoryDeleted(ILogger logger, int categoryId, string categoryName);

    [LoggerMessage(EventId = LogEvents.CategoryNotFound, Level = LogLevel.Warning, Message = "Category {CategoryId} was not found")]
    private static partial void LogCategoryNotFound(ILogger logger, int categoryId);

    [LoggerMessage(EventId = LogEvents.CategoryInUse, Level = LogLevel.Warning,
        Message = "Refused to delete category {CategoryId} '{CategoryName}': {ExpenseCount} expense(s) still use it")]
    private static partial void LogCategoryInUse(ILogger logger, int categoryId, string categoryName, int expenseCount);

    [LoggerMessage(EventId = LogEvents.CategoriesListed, Level = LogLevel.Debug,
        Message = "Listed {ItemCount} of {TotalCount} categories (page {Page}, size {PageSize}, sort {SortBy})")]
    private static partial void LogCategoriesListed(ILogger logger, int itemCount, int totalCount, int page, int pageSize, string sortBy);
}

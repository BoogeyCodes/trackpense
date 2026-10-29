using Trackpense.Core.Domain;

namespace Trackpense.Core.Services;

/// <summary>A year/month pair, used throughout the reporting API.</summary>
public readonly record struct MonthRef(int Year, int Month)
{
    public DateOnly FirstDay => new(Year, Month, 1);
    public DateOnly LastDay => new(Year, Month, DateTime.DaysInMonth(Year, Month));

    public static MonthRef Current => FromDate(DateOnly.FromDateTime(DateTime.Today));
    public static MonthRef FromDate(DateOnly date) => new(date.Year, date.Month);

    public MonthRef AddMonths(int months) => FromDate(FirstDay.AddMonths(months));
    public string Label => FirstDay.ToString("MMM yyyy");
    public override string ToString() => Label;
}

public sealed record CategorySpend(int CategoryId, string Name, string Color, decimal Spent, decimal? Budget)
{
    public bool HasBudget => Budget is > 0;
    public decimal Percent => HasBudget ? Math.Round(Spent / Budget!.Value * 100, 0, MidpointRounding.AwayFromZero) : 0;
    public bool OverBudget => HasBudget && Spent > Budget!.Value;
}

public sealed record MonthlySummary(
    MonthRef Month,
    decimal Income,
    decimal Expense,
    IReadOnlyList<CategorySpend> ByCategory)
{
    public decimal Net => Income - Expense;
    public decimal TotalBudget => ByCategory.Where(c => c.HasBudget).Sum(c => c.Budget!.Value);
    public int CategoriesOverBudget => ByCategory.Count(c => c.OverBudget);
}

public sealed record MonthlyTotals(MonthRef Month, decimal Income, decimal Expense)
{
    public decimal Net => Income - Expense;
}

public sealed record TransactionInput(DateOnly Date, decimal Amount, TransactionKind Kind, int CategoryId, string Note);

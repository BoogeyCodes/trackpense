using Trackpense.Core.Data;
using Trackpense.Core.Domain;
using Trackpense.Core.Services;

namespace Trackpense.Tests;

public sealed class SummaryServiceTests : IDisposable
{
    private readonly TestFactory _factory = new();
    private readonly SummaryService _summary;
    private readonly int _groceries;
    private readonly int _salary;
    private readonly int _dining;

    public SummaryServiceTests()
    {
        _summary = new SummaryService(_factory);
        using var db = _factory.CreateDbContext();
        var groceries = new Category { Name = "Groceries", Color = "#1" };
        var salary = new Category { Name = "Salary", Color = "#2" };
        var dining = new Category { Name = "Dining", Color = "#3" };
        db.Categories.AddRange(groceries, salary, dining);
        db.SaveChanges();
        _groceries = groceries.Id;
        _salary = salary.Id;
        _dining = dining.Id;
    }

    private void Seed(params Transaction[] transactions)
    {
        using var db = _factory.CreateDbContext();
        db.Transactions.AddRange(transactions);
        db.SaveChanges();
    }

    private Transaction Tx(int categoryId, decimal amount, TransactionKind kind, int year, int month, int day) =>
        new() { CategoryId = categoryId, Amount = amount, Kind = kind, Date = new DateOnly(year, month, day) };

    [Fact]
    public async Task Computes_income_expense_and_net_for_a_month()
    {
        Seed(
            Tx(_salary, 3000, TransactionKind.Income, 2026, 3, 1),
            Tx(_groceries, 200, TransactionKind.Expense, 2026, 3, 5),
            Tx(_dining, 50, TransactionKind.Expense, 2026, 3, 9));

        var summary = await _summary.ForMonthAsync(new MonthRef(2026, 3));

        Assert.Equal(3000, summary.Income);
        Assert.Equal(250, summary.Expense);
        Assert.Equal(2750, summary.Net);
    }

    [Fact]
    public async Task Ignores_transactions_outside_the_month()
    {
        Seed(
            Tx(_groceries, 100, TransactionKind.Expense, 2026, 3, 31),
            Tx(_groceries, 999, TransactionKind.Expense, 2026, 4, 1),
            Tx(_groceries, 999, TransactionKind.Expense, 2026, 2, 28));

        var summary = await _summary.ForMonthAsync(new MonthRef(2026, 3));

        Assert.Equal(100, summary.Expense);
    }

    [Fact]
    public async Task Groups_spending_by_category_biggest_first()
    {
        Seed(
            Tx(_groceries, 120, TransactionKind.Expense, 2026, 3, 2),
            Tx(_groceries, 80, TransactionKind.Expense, 2026, 3, 10),
            Tx(_dining, 60, TransactionKind.Expense, 2026, 3, 4));

        var summary = await _summary.ForMonthAsync(new MonthRef(2026, 3));

        Assert.Equal(2, summary.ByCategory.Count);
        Assert.Equal("Groceries", summary.ByCategory[0].Name);
        Assert.Equal(200, summary.ByCategory[0].Spent);
        Assert.Equal("Dining", summary.ByCategory[1].Name);
    }

    [Fact]
    public async Task Attaches_budgets_and_flags_overspend()
    {
        Seed(Tx(_groceries, 450, TransactionKind.Expense, 2026, 3, 2));
        using (var db = _factory.CreateDbContext())
        {
            db.Budgets.Add(new Budget { CategoryId = _groceries, Month = new DateOnly(2026, 3, 1), Limit = 400 });
            db.SaveChanges();
        }

        var row = (await _summary.ForMonthAsync(new MonthRef(2026, 3))).ByCategory.Single();
        Assert.True(row.HasBudget);
        Assert.Equal(400, row.Budget);
        Assert.True(row.OverBudget);
        Assert.Equal(113, row.Percent); // 450/400 = 112.5 -> 113
    }

    [Fact]
    public async Task Includes_a_category_with_a_budget_but_no_spend()
    {
        using (var db = _factory.CreateDbContext())
        {
            db.Budgets.Add(new Budget { CategoryId = _dining, Month = new DateOnly(2026, 3, 1), Limit = 150 });
            db.SaveChanges();
        }

        var row = (await _summary.ForMonthAsync(new MonthRef(2026, 3))).ByCategory.Single();
        Assert.Equal("Dining", row.Name);
        Assert.Equal(0, row.Spent);
        Assert.False(row.OverBudget);
    }

    [Fact]
    public async Task Trend_returns_one_row_per_month_in_order()
    {
        Seed(
            Tx(_salary, 1000, TransactionKind.Income, 2026, 1, 1),
            Tx(_groceries, 300, TransactionKind.Expense, 2026, 3, 1));

        var trend = await _summary.TrendAsync(new MonthRef(2026, 3), months: 3);

        Assert.Equal(3, trend.Count);
        Assert.Equal(new MonthRef(2026, 1), trend[0].Month);
        Assert.Equal(1000, trend[0].Income);
        Assert.Equal(300, trend[2].Expense);
        Assert.Equal(-300, trend[2].Net);
    }

    public void Dispose() => _factory.Dispose();
}

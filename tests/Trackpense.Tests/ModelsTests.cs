using Trackpense.Core.Services;

namespace Trackpense.Tests;

public sealed class ModelsTests
{
    [Fact]
    public void CategorySpend_Remaining_reflects_budget_minus_spent()
    {
        var under = new CategorySpend(1, "Food", "#0a0", Spent: 30m, Budget: 100m);
        Assert.Equal(70m, under.Remaining);
        Assert.False(under.OverBudget);

        var over = new CategorySpend(2, "Fun", "#a00", Spent: 120m, Budget: 100m);
        Assert.Equal(-20m, over.Remaining);
        Assert.True(over.OverBudget);

        var noBudget = new CategorySpend(3, "Misc", "#00a", Spent: 10m, Budget: null);
        Assert.Null(noBudget.Remaining);
    }

    [Fact]
    public void MonthlySummary_SavingsRate_is_net_over_income()
    {
        var summary = new MonthlySummary(MonthRef.Current, Income: 2000m, Expense: 1500m, ByCategory: []);
        Assert.Equal(500m, summary.Net);
        Assert.Equal(25m, summary.SavingsRate); // 500 / 2000 = 25%
    }

    [Fact]
    public void MonthlySummary_SavingsRate_is_zero_without_income()
    {
        var summary = new MonthlySummary(MonthRef.Current, Income: 0m, Expense: 200m, ByCategory: []);
        Assert.Equal(0m, summary.SavingsRate);
    }
}

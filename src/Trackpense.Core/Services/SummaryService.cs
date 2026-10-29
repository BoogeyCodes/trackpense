using Microsoft.EntityFrameworkCore;
using Trackpense.Core.Data;
using Trackpense.Core.Domain;

namespace Trackpense.Core.Services;

public interface ISummaryService
{
    Task<MonthlySummary> ForMonthAsync(MonthRef month, CancellationToken ct = default);
    Task<IReadOnlyList<MonthlyTotals>> TrendAsync(MonthRef through, int months, CancellationToken ct = default);
}

/// <summary>Turns the stored transactions and budgets into the figures the dashboard shows.</summary>
public sealed class SummaryService(IDbContextFactory<AppDbContext> factory) : ISummaryService
{
    public async Task<MonthlySummary> ForMonthAsync(MonthRef month, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var transactions = await db.Transactions
            .Where(t => t.Date >= month.FirstDay && t.Date <= month.LastDay)
            .Select(t => new { t.Amount, t.Kind, t.CategoryId })
            .ToListAsync(ct);

        var categories = await db.Categories.ToListAsync(ct);
        var budgets = await db.Budgets
            .Where(b => b.Month == month.FirstDay)
            .ToDictionaryAsync(b => b.CategoryId, b => b.Limit, ct);

        var income = transactions.Where(t => t.Kind == TransactionKind.Income).Sum(t => t.Amount);
        var expense = transactions.Where(t => t.Kind == TransactionKind.Expense).Sum(t => t.Amount);

        var spentByCategory = transactions
            .Where(t => t.Kind == TransactionKind.Expense)
            .GroupBy(t => t.CategoryId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

        var byCategory = categories
            .Select(c => new CategorySpend(
                c.Id,
                c.Name,
                c.Color,
                spentByCategory.GetValueOrDefault(c.Id),
                budgets.TryGetValue(c.Id, out var limit) ? limit : null))
            .Where(c => c.Spent > 0 || c.HasBudget)
            .OrderByDescending(c => c.Spent)
            .ToList();

        return new MonthlySummary(month, income, expense, byCategory);
    }

    public async Task<IReadOnlyList<MonthlyTotals>> TrendAsync(MonthRef through, int months, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var first = through.AddMonths(-(months - 1));
        var transactions = await db.Transactions
            .Where(t => t.Date >= first.FirstDay && t.Date <= through.LastDay)
            .Select(t => new { t.Date, t.Amount, t.Kind })
            .ToListAsync(ct);

        var result = new List<MonthlyTotals>(months);
        for (var i = 0; i < months; i++)
        {
            var month = first.AddMonths(i);
            var inMonth = transactions.Where(t => t.Date.Year == month.Year && t.Date.Month == month.Month).ToList();
            result.Add(new MonthlyTotals(
                month,
                inMonth.Where(t => t.Kind == TransactionKind.Income).Sum(t => t.Amount),
                inMonth.Where(t => t.Kind == TransactionKind.Expense).Sum(t => t.Amount)));
        }

        return result;
    }
}

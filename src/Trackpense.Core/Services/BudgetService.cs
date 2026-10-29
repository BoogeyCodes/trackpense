using Microsoft.EntityFrameworkCore;
using Trackpense.Core.Data;
using Trackpense.Core.Domain;

namespace Trackpense.Core.Services;

public interface IBudgetService
{
    Task<IReadOnlyList<Budget>> ForMonthAsync(MonthRef month, CancellationToken ct = default);

    /// <summary>Set (or clear, when limit is null/0) a category's budget for a month.</summary>
    Task SetAsync(int categoryId, MonthRef month, decimal? limit, CancellationToken ct = default);
}

public sealed class BudgetService(IDbContextFactory<AppDbContext> factory) : IBudgetService
{
    public async Task<IReadOnlyList<Budget>> ForMonthAsync(MonthRef month, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Budgets.Include(b => b.Category).Where(b => b.Month == month.FirstDay).ToListAsync(ct);
    }

    public async Task SetAsync(int categoryId, MonthRef month, decimal? limit, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var existing = await db.Budgets.FirstOrDefaultAsync(b => b.CategoryId == categoryId && b.Month == month.FirstDay, ct);

        if (limit is null or <= 0)
        {
            if (existing is not null)
            {
                db.Budgets.Remove(existing);
                await db.SaveChangesAsync(ct);
            }
            return;
        }

        if (existing is null)
        {
            db.Budgets.Add(new Budget { CategoryId = categoryId, Month = month.FirstDay, Limit = limit.Value });
        }
        else
        {
            existing.Limit = limit.Value;
        }
        await db.SaveChangesAsync(ct);
    }
}

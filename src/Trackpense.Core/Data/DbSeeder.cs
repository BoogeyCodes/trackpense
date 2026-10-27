using Microsoft.EntityFrameworkCore;
using Trackpense.Core.Domain;

namespace Trackpense.Core.Data;

/// <summary>Creates the schema and, on an empty database, loads demo categories and a few months of data.</summary>
public static class DbSeeder
{
    private static readonly (string Name, string Color)[] Categories =
    [
        ("Salary", "#22c55e"),
        ("Groceries", "#3b82f6"),
        ("Rent", "#8b5cf6"),
        ("Dining", "#f59e0b"),
        ("Transport", "#06b6d4"),
        ("Utilities", "#ef4444"),
        ("Entertainment", "#ec4899"),
        ("Health", "#14b8a6"),
    ];

    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        await db.Database.EnsureCreatedAsync(ct);
        if (await db.Categories.AnyAsync(ct))
        {
            return;
        }

        var categories = Categories.Select(c => new Category { Name = c.Name, Color = c.Color }).ToList();
        db.Categories.AddRange(categories);
        await db.SaveChangesAsync(ct);

        var byName = categories.ToDictionary(c => c.Name, c => c.Id);
        var random = new Random(42);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var transactions = new List<Transaction>();
        var budgets = new List<Budget>();

        // Three months of plausible data, including this month.
        for (var monthOffset = 2; monthOffset >= 0; monthOffset--)
        {
            var monthStart = new DateOnly(today.Year, today.Month, 1).AddMonths(-monthOffset);

            transactions.Add(new Transaction
            {
                Date = monthStart,
                Amount = 3200,
                Kind = TransactionKind.Income,
                CategoryId = byName["Salary"],
                Note = "Monthly salary",
            });
            transactions.Add(new Transaction
            {
                Date = monthStart.AddDays(1),
                Amount = 1100,
                Kind = TransactionKind.Expense,
                CategoryId = byName["Rent"],
                Note = "Rent",
            });

            AddRandom(transactions, random, monthStart, byName["Groceries"], count: 6, min: 20, max: 90, "Groceries");
            AddRandom(transactions, random, monthStart, byName["Dining"], count: 4, min: 12, max: 55, "Lunch");
            AddRandom(transactions, random, monthStart, byName["Transport"], count: 5, min: 5, max: 40, "Transport");
            AddRandom(transactions, random, monthStart, byName["Utilities"], count: 2, min: 40, max: 120, "Utilities");
            AddRandom(transactions, random, monthStart, byName["Entertainment"], count: 3, min: 10, max: 60, "Entertainment");

            foreach (var (name, limit) in new[] { ("Groceries", 400m), ("Dining", 200m), ("Transport", 150m), ("Entertainment", 120m) })
            {
                budgets.Add(new Budget { CategoryId = byName[name], Month = monthStart, Limit = limit });
            }
        }

        db.Transactions.AddRange(transactions);
        db.Budgets.AddRange(budgets);
        await db.SaveChangesAsync(ct);
    }

    private static void AddRandom(
        List<Transaction> into, Random random, DateOnly monthStart, int categoryId, int count, int min, int max, string note)
    {
        var daysInMonth = DateTime.DaysInMonth(monthStart.Year, monthStart.Month);
        for (var i = 0; i < count; i++)
        {
            into.Add(new Transaction
            {
                Date = monthStart.AddDays(random.Next(daysInMonth)),
                Amount = random.Next(min, max) + Math.Round((decimal)random.NextDouble(), 2),
                Kind = TransactionKind.Expense,
                CategoryId = categoryId,
                Note = note,
            });
        }
    }
}

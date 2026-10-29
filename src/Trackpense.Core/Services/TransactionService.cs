using Microsoft.EntityFrameworkCore;
using Trackpense.Core.Data;
using Trackpense.Core.Domain;

namespace Trackpense.Core.Services;

public interface ITransactionService
{
    Task<IReadOnlyList<Transaction>> ListAsync(MonthRef month, int? categoryId = null, CancellationToken ct = default);
    Task<Transaction> AddAsync(TransactionInput input, CancellationToken ct = default);
    Task<bool> UpdateAsync(int id, TransactionInput input, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Category>> CategoriesAsync(CancellationToken ct = default);
}

/// <summary>
/// Uses a <see cref="IDbContextFactory{T}"/> to create a short-lived context per call — the
/// recommended pattern for Blazor Server, where a scoped DbContext would be shared across a circuit.
/// </summary>
public sealed class TransactionService(IDbContextFactory<AppDbContext> factory) : ITransactionService
{
    public async Task<IReadOnlyList<Transaction>> ListAsync(MonthRef month, int? categoryId = null, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var query = db.Transactions
            .Include(t => t.Category)
            .Where(t => t.Date >= month.FirstDay && t.Date <= month.LastDay);

        if (categoryId is { } id)
        {
            query = query.Where(t => t.CategoryId == id);
        }

        return await query.OrderByDescending(t => t.Date).ThenByDescending(t => t.Id).ToListAsync(ct);
    }

    public async Task<Transaction> AddAsync(TransactionInput input, CancellationToken ct = default)
    {
        Validate(input);
        await using var db = await factory.CreateDbContextAsync(ct);
        var transaction = new Transaction
        {
            Date = input.Date,
            Amount = input.Amount,
            Kind = input.Kind,
            CategoryId = input.CategoryId,
            Note = input.Note.Trim(),
        };
        db.Transactions.Add(transaction);
        await db.SaveChangesAsync(ct);
        return transaction;
    }

    public async Task<bool> UpdateAsync(int id, TransactionInput input, CancellationToken ct = default)
    {
        Validate(input);
        await using var db = await factory.CreateDbContextAsync(ct);
        var transaction = await db.Transactions.FindAsync([id], ct);
        if (transaction is null)
        {
            return false;
        }

        transaction.Date = input.Date;
        transaction.Amount = input.Amount;
        transaction.Kind = input.Kind;
        transaction.CategoryId = input.CategoryId;
        transaction.Note = input.Note.Trim();
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var deleted = await db.Transactions.Where(t => t.Id == id).ExecuteDeleteAsync(ct);
        return deleted > 0;
    }

    public async Task<IReadOnlyList<Category>> CategoriesAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Categories.OrderBy(c => c.Name).ToListAsync(ct);
    }

    private static void Validate(TransactionInput input)
    {
        if (input.Amount <= 0)
        {
            throw new ArgumentException("Amount must be greater than zero.", nameof(input));
        }
        if (input.CategoryId <= 0)
        {
            throw new ArgumentException("A category is required.", nameof(input));
        }
    }
}

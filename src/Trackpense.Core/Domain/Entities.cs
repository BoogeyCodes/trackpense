namespace Trackpense.Core.Domain;

public enum TransactionKind
{
    Expense,
    Income,
}

/// <summary>A spending/earning category, e.g. Groceries or Salary.</summary>
public sealed class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>A hex colour used for the charts and chips, e.g. <c>#3b82f6</c>.</summary>
    public string Color { get; set; } = "#64748b";

    public List<Transaction> Transactions { get; set; } = [];
}

/// <summary>A single income or expense entry.</summary>
public sealed class Transaction
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }

    /// <summary>Always stored as a positive amount; the <see cref="Kind"/> gives the direction.</summary>
    public decimal Amount { get; set; }

    public TransactionKind Kind { get; set; }
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
    public string Note { get; set; } = string.Empty;

    /// <summary>Signed value: positive for income, negative for an expense.</summary>
    public decimal SignedAmount => Kind == TransactionKind.Income ? Amount : -Amount;
}

/// <summary>A monthly spending limit for a category.</summary>
public sealed class Budget
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    /// <summary>The first day of the month the budget applies to.</summary>
    public DateOnly Month { get; set; }

    public decimal Limit { get; set; }
}

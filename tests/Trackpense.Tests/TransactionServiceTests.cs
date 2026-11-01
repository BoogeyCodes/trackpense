using Trackpense.Core.Domain;
using Trackpense.Core.Services;

namespace Trackpense.Tests;

public sealed class TransactionServiceTests : IDisposable
{
    private readonly TestFactory _factory = new();
    private readonly TransactionService _service;
    private readonly int _category;

    public TransactionServiceTests()
    {
        _service = new TransactionService(_factory);
        using var db = _factory.CreateDbContext();
        var category = new Category { Name = "Groceries", Color = "#1" };
        db.Categories.Add(category);
        db.SaveChanges();
        _category = category.Id;
    }

    private TransactionInput Input(decimal amount = 25, int categoryId = 0, string note = "  milk  ") =>
        new(new DateOnly(2026, 3, 10), amount, TransactionKind.Expense, categoryId == 0 ? _category : categoryId, note);

    [Fact]
    public async Task Add_persists_and_trims_the_note()
    {
        var created = await _service.AddAsync(Input());
        Assert.True(created.Id > 0);
        Assert.Equal("milk", created.Note);

        var listed = await _service.ListAsync(new MonthRef(2026, 3));
        Assert.Single(listed);
        Assert.Equal("Groceries", listed[0].Category!.Name);
    }

    [Fact]
    public async Task List_filters_by_month_and_category()
    {
        await _service.AddAsync(Input() with { Date = new DateOnly(2026, 3, 1) });
        await _service.AddAsync(Input() with { Date = new DateOnly(2026, 4, 1) });

        Assert.Single(await _service.ListAsync(new MonthRef(2026, 3)));
        Assert.Single(await _service.ListAsync(new MonthRef(2026, 3), _category));
        Assert.Empty(await _service.ListAsync(new MonthRef(2026, 3), categoryId: 9999));
    }

    [Fact]
    public async Task Update_changes_fields()
    {
        var created = await _service.AddAsync(Input());
        var ok = await _service.UpdateAsync(created.Id, Input(amount: 99, note: "bread"));
        Assert.True(ok);

        var listed = await _service.ListAsync(new MonthRef(2026, 3));
        Assert.Equal(99, listed[0].Amount);
        Assert.Equal("bread", listed[0].Note);
    }

    [Fact]
    public async Task Update_and_delete_report_missing_ids()
    {
        Assert.False(await _service.UpdateAsync(123, Input()));
        Assert.False(await _service.DeleteAsync(123));
    }

    [Fact]
    public async Task Delete_removes_the_transaction()
    {
        var created = await _service.AddAsync(Input());
        Assert.True(await _service.DeleteAsync(created.Id));
        Assert.Empty(await _service.ListAsync(new MonthRef(2026, 3)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task Add_rejects_non_positive_amounts(decimal amount)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.AddAsync(Input(amount: amount)));
    }

    [Fact]
    public async Task Add_requires_a_category()
    {
        var input = new TransactionInput(new DateOnly(2026, 3, 1), 10, TransactionKind.Expense, 0, "x");
        await Assert.ThrowsAsync<ArgumentException>(() => _service.AddAsync(input));
    }

    public void Dispose() => _factory.Dispose();
}

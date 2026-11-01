using Trackpense.Core.Domain;
using Trackpense.Core.Services;

namespace Trackpense.Tests;

public sealed class BudgetServiceTests : IDisposable
{
    private readonly TestFactory _factory = new();
    private readonly BudgetService _service;
    private readonly int _category;
    private readonly MonthRef _month = new(2026, 3);

    public BudgetServiceTests()
    {
        _service = new BudgetService(_factory);
        using var db = _factory.CreateDbContext();
        var category = new Category { Name = "Dining", Color = "#1" };
        db.Categories.Add(category);
        db.SaveChanges();
        _category = category.Id;
    }

    [Fact]
    public async Task Set_creates_a_budget()
    {
        await _service.SetAsync(_category, _month, 200);
        var budgets = await _service.ForMonthAsync(_month);
        Assert.Single(budgets);
        Assert.Equal(200, budgets[0].Limit);
    }

    [Fact]
    public async Task Set_updates_an_existing_budget_rather_than_duplicating()
    {
        await _service.SetAsync(_category, _month, 200);
        await _service.SetAsync(_category, _month, 350);
        var budgets = await _service.ForMonthAsync(_month);
        Assert.Single(budgets);
        Assert.Equal(350, budgets[0].Limit);
    }

    [Fact]
    public async Task Set_to_zero_or_null_clears_the_budget()
    {
        await _service.SetAsync(_category, _month, 200);
        await _service.SetAsync(_category, _month, 0);
        Assert.Empty(await _service.ForMonthAsync(_month));

        await _service.SetAsync(_category, _month, 200);
        await _service.SetAsync(_category, _month, null);
        Assert.Empty(await _service.ForMonthAsync(_month));
    }

    [Fact]
    public async Task Budgets_are_scoped_to_their_month()
    {
        await _service.SetAsync(_category, _month, 200);
        Assert.Empty(await _service.ForMonthAsync(_month.AddMonths(1)));
    }

    public void Dispose() => _factory.Dispose();
}

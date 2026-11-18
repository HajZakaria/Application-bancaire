using BankSim.Data;
using BankSim.Models;
using Microsoft.EntityFrameworkCore;

namespace BankSim.Services;

public record CategoryTotal(Category Category, decimal Total);
public record MonthlyFlow(int Year, int Month, decimal Income, decimal Expenses);

public class StatsService
{
    private readonly IDbContextFactory<BankDbContext> _factory;

    public StatsService(IDbContextFactory<BankDbContext> factory) => _factory = factory;

    public async Task<List<CategoryTotal>> SpendingByCategoryAsync(int accountId)
    {
        var start = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        await using var db = await _factory.CreateDbContextAsync();
        var rows = await db.Transactions
            .Where(t => t.FromAccountId == accountId && t.CreatedAt >= start)
            .ToListAsync();

        return rows
            .GroupBy(t => t.Category)
            .Select(g => new CategoryTotal(g.Key, g.Sum(t => t.Amount)))
            .OrderByDescending(x => x.Total)
            .ToList();
    }

    public async Task<List<MonthlyFlow>> MonthlyFlowAsync(int accountId, int months = 6)
    {
        var start = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(-(months - 1));

        await using var db = await _factory.CreateDbContextAsync();
        var rows = await db.Transactions
            .Where(t => (t.FromAccountId == accountId || t.ToAccountId == accountId)
                        && t.CreatedAt >= start)
            .ToListAsync();

        return rows
            .GroupBy(t => new { t.CreatedAt.Year, t.CreatedAt.Month })
            .Select(g => new MonthlyFlow(
                g.Key.Year,
                g.Key.Month,
                g.Where(t => t.ToAccountId == accountId).Sum(t => t.Amount),
                g.Where(t => t.FromAccountId == accountId).Sum(t => t.Amount)))
            .OrderBy(m => m.Year).ThenBy(m => m.Month)
            .ToList();
    }
}
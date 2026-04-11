using KokoAnalytics.Domain.Entities;
using KokoAnalytics.Domain.Interfaces;
using KokoAnalytics.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KokoAnalytics.Infrastructure.Repositories;

public class DailyStatRepository : IDailyStatRepository
{
    private readonly AnalyticsDbContext _context;

    public DailyStatRepository(AnalyticsDbContext context)
    {
        _context = context;
    }

    public async Task<List<DailyStat>> GetByDateRangeAsync(DateTime start, DateTime end)
    {
        return await _context.DailyStats
            .Where(d => d.Date >= start && d.Date <= end)
            .OrderBy(d => d.Date)
            .ToListAsync();
    }

    public async Task<DailyStat?> GetByDateAsync(DateTime date)
    {
        return await _context.DailyStats
            .FirstOrDefaultAsync(d => d.Date == date);
    }

    public async Task<HashSet<DateTime>> GetExistingDatesAsync()
    {
        var dates = await _context.DailyStats
            .Select(d => d.Date)
            .ToListAsync();
        return new HashSet<DateTime>(dates);
    }

    public async Task AddRangeAsync(IEnumerable<DailyStat> stats)
    {
        await _context.DailyStats.AddRangeAsync(stats);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
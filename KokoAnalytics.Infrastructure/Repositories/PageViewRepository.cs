using KokoAnalytics.Domain.Entities;
using KokoAnalytics.Domain.Interfaces;
using KokoAnalytics.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KokoAnalytics.Infrastructure.Repositories;

public class PageViewRepository : IPageViewRepository
{
    private readonly AnalyticsDbContext _context;

    public PageViewRepository(AnalyticsDbContext context)
    {
        _context = context;
    }

    public async Task<List<PageView>> GetByDateRangeAsync(DateTime start, DateTime end)
    {
        return await _context.PageViews
            .Where(p => p.Date >= start && p.Date <= end)
            .ToListAsync();
    }

    public async Task AddRangeAsync(IEnumerable<PageView> pageViews)
    {
        await _context.PageViews.AddRangeAsync(pageViews);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
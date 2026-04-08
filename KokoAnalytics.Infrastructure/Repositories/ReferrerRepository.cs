using KokoAnalytics.Domain.Entities;
using KokoAnalytics.Domain.Interfaces;
using KokoAnalytics.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KokoAnalytics.Infrastructure.Repositories;

public class ReferrerRepository : IReferrerRepository
{
    private readonly AnalyticsDbContext _context;

    public ReferrerRepository(AnalyticsDbContext context)
    {
        _context = context;
    }

    public async Task<List<Referrer>> GetByDateRangeAsync(DateTime start, DateTime end)
    {
        return await _context.Referrers
            .Where(r => r.Date >= start && r.Date <= end)
            .ToListAsync();
    }

    public async Task AddRangeAsync(IEnumerable<Referrer> referrers)
    {
        await _context.Referrers.AddRangeAsync(referrers);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
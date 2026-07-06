using KokoAnalytics.Domain.Entities;
using KokoAnalytics.Domain.Interfaces;
using KokoAnalytics.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KokoAnalytics.Infrastructure.Repositories;

public class PathRepository : IPathRepository
{
    private readonly AnalyticsDbContext _context;

    public PathRepository(AnalyticsDbContext context)
    {
        _context = context;
    }

    public async Task<AnalyticsPath?> GetByUrlAsync(string url)
    {
        return await _context.AnalyticsPaths
            .FirstOrDefaultAsync(p => p.PathUrl == url);
    }

    public async Task<IEnumerable<AnalyticsPath>> GetAllAsync()
    {
        return await _context.AnalyticsPaths.ToListAsync();
    }

    public async Task AddAsync(AnalyticsPath path)
    {
        await _context.AnalyticsPaths.AddAsync(path);
        await _context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<AnalyticsPath> paths)
    {
        await _context.AnalyticsPaths.AddRangeAsync(paths);
        await _context.SaveChangesAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}


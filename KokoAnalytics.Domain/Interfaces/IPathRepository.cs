using KokoAnalytics.Domain.Entities;

namespace KokoAnalytics.Domain.Interfaces;

public interface IPathRepository
{
    Task<AnalyticsPath?> GetByUrlAsync(string url);
    Task<IEnumerable<AnalyticsPath>> GetAllAsync();
    Task AddAsync(AnalyticsPath path);
    Task AddRangeAsync(IEnumerable<AnalyticsPath> paths);
    Task SaveChangesAsync();
}

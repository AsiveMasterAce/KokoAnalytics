using KokoAnalytics.Domain.Entities;

namespace KokoAnalytics.Domain.Interfaces;

public interface IPageViewRepository
{
    Task<List<PageView>> GetByDateRangeAsync(DateTime start, DateTime end);
    Task AddRangeAsync(IEnumerable<PageView> pageViews);
    Task SaveChangesAsync();
}
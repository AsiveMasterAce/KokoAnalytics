using KokoAnalytics.Domain.Entities;

namespace KokoAnalytics.Domain.Interfaces;

public interface IDailyStatRepository
{
    Task<List<DailyStat>> GetByDateRangeAsync(DateTime start, DateTime end);
    Task<DailyStat?> GetByDateAsync(DateTime date);
    Task<HashSet<DateTime>> GetExistingDatesAsync();
    Task AddRangeAsync(IEnumerable<DailyStat> stats);
    Task SaveChangesAsync();
}
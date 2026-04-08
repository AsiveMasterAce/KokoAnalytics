using KokoAnalytics.Domain.Entities;

namespace KokoAnalytics.Domain.Interfaces;

public interface IReferrerRepository
{
    Task<List<Referrer>> GetByDateRangeAsync(DateTime start, DateTime end);
    Task AddRangeAsync(IEnumerable<Referrer> referrers);
    Task SaveChangesAsync();
}
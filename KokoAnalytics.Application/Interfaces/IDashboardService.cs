using KokoAnalytics.Application.DTOs;

namespace KokoAnalytics.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardDto> GetDashboardAsync(DateTime? start, DateTime? end);
}
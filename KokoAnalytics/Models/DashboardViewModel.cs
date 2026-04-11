using KokoAnalytics.Application.DTOs;

namespace KokoAnalytics.Models;

/// <summary>
/// Thin presentation model — maps directly from the Application layer DTO.
/// </summary>
public class DashboardViewModel
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public int TotalViewsToday { get; set; }
    public int TotalVisitorsToday { get; set; }
    public decimal BounceRateToday { get; set; }
    public int TotalViewsInRange { get; set; }
    public int TotalVisitorsInRange { get; set; }
    public decimal AvgBounceRateInRange { get; set; }

    public double ViewsTrendPercent { get; set; }
    public double VisitorsTrendPercent { get; set; }

    public List<string> ChartLabels { get; set; } = [];
    public List<int> ChartViews { get; set; } = [];
    public List<int> ChartVisitors { get; set; } = [];
    public List<decimal> ChartBounceRates { get; set; } = [];

    public List<string> ReferrerLabels { get; set; } = [];
    public List<int> ReferrerData { get; set; } = [];

    public List<PageSummaryDto> TopPages { get; set; } = [];
    public List<ReferrerSummaryDto> TopReferrers { get; set; } = [];
    public List<PageSparklineDto> PageSparklines { get; set; } = [];

    public static DashboardViewModel FromDto(DashboardDto dto) => new()
    {
        StartDate = dto.StartDate,
        EndDate = dto.EndDate,
        TotalViewsToday = dto.TotalViewsToday,
        TotalVisitorsToday = dto.TotalVisitorsToday,
        BounceRateToday = dto.BounceRateToday,
        TotalViewsInRange = dto.TotalViewsInRange,
        TotalVisitorsInRange = dto.TotalVisitorsInRange,
        AvgBounceRateInRange = dto.AvgBounceRateInRange,
        ViewsTrendPercent = dto.ViewsTrendPercent,
        VisitorsTrendPercent = dto.VisitorsTrendPercent,
        ChartLabels = dto.ChartLabels,
        ChartViews = dto.ChartViews,
        ChartVisitors = dto.ChartVisitors,
        ChartBounceRates = dto.ChartBounceRates,
        ReferrerLabels = dto.ReferrerLabels,
        ReferrerData = dto.ReferrerData,
        TopPages = dto.TopPages,
        TopReferrers = dto.TopReferrers,
        PageSparklines = dto.PageSparklines
    };
}
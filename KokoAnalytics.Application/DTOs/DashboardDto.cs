namespace KokoAnalytics.Application.DTOs;

public class DashboardDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    // Summary cards
    public int TotalViewsToday { get; set; }
    public int TotalVisitorsToday { get; set; }
    public decimal BounceRateToday { get; set; }
    public int TotalViewsInRange { get; set; }
    public int TotalVisitorsInRange { get; set; }
    public decimal AvgBounceRateInRange { get; set; }

    // Trends
    public double ViewsTrendPercent { get; set; }
    public double VisitorsTrendPercent { get; set; }

    // Charts
    public List<string> ChartLabels { get; set; } = [];
    public List<int> ChartViews { get; set; } = [];
    public List<int> ChartVisitors { get; set; } = [];
    public List<decimal> ChartBounceRates { get; set; } = [];

    // Referrer chart
    public List<string> ReferrerLabels { get; set; } = [];
    public List<int> ReferrerData { get; set; } = [];

    // Tables
    public List<PageSummaryDto> TopPages { get; set; } = [];
    public List<ReferrerSummaryDto> TopReferrers { get; set; } = [];
    public List<PageSparklineDto> PageSparklines { get; set; } = [];
}

public class PageSummaryDto
{
    public string PageUrl { get; set; } = string.Empty;
    public string PageTitle { get; set; } = string.Empty;
    public int TotalViews { get; set; }
    public int TotalUniqueVisitors { get; set; }
}

public class ReferrerSummaryDto
{
    public string ReferrerUrl { get; set; } = string.Empty;
    public int TotalVisits { get; set; }
}

public class PageSparklineDto
{
    public string PageTitle { get; set; } = string.Empty;
    public List<int> DailyViews { get; set; } = [];
}
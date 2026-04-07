namespace KokoAnalytics.Models;

public class DashboardViewModel
{
    // Date range filter
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    // Summary cards
    public int TotalViewsToday { get; set; }
    public int TotalVisitorsToday { get; set; }
    public decimal BounceRateToday { get; set; }
    public int TotalViewsInRange { get; set; }
    public int TotalVisitorsInRange { get; set; }
    public decimal AvgBounceRateInRange { get; set; }

    // Trend vs previous period
    public double ViewsTrendPercent { get; set; }
    public double VisitorsTrendPercent { get; set; }

    // Line chart: views & visitors over time
    public List<string> ChartLabels { get; set; } = new();
    public List<int> ChartViews { get; set; } = new();
    public List<int> ChartVisitors { get; set; } = new();

    // Bar chart: bounce rate over time
    public List<decimal> ChartBounceRates { get; set; } = new();

    // Doughnut chart: top referrer breakdown
    public List<string> ReferrerLabels { get; set; } = new();
    public List<int> ReferrerData { get; set; } = new();

    // Top pages
    public List<PageSummary> TopPages { get; set; } = new();

    // Top referrers
    public List<ReferrerSummary> TopReferrers { get; set; } = new();

    // Per-page sparkline data (top 5 pages daily views)
    public List<PageSparkline> PageSparklines { get; set; } = new();
}

public class PageSummary
{
    public string PageUrl { get; set; } = string.Empty;
    public string PageTitle { get; set; } = string.Empty;
    public int TotalViews { get; set; }
    public int TotalUniqueVisitors { get; set; }
}

public class ReferrerSummary
{
    public string ReferrerUrl { get; set; } = string.Empty;
    public int TotalVisits { get; set; }
}

public class PageSparkline
{
    public string PageTitle { get; set; } = string.Empty;
    public List<int> DailyViews { get; set; } = new();
}
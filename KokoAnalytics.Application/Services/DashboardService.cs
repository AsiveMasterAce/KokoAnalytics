using KokoAnalytics.Application.DTOs;
using KokoAnalytics.Application.Interfaces;
using KokoAnalytics.Domain.Interfaces;

namespace KokoAnalytics.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IDailyStatRepository _dailyStatRepo;
    private readonly IPageViewRepository _pageViewRepo;
    private readonly IReferrerRepository _referrerRepo;

    public DashboardService(
        IDailyStatRepository dailyStatRepo,
        IPageViewRepository pageViewRepo,
        IReferrerRepository referrerRepo)
    {
        _dailyStatRepo = dailyStatRepo;
        _pageViewRepo = pageViewRepo;
        _referrerRepo = referrerRepo;
    }

    public async Task<DashboardDto> GetDashboardAsync(DateTime? start, DateTime? end)
    {
        var today = DateTime.Today;
        var endDate = end?.Date ?? today;
        var startDate = start?.Date ?? endDate.AddDays(-29);

        if (startDate > endDate)
            (startDate, endDate) = (endDate, startDate);

        var rangeDays = (endDate - startDate).Days + 1;

        // Current period
        var dailyStats = await _dailyStatRepo.GetByDateRangeAsync(startDate, endDate);
        var todayStat = await _dailyStatRepo.GetByDateAsync(today);

        // Previous period
        var prevStart = startDate.AddDays(-rangeDays);
        var prevEnd = startDate.AddDays(-1);
        var prevStats = await _dailyStatRepo.GetByDateRangeAsync(prevStart, prevEnd);

        var currentViews = dailyStats.Sum(d => d.TotalViews);
        var currentVisitors = dailyStats.Sum(d => d.TotalVisitors);
        var prevViews = prevStats.Sum(d => d.TotalViews);
        var prevVisitors = prevStats.Sum(d => d.TotalVisitors);

        // Top pages
        var pageViews = await _pageViewRepo.GetByDateRangeAsync(startDate, endDate);

        var topPages = pageViews
            .GroupBy(p => new { p.PageUrl, p.PageTitle })
            .Select(g => new PageSummaryDto
            {
                PageUrl = g.Key.PageUrl,
                PageTitle = g.Key.PageTitle,
                TotalViews = g.Sum(x => x.ViewCount),
                TotalUniqueVisitors = g.Sum(x => x.UniqueVisitors)
            })
            .OrderByDescending(p => p.TotalViews)
            .Take(10)
            .ToList();

        // Page sparklines (top 5)
        var top5Urls = topPages.Take(5).Select(p => p.PageUrl).ToList();
        var sparklineData = pageViews
            .Where(p => top5Urls.Contains(p.PageUrl))
            .GroupBy(p => new { p.PageUrl, p.PageTitle, p.Date })
            .Select(g => new { g.Key.PageUrl, g.Key.PageTitle, g.Key.Date, Views = g.Sum(x => x.ViewCount) })
            .ToList();

        var allDates = Enumerable.Range(0, rangeDays)
            .Select(i => startDate.AddDays(i))
            .ToList();

        var sparklines = top5Urls.Select(url =>
        {
            var byDate = sparklineData
                .Where(s => s.PageUrl == url)
                .ToDictionary(s => s.Date, s => s.Views);
            return new PageSparklineDto
            {
                PageTitle = sparklineData.FirstOrDefault(s => s.PageUrl == url)?.PageTitle ?? url,
                DailyViews = allDates.Select(d => byDate.GetValueOrDefault(d, 0)).ToList()
            };
        }).ToList();

        // Top referrers
        var referrers = await _referrerRepo.GetByDateRangeAsync(startDate, endDate);

        var topReferrers = referrers
            .GroupBy(r => r.ReferrerUrl)
            .Select(g => new ReferrerSummaryDto
            {
                ReferrerUrl = g.Key,
                TotalVisits = g.Sum(x => x.VisitCount)
            })
            .OrderByDescending(r => r.TotalVisits)
            .Take(10)
            .ToList();

        return new DashboardDto
        {
            StartDate = startDate,
            EndDate = endDate,

            TotalViewsToday = todayStat?.TotalViews ?? 0,
            TotalVisitorsToday = todayStat?.TotalVisitors ?? 0,
            BounceRateToday = todayStat?.BounceRate ?? 0,

            TotalViewsInRange = currentViews,
            TotalVisitorsInRange = currentVisitors,
            AvgBounceRateInRange = dailyStats.Count > 0
                ? Math.Round(dailyStats.Average(d => d.BounceRate), 1)
                : 0,

            ViewsTrendPercent = prevViews > 0
                ? Math.Round((currentViews - prevViews) / (double)prevViews * 100, 1)
                : 0,
            VisitorsTrendPercent = prevVisitors > 0
                ? Math.Round((currentVisitors - prevVisitors) / (double)prevVisitors * 100, 1)
                : 0,

            ChartLabels = dailyStats.Select(d => d.Date.ToString("MMM dd")).ToList(),
            ChartViews = dailyStats.Select(d => d.TotalViews).ToList(),
            ChartVisitors = dailyStats.Select(d => d.TotalVisitors).ToList(),
            ChartBounceRates = dailyStats.Select(d => d.BounceRate).ToList(),

            ReferrerLabels = topReferrers.Take(6).Select(r => TruncateUrl(r.ReferrerUrl)).ToList(),
            ReferrerData = topReferrers.Take(6).Select(r => r.TotalVisits).ToList(),

            TopPages = topPages,
            TopReferrers = topReferrers,
            PageSparklines = sparklines
        };
    }

    private static string TruncateUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return "(direct)";
        try
        {
            var host = new Uri(url).Host.Replace("www.", "");
            return host.Length > 20 ? host[..20] + "…" : host;
        }
        catch
        {
            return url.Length > 25 ? url[..25] + "…" : url;
        }
    }
}
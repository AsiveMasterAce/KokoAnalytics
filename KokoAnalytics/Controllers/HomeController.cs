using System.Diagnostics;
using KokoAnalytics.Data;
using KokoAnalytics.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KokoAnalytics.Controllers;

public class HomeController : Controller
{
    private readonly AnalyticsDbContext _context;

    public HomeController(AnalyticsDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(DateTime? start, DateTime? end)
    {
        var today = DateTime.Today;
        var endDate = end?.Date ?? today;
        var startDate = start?.Date ?? endDate.AddDays(-29);

        // Clamp so start <= end
        if (startDate > endDate) (startDate, endDate) = (endDate, startDate);

        var rangeDays = (endDate - startDate).Days + 1;

        // ?? Current period ??
        var dailyStats = await _context.DailyStats
            .Where(d => d.Date >= startDate && d.Date <= endDate)
            .OrderBy(d => d.Date)
            .ToListAsync();

        var todayStat = await _context.DailyStats
            .FirstOrDefaultAsync(d => d.Date == today);

        // ?? Previous period (for trend comparison) ??
        var prevStart = startDate.AddDays(-rangeDays);
        var prevEnd = startDate.AddDays(-1);

        var prevStats = await _context.DailyStats
            .Where(d => d.Date >= prevStart && d.Date <= prevEnd)
            .ToListAsync();

        var currentViews = dailyStats.Sum(d => d.TotalViews);
        var currentVisitors = dailyStats.Sum(d => d.TotalVisitors);
        var prevViews = prevStats.Sum(d => d.TotalViews);
        var prevVisitors = prevStats.Sum(d => d.TotalVisitors);

        // ?? Top pages ??
        var topPages = await _context.PageViews
            .Where(p => p.Date >= startDate && p.Date <= endDate)
            .GroupBy(p => new { p.PageUrl, p.PageTitle })
            .Select(g => new PageSummary
            {
                PageUrl = g.Key.PageUrl,
                PageTitle = g.Key.PageTitle,
                TotalViews = g.Sum(x => x.ViewCount),
                TotalUniqueVisitors = g.Sum(x => x.UniqueVisitors)
            })
            .OrderByDescending(p => p.TotalViews)
            .Take(10)
            .ToListAsync();

        // ?? Page sparklines (top 5) ??
        var top5Urls = topPages.Take(5).Select(p => p.PageUrl).ToList();
        var sparklineData = await _context.PageViews
            .Where(p => p.Date >= startDate && p.Date <= endDate && top5Urls.Contains(p.PageUrl))
            .GroupBy(p => new { p.PageUrl, p.PageTitle, p.Date })
            .Select(g => new { g.Key.PageUrl, g.Key.PageTitle, g.Key.Date, Views = g.Sum(x => x.ViewCount) })
            .ToListAsync();

        var allDates = Enumerable.Range(0, rangeDays)
            .Select(i => startDate.AddDays(i))
            .ToList();

        var sparklines = top5Urls.Select(url =>
        {
            var byDate = sparklineData
                .Where(s => s.PageUrl == url)
                .ToDictionary(s => s.Date, s => s.Views);
            return new PageSparkline
            {
                PageTitle = sparklineData.FirstOrDefault(s => s.PageUrl == url)?.PageTitle ?? url,
                DailyViews = allDates.Select(d => byDate.GetValueOrDefault(d, 0)).ToList()
            };
        }).ToList();

        // ?? Top referrers ??
        var topReferrers = await _context.Referrers
            .Where(r => r.Date >= startDate && r.Date <= endDate)
            .GroupBy(r => r.ReferrerUrl)
            .Select(g => new ReferrerSummary
            {
                ReferrerUrl = g.Key,
                TotalVisits = g.Sum(x => x.VisitCount)
            })
            .OrderByDescending(r => r.TotalVisits)
            .Take(10)
            .ToListAsync();

        // ?? Build view model ??
        var viewModel = new DashboardViewModel
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

        return View(viewModel);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
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

using System.Text.RegularExpressions;
using KokoAnalytics.Data;
using KokoAnalytics.Models;
using Microsoft.EntityFrameworkCore;

namespace KokoAnalytics.Services
{
    public class WordPressImportService
    {
        private readonly AnalyticsDbContext _context;

        public WordPressImportService(AnalyticsDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Takes a single raw SQL dump, auto-detects table names from INSERT statements,
        /// splits them into the correct categories, and imports everything.
        /// Returns a structured result with per-table counts.
        /// </summary>
        public async Task<ImportResultDto> ImportFromRawDumpAsync(string rawSql)
        {
            var result = new ImportResultDto();
            var model = SplitByTableName(rawSql);

            // Warn about missing tables
            if (string.IsNullOrWhiteSpace(model.SiteStatsSql))
                result.Warnings.Add("No site_stats data found — daily visitor summary won't be imported.");
            if (string.IsNullOrWhiteSpace(model.PostStatsSql))
                result.Warnings.Add("No post_stats data found — page view breakdown won't be imported.");
            if (string.IsNullOrWhiteSpace(model.ReferrerUrlsSql))
                result.Warnings.Add("No referrer_urls data found — referrer names may show as \"unknown\".");
            if (string.IsNullOrWhiteSpace(model.ReferrerStatsSql))
                result.Warnings.Add("No referrer_stats data found — referrer traffic won't be imported.");

            // 1. Site stats → DailyStats
            if (!string.IsNullOrWhiteSpace(model.SiteStatsSql))
            {
                var (count, err) = await ImportSiteStatsAsync(model.SiteStatsSql);
                result.SiteStatsCount = count;
                result.TotalRows += count;
                result.Errors.AddRange(err);
            }

            // 2. Post stats → PageViews
            if (!string.IsNullOrWhiteSpace(model.PostStatsSql))
            {
                var (count, err) = await ImportPostStatsAsync(model.PostStatsSql);
                result.PostStatsCount = count;
                result.TotalRows += count;
                result.Errors.AddRange(err);
            }

            // 3. Referrer URLs → lookup
            var referrerLookup = new Dictionary<int, string>();
            if (!string.IsNullOrWhiteSpace(model.ReferrerUrlsSql))
            {
                referrerLookup = ParseReferrerUrls(model.ReferrerUrlsSql);
                result.ReferrerUrlsCount = referrerLookup.Count;
            }

            // 4. Referrer stats → Referrers
            if (!string.IsNullOrWhiteSpace(model.ReferrerStatsSql))
            {
                var (count, err) = await ImportReferrerStatsAsync(model.ReferrerStatsSql, referrerLookup);
                result.ReferrerStatsCount = count;
                result.TotalRows += count;
                result.Errors.AddRange(err);
            }

            result.Success = result.Errors.Count == 0 && result.TotalRows > 0;
            return result;
        }

        /// <summary>
        /// Kept for Advanced Import page compatibility.
        /// </summary>
        public async Task<(int totalRows, List<string> errors)> ImportAllAsync(ImportViewModel model)
        {
            var totalRows = 0;
            var errors = new List<string>();

            if (!string.IsNullOrWhiteSpace(model.SiteStatsSql))
            {
                var (count, err) = await ImportSiteStatsAsync(model.SiteStatsSql);
                totalRows += count;
                errors.AddRange(err);
            }

            if (!string.IsNullOrWhiteSpace(model.PostStatsSql))
            {
                var (count, err) = await ImportPostStatsAsync(model.PostStatsSql);
                totalRows += count;
                errors.AddRange(err);
            }

            var referrerLookup = new Dictionary<int, string>();
            if (!string.IsNullOrWhiteSpace(model.ReferrerUrlsSql))
            {
                referrerLookup = ParseReferrerUrls(model.ReferrerUrlsSql);
            }

            if (!string.IsNullOrWhiteSpace(model.ReferrerStatsSql))
            {
                var (count, err) = await ImportReferrerStatsAsync(model.ReferrerStatsSql, referrerLookup);
                totalRows += count;
                errors.AddRange(err);
            }

            return (totalRows, errors);
        }

        private static ImportViewModel SplitByTableName(string rawSql)
        {
            var model = new ImportViewModel();

            var insertRegex = new Regex(
                @"INSERT\s+INTO\s+[`""]?(\w+)[`""]?\s*(?:\([^)]*\)\s*)?VALUES\s*(.*?);",
                RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

            foreach (Match match in insertRegex.Matches(rawSql))
            {
                var tableName = match.Groups[1].Value.ToLowerInvariant();
                var fullStatement = match.Value;

                if (tableName.Contains("site_stats"))
                    model.SiteStatsSql = Append(model.SiteStatsSql, fullStatement);
                else if (tableName.Contains("post_stats"))
                    model.PostStatsSql = Append(model.PostStatsSql, fullStatement);
                else if (tableName.Contains("referrer_urls"))
                    model.ReferrerUrlsSql = Append(model.ReferrerUrlsSql, fullStatement);
                else if (tableName.Contains("referrer_stats"))
                    model.ReferrerStatsSql = Append(model.ReferrerStatsSql, fullStatement);
            }

            return model;
        }

        private static string Append(string? existing, string newValue)
        {
            return string.IsNullOrWhiteSpace(existing)
                ? newValue
                : existing + "\n" + newValue;
        }

        private async Task<(int count, List<string> errors)> ImportSiteStatsAsync(string sql)
        {
            var errors = new List<string>();
            var rows = ExtractValueTuples(sql);
            var stats = new List<DailyStat>();

            foreach (var row in rows)
            {
                try
                {
                    var fields = ParseFields(row);
                    if (fields.Count < 3) continue;

                    var date = DateTime.Parse(fields[0].Trim('\'', '"'));
                    var visitors = int.Parse(fields[1]);
                    var pageviews = int.Parse(fields[2]);

                    stats.Add(new DailyStat
                    {
                        Date = date,
                        TotalViews = pageviews,
                        TotalVisitors = visitors,
                        BounceRate = visitors > 0
                            ? Math.Round((decimal)(pageviews - visitors) / pageviews * 100, 2)
                            : 0
                    });
                }
                catch (Exception ex)
                {
                    errors.Add($"Site stats: couldn't read row — {ex.Message}");
                }
            }

            var existingDates = await _context.DailyStats
                .Select(d => d.Date)
                .ToHashSetAsync();

            var newStats = stats.Where(s => !existingDates.Contains(s.Date)).ToList();
            var skipped = stats.Count - newStats.Count;
            if (skipped > 0)
                errors.Add($"Site stats: {skipped} date(s) already existed and were skipped.");

            _context.DailyStats.AddRange(newStats);
            await _context.SaveChangesAsync();

            return (newStats.Count, errors);
        }

        private async Task<(int count, List<string> errors)> ImportPostStatsAsync(string sql)
        {
            var errors = new List<string>();
            var rows = ExtractValueTuples(sql);
            var pageViews = new List<PageView>();

            foreach (var row in rows)
            {
                try
                {
                    var fields = ParseFields(row);
                    if (fields.Count < 4) continue;

                    var postId = fields[0].Trim('\'', '"');
                    var date = DateTime.Parse(fields[1].Trim('\'', '"'));
                    var visitors = int.Parse(fields[2]);
                    var pageviewCount = int.Parse(fields[3]);

                    pageViews.Add(new PageView
                    {
                        PageUrl = $"/post/{postId}",
                        PageTitle = $"Post #{postId}",
                        ViewCount = pageviewCount,
                        UniqueVisitors = visitors,
                        Date = date
                    });
                }
                catch (Exception ex)
                {
                    errors.Add($"Post stats: couldn't read row — {ex.Message}");
                }
            }

            _context.PageViews.AddRange(pageViews);
            await _context.SaveChangesAsync();

            return (pageViews.Count, errors);
        }

        private Dictionary<int, string> ParseReferrerUrls(string sql)
        {
            var lookup = new Dictionary<int, string>();
            var rows = ExtractValueTuples(sql);

            foreach (var row in rows)
            {
                try
                {
                    var fields = ParseFields(row);
                    if (fields.Count < 2) continue;

                    var id = int.Parse(fields[0].Trim('\'', '"'));
                    var url = fields[1].Trim('\'', '"');
                    lookup[id] = url;
                }
                catch
                {
                    // Skip malformed rows
                }
            }

            return lookup;
        }

        private async Task<(int count, List<string> errors)> ImportReferrerStatsAsync(
            string sql, Dictionary<int, string> urlLookup)
        {
            var errors = new List<string>();
            var rows = ExtractValueTuples(sql);
            var referrers = new List<Referrer>();

            foreach (var row in rows)
            {
                try
                {
                    var fields = ParseFields(row);
                    if (fields.Count < 4) continue;

                    var date = DateTime.Parse(fields[0].Trim('\'', '"'));
                    var refId = int.Parse(fields[1].Trim('\'', '"'));
                    var visitors = int.Parse(fields[2]);

                    var url = urlLookup.TryGetValue(refId, out var u) ? u : $"unknown-referrer-{refId}";

                    referrers.Add(new Referrer
                    {
                        ReferrerUrl = url,
                        VisitCount = visitors,
                        Date = date
                    });
                }
                catch (Exception ex)
                {
                    errors.Add($"Referrer stats: couldn't read row — {ex.Message}");
                }
            }

            _context.Referrers.AddRange(referrers);
            await _context.SaveChangesAsync();

            return (referrers.Count, errors);
        }

        private static List<string> ExtractValueTuples(string sql)
        {
            var tuples = new List<string>();
            var regex = new Regex(@"\(([^)]+)\)", RegexOptions.Compiled);
            var valuesRegex = new Regex(@"VALUES\s*(.*?);", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

            foreach (Match valuesMatch in valuesRegex.Matches(sql))
            {
                var valuesBlock = valuesMatch.Groups[1].Value;
                foreach (Match tupleMatch in regex.Matches(valuesBlock))
                {
                    tuples.Add(tupleMatch.Groups[1].Value);
                }
            }

            return tuples;
        }

        private static List<string> ParseFields(string tuple)
        {
            var fields = new List<string>();
            var current = "";
            var inQuote = false;
            var quoteChar = '\'';

            foreach (var c in tuple)
            {
                if (inQuote)
                {
                    if (c == quoteChar)
                        inQuote = false;
                    else
                        current += c;
                }
                else if (c == '\'' || c == '"')
                {
                    inQuote = true;
                    quoteChar = c;
                }
                else if (c == ',')
                {
                    fields.Add(current.Trim());
                    current = "";
                }
                else
                {
                    current += c;
                }
            }
            fields.Add(current.Trim());

            return fields;
        }
    }

    public static class QueryableExtensions
    {
        public static async Task<HashSet<T>> ToHashSetAsync<T>(
            this IQueryable<T> source, CancellationToken ct = default)
        {
            var list = await source.ToListAsync(ct);
            return new HashSet<T>(list);
        }
    }
}
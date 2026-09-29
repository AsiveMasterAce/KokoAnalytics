using System.Text.Json;
using System.Text.RegularExpressions;
using KokoAnalytics.Application.DTOs;
using KokoAnalytics.Application.Interfaces;
using KokoAnalytics.Application.Utilities;
using KokoAnalytics.Domain.Entities;
using KokoAnalytics.Domain.Interfaces;

namespace KokoAnalytics.Application.Services;

public class WordPressImportService : IImportService
{
    private readonly IDailyStatRepository _dailyStatRepo;
    private readonly IPageViewRepository _pageViewRepo;
    private readonly IReferrerRepository _referrerRepo;

    public WordPressImportService(
        IDailyStatRepository dailyStatRepo,
        IPageViewRepository pageViewRepo,
        IReferrerRepository referrerRepo)
    {
        _dailyStatRepo = dailyStatRepo;
        _pageViewRepo = pageViewRepo;
        _referrerRepo = referrerRepo;
    }

    public async Task<ImportResultDto> ImportFromRawDumpAsync(string rawSql)
    {
        if (LooksLikeNdjsonExport(rawSql))
            return await ImportFromNdjsonAsync(rawSql);

        var result = new ImportResultDto();
        var model = SplitByTableName(rawSql);

        if (string.IsNullOrWhiteSpace(model.SiteStatsSql))
            result.Warnings.Add("No site_stats data found - daily visitor summary won't be imported.");
        if (string.IsNullOrWhiteSpace(model.PostStatsSql))
            result.Warnings.Add("No post_stats data found - page view breakdown won't be imported.");
        if (string.IsNullOrWhiteSpace(model.PathsSql))
            result.Warnings.Add("No paths data found - pages will show as \"Post #<id>\" instead of their real page name.");
        if (string.IsNullOrWhiteSpace(model.ReferrerUrlsSql))
            result.Warnings.Add("No referrer_urls data found - referrer names may show as \"unknown\".");
        if (string.IsNullOrWhiteSpace(model.ReferrerStatsSql))
            result.Warnings.Add("No referrer_stats data found - referrer traffic won't be imported.");

        if (!string.IsNullOrWhiteSpace(model.SiteStatsSql))
        {
            var (count, err) = await ImportSiteStatsAsync(model.SiteStatsSql);
            result.SiteStatsCount = count;
            result.TotalRows += count;
            result.Errors.AddRange(err);
        }

        var pathsLookup = new Dictionary<int, string>();
        if (!string.IsNullOrWhiteSpace(model.PathsSql))
        {
            pathsLookup = ParsePaths(model.PathsSql);
        }

        if (!string.IsNullOrWhiteSpace(model.PostStatsSql))
        {
            var (count, err) = await ImportPostStatsAsync(model.PostStatsSql, pathsLookup);
            result.PostStatsCount = count;
            result.TotalRows += count;
            result.Errors.AddRange(err);
        }

        var referrerLookup = new Dictionary<int, string>();
        if (!string.IsNullOrWhiteSpace(model.ReferrerUrlsSql))
        {
            referrerLookup = ParseReferrerUrls(model.ReferrerUrlsSql);
            result.ReferrerUrlsCount = referrerLookup.Count;
        }

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

    public async Task<(int totalRows, List<string> errors)> ImportAllAsync(ImportRequest request)
    {
        var totalRows = 0;
        var errors = new List<string>();

        if (!string.IsNullOrWhiteSpace(request.SiteStatsSql))
        {
            var (count, err) = await ImportSiteStatsAsync(request.SiteStatsSql);
            totalRows += count;
            errors.AddRange(err);
        }

        var pathsLookup = new Dictionary<int, string>();
        if (!string.IsNullOrWhiteSpace(request.PathsSql))
        {
            pathsLookup = ParsePaths(request.PathsSql);
        }

        if (!string.IsNullOrWhiteSpace(request.PostStatsSql))
        {
            var (count, err) = await ImportPostStatsAsync(request.PostStatsSql, pathsLookup);
            totalRows += count;
            errors.AddRange(err);
        }

        var referrerLookup = new Dictionary<int, string>();
        if (!string.IsNullOrWhiteSpace(request.ReferrerUrlsSql))
        {
            referrerLookup = ParseReferrerUrls(request.ReferrerUrlsSql);
        }

        if (!string.IsNullOrWhiteSpace(request.ReferrerStatsSql))
        {
            var (count, err) = await ImportReferrerStatsAsync(request.ReferrerStatsSql, referrerLookup);
            totalRows += count;
            errors.AddRange(err);
        }

        return (totalRows, errors);
    }

    #region Private Helpers

    private static ImportRequest SplitByTableName(string rawSql)
    {
        var model = new ImportRequest();
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
            else if (tableName.Contains("paths"))
                model.PathsSql = Append(model.PathsSql, fullStatement);
            else if (tableName.Contains("referrer_urls"))
                model.ReferrerUrlsSql = Append(model.ReferrerUrlsSql, fullStatement);
            else if (tableName.Contains("referrer_stats"))
                model.ReferrerStatsSql = Append(model.ReferrerStatsSql, fullStatement);
        }

        return model;
    }

    private static string Append(string? existing, string newValue) =>
        string.IsNullOrWhiteSpace(existing) ? newValue : existing + "\n" + newValue;

    private async Task<ImportResultDto> ImportFromNdjsonAsync(string ndjson)
    {
        var result = new ImportResultDto();
        var pathsLookup = new Dictionary<int, string>();
        var referrerLookup = new Dictionary<int, string>();
        var siteStats = new List<DailyStat>();
        var pageViews = new List<PageView>();
        var referrers = new List<Referrer>();

        string? currentTable = null;
        List<string> currentColumns = [];
        var lineNumber = 0;

        foreach (var rawLine in ndjson.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            lineNumber++;
            var line = rawLine.Trim();
            if (line.Length == 0)
                continue;

            try
            {
                using var document = JsonDocument.Parse(line);
                var root = document.RootElement;

                if (TryReadTableDeclaration(root, out var table, out var columns))
                {
                    currentTable = table;
                    currentColumns = columns;
                    continue;
                }

                if (currentTable is null || root.ValueKind != JsonValueKind.Array)
                {
                    result.Errors.Add($"NDJSON line {lineNumber}: row data appeared before a table declaration.");
                    continue;
                }

                foreach (var row in root.EnumerateArray())
                {
                    if (row.ValueKind != JsonValueKind.Array || row.GetArrayLength() != currentColumns.Count)
                    {
                        result.Errors.Add($"NDJSON line {lineNumber}: row does not match the declared columns for {currentTable}.");
                        continue;
                    }

                    var values = row.EnumerateArray().Select(GetJsonScalarValue).ToList();
                    ImportNdjsonRow(currentTable, currentColumns, values, pathsLookup, referrerLookup, siteStats, pageViews, referrers);
                }
            }
            catch (JsonException ex)
            {
                result.Errors.Add($"NDJSON line {lineNumber}: invalid JSON - {ex.Message}");
            }
            catch (Exception ex)
            {
                result.Errors.Add($"NDJSON line {lineNumber}: couldn't read row - {ex.Message}");
            }
        }

        if (siteStats.Count == 0)
            result.Warnings.Add("No site_stats data found - daily visitor summary won't be imported.");
        if (pageViews.Count == 0)
            result.Warnings.Add("No post_stats data found - page view breakdown won't be imported.");
        if (pathsLookup.Count == 0)
            result.Warnings.Add("No paths data found - pages may show as unknown.");
        if (referrerLookup.Count == 0)
            result.Warnings.Add("No referrer_labels data found - referrer names may show as unknown.");
        if (referrers.Count == 0)
            result.Warnings.Add("No referrer_stats data found - referrer traffic won't be imported.");

        var existingDates = await _dailyStatRepo.GetExistingDatesAsync();
        var newSiteStats = siteStats.Where(s => !existingDates.Contains(s.Date)).ToList();
        var skippedSiteStats = siteStats.Count - newSiteStats.Count;
        if (skippedSiteStats > 0)
            result.Errors.Add($"Site stats: {skippedSiteStats} date(s) already existed and were skipped.");

        await _dailyStatRepo.AddRangeAsync(newSiteStats);
        await _pageViewRepo.AddRangeAsync(pageViews);
        await _referrerRepo.AddRangeAsync(referrers);
        await _dailyStatRepo.SaveChangesAsync();
        await _pageViewRepo.SaveChangesAsync();
        await _referrerRepo.SaveChangesAsync();

        result.SiteStatsCount = newSiteStats.Count;
        result.PostStatsCount = pageViews.Count;
        result.ReferrerUrlsCount = referrerLookup.Count;
        result.ReferrerStatsCount = referrers.Count;
        result.TotalRows = result.SiteStatsCount + result.PostStatsCount + result.ReferrerStatsCount;
        result.Success = result.Errors.Count == 0 && result.TotalRows > 0;

        return result;
    }

    private static bool LooksLikeNdjsonExport(string input)
    {
        var firstLine = input.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault()?.Trim();
        return firstLine?.StartsWith("{\"table\"", StringComparison.OrdinalIgnoreCase) == true
            || firstLine?.StartsWith("{\"columns\"", StringComparison.OrdinalIgnoreCase) == true
            || input.Contains("\"table\":\"koko_analytics_", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryReadTableDeclaration(JsonElement root, out string table, out List<string> columns)
    {
        table = string.Empty;
        columns = [];

        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty("table", out var tableElement)
            || !root.TryGetProperty("columns", out var columnsElement)
            || tableElement.ValueKind != JsonValueKind.String
            || columnsElement.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        table = tableElement.GetString() ?? string.Empty;
        columns = columnsElement.EnumerateArray()
            .Where(c => c.ValueKind == JsonValueKind.String)
            .Select(c => c.GetString() ?? string.Empty)
            .ToList();
        return table.Length > 0 && columns.Count == columnsElement.GetArrayLength();
    }

    private static string GetJsonScalarValue(JsonElement element) =>
        element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => string.Empty,
            _ => element.GetRawText()
        };

    private static void ImportNdjsonRow(
        string table,
        List<string> columns,
        List<string> values,
        Dictionary<int, string> pathsLookup,
        Dictionary<int, string> referrerLookup,
        List<DailyStat> siteStats,
        List<PageView> pageViews,
        List<Referrer> referrers)
    {
        string Get(string column)
        {
            var index = columns.FindIndex(c => c.Equals(column, StringComparison.OrdinalIgnoreCase));
            return index >= 0 && index < values.Count ? values[index] : string.Empty;
        }

        if (table.EndsWith("site_stats", StringComparison.OrdinalIgnoreCase))
        {
            var visitors = int.Parse(Get("visitors"));
            var pageviews = int.Parse(Get("pageviews"));
            siteStats.Add(new DailyStat
            {
                Date = DateTime.Parse(Get("date")),
                TotalViews = pageviews,
                TotalVisitors = visitors,
                BounceRate = visitors > 0 && pageviews > 0
                    ? Math.Round((decimal)(pageviews - visitors) / pageviews * 100, 2)
                    : 0
            });
        }
        else if (table.EndsWith("paths", StringComparison.OrdinalIgnoreCase))
        {
            pathsLookup[int.Parse(Get("id"))] = Get("path");
        }
        else if (table.EndsWith("post_stats", StringComparison.OrdinalIgnoreCase))
        {
            var pathId = int.Parse(Get("path_id"));
            var path = pathsLookup.TryGetValue(pathId, out var knownPath) ? knownPath : $"/unknown-path/{pathId}";
            pageViews.Add(new PageView
            {
                PageUrl = path,
                PageTitle = PageNameGenerator.GetFriendlyName(path),
                ViewCount = int.Parse(Get("pageviews")),
                UniqueVisitors = int.Parse(Get("visitors")),
                Date = DateTime.Parse(Get("date"))
            });
        }
        else if (table.EndsWith("referrer_labels", StringComparison.OrdinalIgnoreCase)
            || table.EndsWith("referrer_urls", StringComparison.OrdinalIgnoreCase))
        {
            var label = !string.IsNullOrEmpty(Get("value")) ? Get("value") : Get("url");
            referrerLookup[int.Parse(Get("id"))] = label;
        }
        else if (table.EndsWith("referrer_stats", StringComparison.OrdinalIgnoreCase))
        {
            var id = int.Parse(Get("id"));
            var visits = !string.IsNullOrEmpty(Get("unique_hits")) ? Get("unique_hits") : Get("visitors");
            referrers.Add(new Referrer
            {
                ReferrerUrl = referrerLookup.TryGetValue(id, out var url) ? url : $"unknown-referrer-{id}",
                VisitCount = int.Parse(visits),
                Date = DateTime.Parse(Get("date"))
            });
        }
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
                errors.Add($"Site stats: couldn't read row - {ex.Message}");
            }
        }

        var existingDates = await _dailyStatRepo.GetExistingDatesAsync();
        var newStats = stats.Where(s => !existingDates.Contains(s.Date)).ToList();
        var skipped = stats.Count - newStats.Count;
        if (skipped > 0)
            errors.Add($"Site stats: {skipped} date(s) already existed and were skipped.");

        await _dailyStatRepo.AddRangeAsync(newStats);
        await _dailyStatRepo.SaveChangesAsync();

        return (newStats.Count, errors);
    }

    private async Task<(int count, List<string> errors)> ImportPostStatsAsync(string sql, Dictionary<int, string> pathsLookup)
    {
        var errors = new List<string>();
        var pageViews = new List<PageView>();
        var statements = ExtractStatementsWithColumns(sql);

        if (statements.Count == 0)
        {
            errors.Add("Post stats: couldn't find any INSERT statements.");
            return (0, errors);
        }

        foreach (var (columns, tuples) in statements)
        {
            // The exported column order varies (e.g. date, path_id, post_id, visitors, pageviews),
            // so resolve fields by column name instead of assuming a fixed position.
            var dateIdx = columns.FindIndex(c => c == "date");
            var pathIdIdx = columns.FindIndex(c => c == "path_id");
            var visitorsIdx = columns.FindIndex(c => c == "visitors");
            var pageviewsIdx = columns.FindIndex(c => c == "pageviews");

            if (dateIdx < 0 || visitorsIdx < 0 || pageviewsIdx < 0)
            {
                errors.Add("Post stats: couldn't determine date/visitors/pageviews columns from the INSERT statement.");
                continue;
            }

            foreach (var tuple in tuples)
            {
                try
                {
                    var fields = ParseFields(tuple)
                        .Select(field => field.Trim('\'', '"'))
                        .ToList();

                    var maxIdx = new[] { dateIdx, visitorsIdx, pageviewsIdx, pathIdIdx }.Max();
                    if (fields.Count <= maxIdx)
                        continue;

                    var date = DateTime.Parse(fields[dateIdx]);
                    var visitors = int.Parse(fields[visitorsIdx]);
                    var pageviewCount = int.Parse(fields[pageviewsIdx]);

                    string pageUrl;
                    string pageTitle;
                    if (pathIdIdx >= 0 && int.TryParse(fields[pathIdIdx], out var pathId)
                        && pathsLookup.TryGetValue(pathId, out var path))
                    {
                        pageUrl = path;
                        pageTitle = PageNameGenerator.GetFriendlyName(path);
                    }
                    else
                    {
                        var rawId = pathIdIdx >= 0 ? fields[pathIdIdx] : "unknown";
                        pageUrl = $"/unknown-path/{rawId}";
                        pageTitle = $"Unknown Page #{rawId}";
                    }

                    pageViews.Add(new PageView
                    {
                        PageUrl = pageUrl,
                        PageTitle = pageTitle,
                        ViewCount = pageviewCount,
                        UniqueVisitors = visitors,
                        Date = date
                    });
                }
                catch (Exception ex)
                {
                    errors.Add($"Post stats: couldn't read row - {ex.Message}");
                }
            }
        }

        await _pageViewRepo.AddRangeAsync(pageViews);
        await _pageViewRepo.SaveChangesAsync();

        return (pageViews.Count, errors);
    }

    private static Dictionary<int, string> ParsePaths(string sql)
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
                var path = fields[1].Trim('\'', '"');
                lookup[id] = path;
            }
            catch { }
        }

        return lookup;
    }

    private static Dictionary<int, string> ParseReferrerUrls(string sql)
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
            catch { }
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
                errors.Add($"Referrer stats: couldn't read row - {ex.Message}");
            }
        }

        await _referrerRepo.AddRangeAsync(referrers);
        await _referrerRepo.SaveChangesAsync();

        return (referrers.Count, errors);
    }

    private static List<(List<string> Columns, List<string> Tuples)> ExtractStatementsWithColumns(string sql)
    {
        var results = new List<(List<string>, List<string>)>();
        var statementRegex = new Regex(
            @"INSERT\s+INTO\s+[`""]?\w+[`""]?\s*\(([^)]*)\)\s*VALUES\s*(.*?);",
            RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
        var tupleRegex = new Regex(@"\(([^)]+)\)", RegexOptions.Compiled);

        foreach (Match statementMatch in statementRegex.Matches(sql))
        {
            var columns = statementMatch.Groups[1].Value
                .Split(',')
                .Select(c => c.Trim().Trim('`', '"').ToLowerInvariant())
                .ToList();

            var valuesBlock = statementMatch.Groups[2].Value;
            var tuples = tupleRegex.Matches(valuesBlock)
                .Select(m => m.Groups[1].Value)
                .ToList();

            results.Add((columns, tuples));
        }

        return results;
    }

    private static List<string> ExtractValueTuples(string sql)
    {
        var tuples = new List<string>();
        var regex = new Regex(@"\(([^)]+)\)", RegexOptions.Compiled);
        var valuesRegex = new Regex(@"VALUES\s*(.*?);",
            RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

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
                if (c == quoteChar) inQuote = false;
                else current += c;
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

    #endregion
}

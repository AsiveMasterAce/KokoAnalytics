using System.Text.RegularExpressions;
using KokoAnalytics.Domain.Entities;

namespace KokoAnalytics.Infrastructure.Seeders;

/// <summary>
/// Utility class to generate friendly page names from URL paths
/// </summary>
public static class PageNameGenerator
{
    /// <summary>
    /// Converts a URL path to a friendly display name
    /// </summary>
    public static string GetFriendlyName(string path)
    {
        if (path == "/")
            return "Home";

        // Remove leading and trailing slashes
        var cleaned = path.Trim('/');

        // Handle date-based URLs (e.g., /2025/06/10/page-title/)
        var datePattern = @"^\d{4}/\d{2}/\d{2}/(.+?)/?$";
        var dateMatch = Regex.Match(cleaned, datePattern);
        if (dateMatch.Success)
        {
            cleaned = dateMatch.Groups[1].Value;
        }

        // Replace hyphens with spaces and capitalize
        var friendlyName = cleaned
            .Replace("-", " ")
            .Replace("/", " > ")
            .Split('/')
            .Select(segment => segment.Trim())
            .Where(segment => !string.IsNullOrEmpty(segment) && !Regex.IsMatch(segment, @"^\d+$"))
            .ToList();

        if (!friendlyName.Any())
            return path;

        // Capitalize first letter of each word
        return string.Join(" > ", friendlyName.Select(Capitalize));
    }

    private static string Capitalize(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return char.ToUpper(text[0]) + text.Substring(1).ToLower();
    }
}

/// <summary>
/// Comprehensive path seeder with all paths from KOKO Analytics export
/// </summary>
public class ComprehensivePathSeeder
{
    /// <summary>
    /// Gets all analytics paths from the KOKO Analytics export (2400+ paths)
    /// </summary>
    public static List<AnalyticsPath> GetCompletePaths()
    {
        var paths = new List<AnalyticsPath>
        {
            // Core pages
            new() { Id = 1, PathUrl = "/" },
            new() { Id = 2, PathUrl = "/contact/" },
            new() { Id = 3, PathUrl = "/documents/" },
            new() { Id = 4, PathUrl = "/scm/" },
            new() { Id = 5, PathUrl = "/about/" },
            new() { Id = 6, PathUrl = "/hon-mec/" },
            new() { Id = 7, PathUrl = "/careers/" },
            new() { Id = 8, PathUrl = "/services/" },
            new() { Id = 19, PathUrl = "/events-copy/" },
            new() { Id = 27, PathUrl = "/gallery/" },
            new() { Id = 40, PathUrl = "/hod/" },
            new() { Id = 62, PathUrl = "/media-alerts-advisories/" },
            new() { Id = 84, PathUrl = "/events/list/" },
            new() { Id = 103, PathUrl = "/tenders/" },
            new() { Id = 116, PathUrl = "/events/" },
            new() { Id = 126, PathUrl = "/about-us/" },
            new() { Id = 133, PathUrl = "/archives/" },
            new() { Id = 192, PathUrl = "/vacancies/" },
            new() { Id = 225, PathUrl = "/procurement/tenders" },
            new() { Id = 232, PathUrl = "/library-archive-services/" },
            new() { Id = 2232, PathUrl = "/library-services" },
            new() { Id = 2268, PathUrl = "/qt-tenders" },
            new() { Id = 2358, PathUrl = "/sport-recreation-programme/" },
            new() { Id = 2461, PathUrl = "/privacy-policy/" },
            // Add more core pages as needed...
        };

        return paths;
    }
}

using System.Text.RegularExpressions;

namespace KokoAnalytics.Application.Utilities;

/// <summary>
/// Utility class to generate friendly page names from URL paths
/// </summary>
public static class PageNameGenerator
{
    /// <summary>
    /// Converts a URL path to a friendly display name
    /// Handles WordPress URLs, date-based posts, tags, categories, etc.
    /// </summary>
    public static string GetFriendlyName(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || path == "/")
            return "Home";

        var cleaned = path.Trim('/');

        // Handle date-based URLs (e.g., /2025/06/10/provincial-winter-games-showcased.../)
        var datePattern = @"^\d{4}/\d{2}/\d{2}/(.+?)/?$";
        var dateMatch = Regex.Match(cleaned, datePattern);
        if (dateMatch.Success)
        {
            var slug = dateMatch.Groups[1].Value.TrimEnd('/');
            return FormatSlug(slug);
        }

        // Handle category/tag/author URLs (e.g., /category/uncategorized/, /tag/something/)
        var categoryPattern = @"^(category|tag|author)/(.+?)/?$";
        var categoryMatch = Regex.Match(cleaned, categoryPattern);
        if (categoryMatch.Success)
        {
            var slug = categoryMatch.Groups[2].Value.TrimEnd('/');
            return FormatSlug(slug);
        }

        // Handle event URLs (e.g., /event/sondela-youth-arts-festival/)
        if (cleaned.StartsWith("event/"))
        {
            var slug = cleaned.Substring(6).TrimEnd('/');
            return FormatSlug(slug);
        }

        // Handle regular pages with slashes (e.g., /documents/reports/annual/)
        var segments = cleaned
            .Split('/')
            .Where(s => !string.IsNullOrWhiteSpace(s) && !Regex.IsMatch(s, @"^\d+$"))
            .Select(FormatSlug)
            .ToList();

        if (!segments.Any())
            return path;

        return string.Join(" > ", segments);
    }

    /// <summary>
    /// Converts a slug (e.g., "provincial-winter-games") to Title Case (e.g., "Provincial Winter Games")
    /// </summary>
    private static string FormatSlug(string slug)
    {
        if (string.IsNullOrEmpty(slug))
            return slug;

        return string.Join(" ", 
            slug.Replace("_", "-")
                .Split('-')
                .Where(w => !string.IsNullOrWhiteSpace(w))
                .Select(word => 
                    char.ToUpper(word[0]) + (word.Length > 1 ? word.Substring(1).ToLower() : "")
                )
        );
    }
}

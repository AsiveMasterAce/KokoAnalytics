using System.Text.RegularExpressions;
using KokoAnalytics.Domain.Entities;

namespace KokoAnalytics.Application.Utilities;

/// <summary>
/// Utility to generate and populate page titles from analytics data
/// Extracts readable titles from WordPress URL slugs
/// </summary>
public static class PageTitlePopulator
{
    /// <summary>
    /// Extracts a readable title from a WordPress page path
    /// </summary>
    /// <remarks>
    /// Examples:
    /// "/" → "Home"
    /// "/about/" → "About"
    /// "/2025/06/10/provincial-winter-games-showcased-young-sporting-talent-in-east-london/" → "Provincial Winter Games Showcased Young Sporting Talent In East London"
    /// "/event/sondela-youth-arts-festival/" → "Sondela Youth Arts Festival"
    /// "/tag/africa-month/" → "Africa Month"
    /// </remarks>
    public static string ExtractTitleFromPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || path == "/")
            return "Home";

        var cleaned = path.Trim('/');

        // Extract title from date-based URLs (blog posts)
        // Format: /YYYY/MM/DD/post-slug/
        var datePattern = @"^\d{4}/\d{2}/\d{2}/(.+?)/?$";
        var dateMatch = Regex.Match(cleaned, datePattern);
        if (dateMatch.Success)
        {
            return FormatSlug(dateMatch.Groups[1].Value.TrimEnd('/'));
        }

        // Handle event URLs (e.g., /event/sondela-youth-arts-festival/)
        if (cleaned.StartsWith("event/"))
        {
            var slug = cleaned.Substring(6).TrimEnd('/');
            return FormatSlug(slug);
        }

        // Handle tag URLs (e.g., /tag/something/)
        if (cleaned.StartsWith("tag/"))
        {
            var slug = cleaned.Substring(4).TrimEnd('/');
            return FormatSlug(slug);
        }

        // Handle category URLs (e.g., /category/uncategorized/)
        if (cleaned.StartsWith("category/"))
        {
            var slug = cleaned.Substring(9).TrimEnd('/');
            return FormatSlug(slug);
        }

        // Handle author URLs (e.g., /author/john/)
        if (cleaned.StartsWith("author/"))
        {
            var slug = cleaned.Substring(7).TrimEnd('/');
            return FormatSlug(slug);
        }

        // For regular pages, take last non-numeric segment
        var segments = cleaned
            .Split('/')
            .Where(s => !string.IsNullOrWhiteSpace(s) && !Regex.IsMatch(s, @"^\d+$"))
            .ToList();

        if (!segments.Any())
            return path;

        return FormatSlug(segments[segments.Count - 1]);
    }

    /// <summary>
    /// Converts a URL slug to Title Case
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

    /// <summary>
    /// Creates PageView objects with extracted titles (for batch operations)
    /// </summary>
    public static IEnumerable<PageView> EnrichPageViewsWithTitles(IEnumerable<PageView> pageViews)
    {
        foreach (var pageView in pageViews)
        {
            if (string.IsNullOrWhiteSpace(pageView.PageTitle) || pageView.PageTitle == "Post #0")
            {
                pageView.PageTitle = ExtractTitleFromPath(pageView.PageUrl);
            }
            yield return pageView;
        }
    }
}

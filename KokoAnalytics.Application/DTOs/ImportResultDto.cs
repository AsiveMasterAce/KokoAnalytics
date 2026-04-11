namespace KokoAnalytics.Application.DTOs;

public class ImportResultDto
{
    public bool Success { get; set; }
    public int TotalRows { get; set; }
    public int SiteStatsCount { get; set; }
    public int PostStatsCount { get; set; }
    public int ReferrerUrlsCount { get; set; }
    public int ReferrerStatsCount { get; set; }
    public List<string> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}
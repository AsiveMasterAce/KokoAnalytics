namespace KokoAnalytics.Application.DTOs;

public class ImportRequest
{
    public string? SiteStatsSql { get; set; }
    public string? PostStatsSql { get; set; }
    public string? ReferrerUrlsSql { get; set; }
    public string? ReferrerStatsSql { get; set; }
}
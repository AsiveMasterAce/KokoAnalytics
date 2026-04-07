namespace KokoAnalytics.Models
{
    public class ImportViewModel
    {
        // Existing per-table fields (kept for the advanced Import page)
        public string? SiteStatsSql { get; set; }
        public string? PostStatsSql { get; set; }
        public string? ReferrerUrlsSql { get; set; }
        public string? ReferrerStatsSql { get; set; }

        // Single paste box for Easy Import
        public string? RawSqlDump { get; set; }

        public string? ResultMessage { get; set; }
        public bool IsSuccess { get; set; }
    }

    /// <summary>
    /// JSON response returned by the Easy Import AJAX endpoint.
    /// </summary>
    public class ImportResultDto
    {
        public bool Success { get; set; }
        public int TotalRows { get; set; }
        public int SiteStatsCount { get; set; }
        public int PostStatsCount { get; set; }
        public int ReferrerUrlsCount { get; set; }
        public int ReferrerStatsCount { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }
}
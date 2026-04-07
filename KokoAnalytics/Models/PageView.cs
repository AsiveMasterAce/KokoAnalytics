namespace KokoAnalytics.Models
{
    public class PageView
    {
        public int Id { get; set; }
        public string PageUrl { get; set; } = string.Empty;
        public string PageTitle { get; set; } = string.Empty;
        public int ViewCount { get; set; }
        public int UniqueVisitors { get; set; }
        public DateTime Date { get; set; }
    }
}
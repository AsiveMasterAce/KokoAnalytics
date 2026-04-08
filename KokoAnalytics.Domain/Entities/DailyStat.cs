namespace KokoAnalytics.Domain.Entities;

public class DailyStat
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public int TotalViews { get; set; }
    public int TotalVisitors { get; set; }
    public decimal BounceRate { get; set; }
}
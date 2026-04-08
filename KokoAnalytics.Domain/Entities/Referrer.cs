namespace KokoAnalytics.Domain.Entities;

public class Referrer
{
    public int Id { get; set; }
    public string ReferrerUrl { get; set; } = string.Empty;
    public int VisitCount { get; set; }
    public DateTime Date { get; set; }
}
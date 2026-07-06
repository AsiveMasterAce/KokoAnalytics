using KokoAnalytics.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KokoAnalytics.Infrastructure.Data;

public class AnalyticsDbContext : DbContext
{
    public AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options)
        : base(options)
    {
    }

    public DbSet<PageView> PageViews { get; set; }
    public DbSet<Referrer> Referrers { get; set; }
    public DbSet<DailyStat> DailyStats { get; set; }
    public DbSet<AnalyticsPath> AnalyticsPaths { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DailyStat>().ToTable("DailyStats");
        modelBuilder.Entity<PageView>().ToTable("PageViews");
        modelBuilder.Entity<Referrer>().ToTable("Referrers");
        modelBuilder.Entity<AnalyticsPath>().ToTable("Paths");

        modelBuilder.Entity<DailyStat>()
            .Property(d => d.BounceRate)
            .HasPrecision(18, 2);

        modelBuilder.Entity<DailyStat>()
            .HasIndex(d => d.Date)
            .IsUnique();

        modelBuilder.Entity<AnalyticsPath>()
            .HasIndex(p => p.PathUrl)
            .IsUnique();
    }
}
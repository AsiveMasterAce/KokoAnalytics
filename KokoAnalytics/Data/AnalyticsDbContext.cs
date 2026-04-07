using KokoAnalytics.Models;
using Microsoft.EntityFrameworkCore;

namespace KokoAnalytics.Data
{
    public class AnalyticsDbContext : DbContext
    {
        public AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options)
            : base(options)
        {
        }

        public DbSet<PageView> PageViews { get; set; }
        public DbSet<Referrer> Referrers { get; set; }
        public DbSet<DailyStat> DailyStats { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DailyStat>()
                .HasIndex(d => d.Date)
                .IsUnique();
        }
    }
}
using KokoAnalytics.Domain.Interfaces;
using KokoAnalytics.Infrastructure.Data;
using KokoAnalytics.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
namespace KokoAnalytics.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AnalyticsDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IDailyStatRepository, DailyStatRepository>();
        services.AddScoped<IPageViewRepository, PageViewRepository>();
        services.AddScoped<IReferrerRepository, ReferrerRepository>();

        return services;
    }
}
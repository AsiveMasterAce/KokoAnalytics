using KokoAnalytics.Application.Interfaces;
using KokoAnalytics.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace KokoAnalytics.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IImportService, WordPressImportService>();
        return services;
    }
}
# Quick Start: Using the Path Mapping System

## Status
✅ Implementation complete and tested
✅ Database migration applied
✅ Project builds successfully

## Steps to Populate Path Data

### Option 1: Quick Seed (Sample Paths)
Add this to `Program.cs` after building the app:

```csharp
using (var scope = app.Services.CreateScope())
{
    var pathRepo = scope.ServiceProvider.GetRequiredService<IPathRepository>();
    
    // Check if paths are already seeded
    var existingPaths = await pathRepo.GetAllAsync();
    if (!existingPaths.Any())
    {
        var seeder = new PathSeederService(pathRepo);
        await seeder.SeedPathsAsync();
    }
}
```

### Option 2: Import from SQL File
Run the SQL seeder directly against your database:

```sql
-- Copy contents from: KokoAnalytics.Infrastructure\Seeders\PathSeeder.sql
-- Then execute in SQL Server Management Studio
```

### Option 3: Bulk Import (Recommended)
Use the user's complete SQL export (2400+ paths):

```csharp
// In PathSeederService.cs, replace GetAnalyticsPathData() with the full path list from the SQL export
// The structure is already there - just extend the List<AnalyticsPath>
```

## Using Friendly Names in Dashboard

### Update DashboardService

Add path repository injection:

```csharp
public class DashboardService : IDashboardService
{
    private readonly IPathRepository _pathRepository;

    public DashboardService(
        IDailyStatRepository dailyStatRepo,
        IPageViewRepository pageViewRepo,
        IReferrerRepository referrerRepo,
        IPathRepository pathRepository)
    {
        _pathRepository = pathRepository;
        // ... existing code ...
    }

    // In GetDashboardAsync, update the topPages mapping:
    var topPages = pageViews
        .GroupBy(p => new { p.PageUrl, p.PageTitle })
        .Select(g => new PageSummaryDto
        {
            PageUrl = g.Key.PageUrl,
            PageTitle = g.Key.PageTitle ?? PageNameGenerator.GetFriendlyName(g.Key.PageUrl),
            TotalViews = g.Sum(x => x.ViewCount),
            TotalUniqueVisitors = g.Sum(x => x.UniqueVisitors)
        })
        .OrderByDescending(p => p.TotalViews)
        .Take(10)
        .ToList();
}
```

### Update Views

In your dashboard Razor views:

```html
@foreach (var page in Model.TopPages)
{
    <tr>
        <td>@page.PageTitle</td>
        <td>@page.TotalViews</td>
        <td>@page.TotalUniqueVisitors</td>
    </tr>
}
```

## Example: Path to Friendly Name Conversion

| Path | Friendly Name |
|------|---------------|
| `/` | Home |
| `/careers/` | Careers |
| `/about/` | About |
| `/contact/` | Contact |
| `/events/tag/africa-month/list/` | Events > Tag > Africa Month > List |
| `/2025/06/10/provincial-winter-games-showcased/` | Provincial Winter Games Showcased |

## API Reference

### PageNameGenerator
```csharp
// Convert path to friendly name
string friendlyName = PageNameGenerator.GetFriendlyName("/careers/");
// Output: "Careers"
```

### IPathRepository
```csharp
// Get a specific path
var path = await _pathRepository.GetByUrlAsync("/careers/");

// Get all paths
var allPaths = await _pathRepository.GetAllAsync();

// Add new path
await _pathRepository.AddAsync(new AnalyticsPath { PathUrl = "/news/" });

// Bulk add paths
var paths = new List<AnalyticsPath> { /* ... */ };
await _pathRepository.AddRangeAsync(paths);
```

## Testing

Test the friendly name generator:

```csharp
// In a test or console app
var tests = new[]
{
    "/",
    "/careers/",
    "/about/",
    "/events/tag/africa-month/list/",
    "/2025/06/10/provincial-winter-games/",
    "/Pages/default.aspx"
};

foreach (var path in tests)
{
    var friendly = PageNameGenerator.GetFriendlyName(path);
    Console.WriteLine($"{path} → {friendly}");
}
```

## Troubleshooting

### Error: "AnalyticsPath not found"
- Ensure database migration ran: `dotnet ef database update`
- Check Paths table exists in database

### Paths not showing friendly names
- Ensure `PathRepository` is registered in DependencyInjection
- Verify paths are seeded in database
- Check that PageTitle falls back to `PageNameGenerator` in DashboardService

### Build errors
- Clean solution: `dotnet clean`
- Rebuild: `dotnet build`
- Check that `System.IO` is not being used ambiguously with `AnalyticsPath`

## Files to Review

1. [PATH_MAPPING_IMPLEMENTATION.md](./PATH_MAPPING_IMPLEMENTATION.md) - Complete implementation details
2. `KokoAnalytics.Domain/Entities/Path.cs` - AnalyticsPath entity
3. `KokoAnalytics.Infrastructure/Seeders/PageNameUtilities.cs` - Name generation logic
4. `KokoAnalytics.Infrastructure/Migrations/20260706090114_AddAnalyticsPathsTable.cs` - Database schema

## Next Steps

1. ✅ Seed the 2400+ paths from your SQL export
2. ⏳ Update DashboardService to use friendly names
3. ⏳ Update dashboard views to display friendly page names
4. ⏳ Test dashboard with sample data
5. ⏳ (Optional) Create admin UI to manage path mappings

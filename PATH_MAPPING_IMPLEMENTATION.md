# KokoAnalytics Path Mapping Implementation

## Overview
This implementation adds a path name mapping system to the KokoAnalytics application, allowing the analytics dashboard to display human-readable page names instead of technical paths or IDs.

## Changes Made

### 1. **New Entity: AnalyticsPath**
- **File**: `KokoAnalytics.Domain/Entities/Path.cs`
- **Purpose**: Stores URL paths from the KOKO Analytics WordPress plugin
- **Table**: `Paths`
- **Fields**:
  - `Id` (int): Primary key
  - `PathUrl` (nvarchar): The URL path (unique index)

### 2. **Database Integration**
- **Migration**: `20260706090114_AddAnalyticsPathsTable`
- **DbContext Update**: `AnalyticsDbContext.cs` now includes `DbSet<AnalyticsPath> AnalyticsPaths`
- **Database Table**: Created `Paths` table with:
  - Primary key on `Id`
  - Unique index on `PathUrl`

### 3. **Data Access Layer**
- **Interface**: `IPathRepository` in `KokoAnalytics.Domain/Interfaces/`
  - Methods:
    - `GetByUrlAsync(string url)`: Get a specific path
    - `GetAllAsync()`: Retrieve all paths
    - `AddAsync(AnalyticsPath path)`: Add a single path
    - `AddRangeAsync(IEnumerable<AnalyticsPath> paths)`: Bulk insert paths
    - `SaveChangesAsync()`: Save changes to database

- **Implementation**: `PathRepository` in `KokoAnalytics.Infrastructure/Repositories/`

### 4. **Seeding Services**
- **PathSeederService** (`KokoAnalytics.Infrastructure/Seeders/PathSeederService.cs`)
  - Provides `SeedPathsAsync()` method
  - Prevents duplicate seeding
  - Can be called from startup or migration seed

- **PageNameGenerator** (`KokoAnalytics.Infrastructure/Seeders/PageNameUtilities.cs`)
  - Converts URL paths to friendly display names
  - Handles date-based URLs (e.g., `/2025/06/10/page-title/`)
  - Removes hyphens, capitalizes words
  - Examples:
    - `/` → "Home"
    - `/careers/` → "Careers"
    - `/about/` → "About"
    - `/events/tag/africa-month/list/` → "Events > Tag > Africa Month > List"

### 5. **Dependency Injection**
- Updated `KokoAnalytics.Infrastructure/DependencyInjection.cs`
- Registered `IPathRepository` → `PathRepository`

## Database Schema

```sql
CREATE TABLE [Paths] (
    [Id] int NOT NULL IDENTITY,
    [PathUrl] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_Paths] PRIMARY KEY ([Id]),
    UNIQUE INDEX [IX_Paths_PathUrl] ON [PathUrl]
);
```

## Usage

### 1. **Initialize Database**
The migration has been applied. The `Paths` table is ready for data.

### 2. **Seed Path Data**
```csharp
// In your startup code or Program.cs
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<PathSeederService>();
    await seeder.SeedPathsAsync();
}
```

### 3. **Query Paths in Services**
```csharp
public class DashboardService
{
    private readonly IPathRepository _pathRepository;

    public DashboardService(IPathRepository pathRepository)
    {
        _pathRepository = pathRepository;
    }

    public async Task<string> GetPageNameAsync(string pathUrl)
    {
        var path = await _pathRepository.GetByUrlAsync(pathUrl);
        if (path != null)
        {
            return PageNameGenerator.GetFriendlyName(path.PathUrl);
        }
        return pageTitle; // Fallback to existing PageTitle
    }
}
```

### 4. **Display in Views**
```html
<table>
  <tr>
    <th>Page</th>
    <th>Views</th>
    <th>Visitors</th>
  </tr>
  @foreach (var page in Model.TopPages)
  {
    <tr>
      <td>@PageNameGenerator.GetFriendlyName(page.PageUrl)</td>
      <td>@page.TotalViews</td>
      <td>@page.TotalUniqueVisitors</td>
    </tr>
  }
</table>
```

## Next Steps

1. **Import Full Path Data**
   - The SQL file contains 2400+ paths from your export
   - Extend `ComprehensivePathSeeder.GetCompletePaths()` with all paths
   - Or create a utility to parse and import directly from the SQL file

2. **Update DashboardService**
   - Inject `IPathRepository`
   - Use `PageNameGenerator.GetFriendlyName()` for display
   - Update dashboard views to show friendly names

3. **Create Admin UI (Optional)**
   - Add page to manage path-to-name mappings
   - Allow manual customization of friendly names
   - Add custom display name column to AnalyticsPath if needed

4. **Performance Optimization**
   - Consider caching the paths dictionary in memory
   - Use `IDistributedCache` if running multiple instances

## File Structure
```
KokoAnalytics.Domain/
├── Entities/
│   └── Path.cs (AnalyticsPath class)
├── Interfaces/
│   └── IPathRepository.cs

KokoAnalytics.Infrastructure/
├── Data/
│   └── AnalyticsDbContext.cs (updated)
├── Repositories/
│   └── PathRepository.cs
├── Seeders/
│   ├── PathSeeder.sql
│   ├── PathSeederService.cs
│   └── PageNameUtilities.cs
├── Migrations/
│   └── 20260706090114_AddAnalyticsPathsTable.cs
└── DependencyInjection.cs (updated)
```

## Notes
- The `Path` class was renamed to `AnalyticsPath` to avoid conflicts with `System.IO.Path`
- The `PathUrl` field has a unique constraint to prevent duplicate paths
- The `PageNameGenerator` is currently static but can be converted to a service if needed
- The implementation is backward compatible with existing code

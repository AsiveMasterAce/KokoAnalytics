using KokoAnalytics.Application.DTOs;

namespace KokoAnalytics.Application.Interfaces;

public interface IImportService
{
    Task<ImportResultDto> ImportFromRawDumpAsync(string rawSql);
    Task<(int totalRows, List<string> errors)> ImportAllAsync(ImportRequest request);
}
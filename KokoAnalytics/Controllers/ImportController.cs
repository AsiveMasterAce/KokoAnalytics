using KokoAnalytics.Application.DTOs;
using KokoAnalytics.Application.Interfaces;
using KokoAnalytics.Models;
using Microsoft.AspNetCore.Mvc;

namespace KokoAnalytics.Controllers;

public class ImportController : Controller
{
    private readonly IImportService _importService;

    public ImportController(IImportService importService)
    {
        _importService = importService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View(new ImportViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ImportViewModel model)
    {
        try
        {
            var request = new ImportRequest
            {
                SiteStatsSql = model.SiteStatsSql,
                PostStatsSql = model.PostStatsSql,
                ReferrerUrlsSql = model.ReferrerUrlsSql,
                ReferrerStatsSql = model.ReferrerStatsSql
            };

            var (totalRows, errors) = await _importService.ImportAllAsync(request);

            model.IsSuccess = errors.Count == 0;
            model.ResultMessage = errors.Count == 0
                ? $"✅ Successfully imported {totalRows} rows."
                : $"⚠️ Imported {totalRows} rows with {errors.Count} error(s):\n" +
                  string.Join("\n", errors.Take(10));
        }
        catch (Exception ex)
        {
            model.IsSuccess = false;
            model.ResultMessage = $"❌ Import failed: {ex.Message}";
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult Easy()
    {
        return View(new ImportViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EasyImportApi([FromBody] EasyImportRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.RawSqlDump))
        {
            return Json(new Application.DTOs.ImportResultDto
            {
                Success = false,
                Errors = ["No SQL data was provided."]
            });
        }

        try
        {
            var result = await _importService.ImportFromRawDumpAsync(request.RawSqlDump);
            return Json(result);
        }
        catch (Exception ex)
        {
            return Json(new Application.DTOs.ImportResultDto
            {
                Success = false,
                Errors = [$"Import failed: {ex.Message}"]
            });
        }
    }
}

public class EasyImportRequest
{
    public string? RawSqlDump { get; set; }
}
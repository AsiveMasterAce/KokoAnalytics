using KokoAnalytics.Models;
using KokoAnalytics.Services;
using Microsoft.AspNetCore.Mvc;

namespace KokoAnalytics.Controllers
{
    public class ImportController : Controller
    {
        private readonly WordPressImportService _importService;

        public ImportController(WordPressImportService importService)
        {
            _importService = importService;
        }

        // ---- Advanced Import (4 separate boxes) ----
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
                var (totalRows, errors) = await _importService.ImportAllAsync(model);

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

        // ---- Easy Import (page) ----
        [HttpGet]
        public IActionResult Easy()
        {
            return View(new ImportViewModel());
        }

        // ---- Easy Import AJAX endpoint ----
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EasyImportApi([FromBody] EasyImportRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.RawSqlDump))
            {
                return Json(new ImportResultDto
                {
                    Success = false,
                    Errors = new List<string> { "No SQL data was provided." }
                });
            }

            try
            {
                var result = await _importService.ImportFromRawDumpAsync(request.RawSqlDump);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ImportResultDto
                {
                    Success = false,
                    Errors = new List<string> { $"Import failed: {ex.Message}" }
                });
            }
        }
    }

    public class EasyImportRequest
    {
        public string? RawSqlDump { get; set; }
    }
}
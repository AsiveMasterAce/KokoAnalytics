using System.Diagnostics;
using KokoAnalytics.Application.Interfaces;
using KokoAnalytics.Models;
using Microsoft.AspNetCore.Mvc;

namespace KokoAnalytics.Controllers;

public class HomeController : Controller
{
    private readonly IDashboardService _dashboardService;

    public HomeController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public async Task<IActionResult> Index(DateTime? start, DateTime? end)
    {
        var dto = await _dashboardService.GetDashboardAsync(start, end);
        var viewModel = DashboardViewModel.FromDto(dto);
        return View(viewModel);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

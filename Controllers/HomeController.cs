using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using CafeWebsite.Models;
using CafeWebsite.Data;
using Microsoft.EntityFrameworkCore;

namespace CafeWebsite.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly CafeDbContext _context;

    public HomeController(ILogger<HomeController> logger, CafeDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult About()
    {
        return View();
    }

    public async Task<IActionResult> Events()
    {
        try
        {
            var events = await _context.Events.Where(e => e.IsActive).ToListAsync();
            return View(events);
        }
        catch
        {
            return View(new List<Event>());
        }
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

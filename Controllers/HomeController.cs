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

    public IActionResult Events()
    {
        List<Event> events = new List<Event>();
        try {
            events = _context.Events.Where(e => e.IsActive).ToList();
        } catch { }
        return View(events);
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

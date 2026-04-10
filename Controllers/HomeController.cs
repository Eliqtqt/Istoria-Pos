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
        try
        {
            IQueryable<Event> query = _context.Events;
            var events = query.Where(e => e.IsActive).OrderBy(e => e.CreatedAt).ToList();
            return View(events);
        }
        catch (Exception ex)
        {
            ViewBag.Error = ex.Message;
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

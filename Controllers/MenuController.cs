using CafeWebsite.Data;
using CafeWebsite.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafeWebsite.Controllers
{
    public class MenuController : Controller
    {
        private readonly CafeDbContext _context;

        public MenuController(CafeDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var menuItems = await _context.MenuItems.ToListAsync();
            return View(menuItems);
        }

        public async Task<IActionResult> Details(int id)
        {
            var menuItem = await _context.MenuItems.FindAsync(id);
            if (menuItem == null)
            {
                return NotFound();
            }
            return View(menuItem);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rate(int id, int rating)
        {
            var menuItem = await _context.MenuItems.FindAsync(id);
            if (menuItem == null)
            {
                return NotFound();
            }

            // Validate rating (1-5)
            if (rating < 1 || rating > 5)
            {
                TempData["Error"] = "Rating must be between 1 and 5 stars.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Calculate new average rating
            var newReviewCount = menuItem.ReviewCount + 1;
            var newRating = ((menuItem.Rating * menuItem.ReviewCount) + rating) / newReviewCount;

            menuItem.Rating = newRating;
            menuItem.ReviewCount = newReviewCount;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Thank you for your rating!";

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
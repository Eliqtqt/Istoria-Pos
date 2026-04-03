using CafeWebsite.Data;
using CafeWebsite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafeWebsite.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly CafeDbContext _context;

        public AdminController(CafeDbContext context)
        {
            _context = context;
        }

        // Dashboard
        public async Task<IActionResult> Index()
        {
            var now = DateTime.UtcNow;
            var startOfDay = now.Date;
            var endOfDay = startOfDay.AddDays(1);
            
            var stats = new AdminDashboardViewModel
            {
                TotalOrders = await _context.Orders.CountAsync(),
                TotalRevenue = await _context.Orders.AnyAsync() ? await _context.Orders.SumAsync(o => o.TotalAmount) : 0,
                TotalMenuItems = await _context.MenuItems.CountAsync(),
                TotalUsers = await _context.Users.CountAsync(),
                RecentOrders = await _context.Orders
                    .Include(o => o.User)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(5)
                    .ToListAsync(),
                PendingOrders = await _context.Orders.CountAsync(o => o.Status == "Pending"),
                TodayOrders = await _context.Orders.CountAsync(o => o.OrderDate >= startOfDay && o.OrderDate < endOfDay)
            };
            return View(stats);
        }

        // Menu Management
        public async Task<IActionResult> MenuItems()
        {
            Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
            Response.Headers.Append("Pragma", "no-cache");
            Response.Headers.Append("Expires", "0");
            
            var menuItems = await _context.MenuItems.ToListAsync();
            return View(menuItems);
        }

        public async Task<IActionResult> MenuDetails(int id)
        {
            var menuItem = await _context.MenuItems.FindAsync(id);
            if (menuItem == null)
            {
                return NotFound();
            }
            return View(menuItem);
        }

        [HttpGet]
        public IActionResult CreateMenuItem()
        {
            return View(new MenuItem());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMenuItem(MenuItem menuItem)
        {
            if (ModelState.IsValid)
            {
                _context.MenuItems.Add(menuItem);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Menu item created successfully!";
                return RedirectToAction(nameof(MenuItems));
            }
            var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            TempData["Error"] = string.IsNullOrEmpty(errors) ? "Validation failed" : errors;
            return View(menuItem);
        }

        [HttpGet]
        public async Task<IActionResult> EditMenuItem(int id)
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
        public async Task<IActionResult> EditMenuItem(MenuItem menuItem)
        {
            var existingItem = await _context.MenuItems.FindAsync(menuItem.Id);
            if (existingItem == null)
            {
                return NotFound();
            }
            
            existingItem.Name = menuItem.Name;
            existingItem.Description = menuItem.Description;
            existingItem.Price = menuItem.Price;
            existingItem.Category = menuItem.Category;
            existingItem.ImageUrl = menuItem.ImageUrl;
            existingItem.Rating = menuItem.Rating;
            existingItem.ReviewCount = menuItem.ReviewCount;
            existingItem.IsVegetarian = menuItem.IsVegetarian;
            existingItem.IsVegan = menuItem.IsVegan;
            existingItem.IsGlutenFree = menuItem.IsGlutenFree;
            
            await _context.SaveChangesAsync();
            TempData["Success"] = "Menu item updated successfully!";
            return RedirectToAction(nameof(MenuItems));
        }

        [HttpGet]
        public async Task<IActionResult> DeleteMenuItem(int id)
        {
            var menuItem = await _context.MenuItems.FindAsync(id);
            if (menuItem == null)
            {
                return NotFound();
            }
            return View(menuItem);
        }

        [HttpPost, ActionName("DeleteMenuItem")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMenuItemConfirmed(int id)
        {
            var menuItem = await _context.MenuItems.FindAsync(id);
            if (menuItem != null)
            {
                _context.MenuItems.Remove(menuItem);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Menu item deleted successfully!";
            }
            return RedirectToAction(nameof(MenuItems));
        }

        // Order Management
        public async Task<IActionResult> Orders(string? status = null)
        {
            var orders = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                orders = orders.Where(o => o.Status == status);
            }

            return View(await orders.OrderByDescending(o => o.OrderDate).ToListAsync());
        }

        public async Task<IActionResult> OrderDetails(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
                .FirstOrDefaultAsync(o => o.Id == id);
            
            if (order == null)
            {
                return NotFound();
            }
            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(int id, string status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            order.Status = status;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Order status updated successfully!";
            return RedirectToAction(nameof(OrderDetails), new { id });
        }

        // User Management
        public async Task<IActionResult> Users()
        {
            var users = await _context.Users.Include(u => u.Orders).ToListAsync();
            return View(users);
        }

        public async Task<IActionResult> UserDetails(int id)
        {
            var user = await _context.Users
                .Include(u => u.Orders)
                .ThenInclude(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
                .FirstOrDefaultAsync(u => u.Id == id);
            
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUserRole(int id, string role)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.Role = role;
            await _context.SaveChangesAsync();
            TempData["Success"] = "User role updated successfully!";
            return RedirectToAction(nameof(UserDetails), new { id });
        }

        // Event Management
        public async Task<IActionResult> Events()
        {
            var events = await _context.Events.OrderBy(e => e.CreatedAt).ToListAsync();
            return View(events);
        }

        [HttpGet]
        public IActionResult CreateEvent()
        {
            return View(new Event());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEvent(Event eventModel, IFormFile? ImageFile)
        {
            if (ModelState.IsValid)
            {
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images", "events", fileName);
                    
                    var directory = Path.GetDirectoryName(filePath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(stream);
                    }
                    eventModel.ImagePath = $"/Images/events/{fileName}";
                }

                _context.Events.Add(eventModel);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Event created successfully!";
                return RedirectToAction(nameof(Events));
            }
            return View(eventModel);
        }

        [HttpGet]
        public async Task<IActionResult> EditEvent(int id)
        {
            var eventModel = await _context.Events.FindAsync(id);
            if (eventModel == null)
            {
                return NotFound();
            }
            return View(eventModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEvent(Event eventModel, IFormFile? ImageFile, string? keepImage)
        {
            if (ModelState.IsValid)
            {
                var existingEvent = await _context.Events.FindAsync(eventModel.Id);
                if (existingEvent == null)
                {
                    return NotFound();
                }

                existingEvent.Title = eventModel.Title;
                existingEvent.Description = eventModel.Description;
                existingEvent.Schedule = eventModel.Schedule;
                existingEvent.Time = eventModel.Time;
                existingEvent.Icon = eventModel.Icon;
                existingEvent.IsActive = eventModel.IsActive;

                if (ImageFile != null && ImageFile.Length > 0)
                {
                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(existingEvent.ImagePath))
                    {
                        var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existingEvent.ImagePath.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images", "events", fileName);
                    
                    var directory = Path.GetDirectoryName(filePath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(stream);
                    }
                    existingEvent.ImagePath = $"/Images/events/{fileName}";
                }
                else if (keepImage == "on" && !string.IsNullOrEmpty(existingEvent.ImagePath))
                {
                    // Keep existing image
                }
                else if (string.IsNullOrEmpty(keepImage))
                {
                    // Remove image if checkbox is unchecked
                    if (!string.IsNullOrEmpty(existingEvent.ImagePath))
                    {
                        var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existingEvent.ImagePath.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                        existingEvent.ImagePath = null;
                    }
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Event updated successfully!";
                return RedirectToAction(nameof(Events));
            }
            return View(eventModel);
        }

        [HttpGet]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var eventModel = await _context.Events.FindAsync(id);
            if (eventModel == null)
            {
                return NotFound();
            }
            return View(eventModel);
        }

        [HttpPost, ActionName("DeleteEvent")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEventConfirmed(int id)
        {
            var eventModel = await _context.Events.FindAsync(id);
            if (eventModel != null)
            {
                // Delete image if exists
                if (!string.IsNullOrEmpty(eventModel.ImagePath))
                {
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", eventModel.ImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                _context.Events.Remove(eventModel);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Event deleted successfully!";
            }
            return RedirectToAction(nameof(Events));
        }
    }

    public class AdminDashboardViewModel
    {
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalMenuItems { get; set; }
        public int TotalUsers { get; set; }
        public List<Order> RecentOrders { get; set; } = new();
        public int PendingOrders { get; set; }
        public int TodayOrders { get; set; }
    }
}

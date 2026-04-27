using CafeWebsite.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace CafeWebsite.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(CafeDbContext context)
        {
            try
            {
                // Apply any pending migrations
                await context.Database.MigrateAsync();
                Console.WriteLine("[DB Init] Database migrations applied");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB Init] Migration error: {ex.Message}");
                // If migrations fail, try to ensure database exists (for first-time setup)
                try
                {
                    await context.Database.EnsureCreatedAsync();
                    Console.WriteLine("[DB Init] Database created (no migrations applied)");
                }
                catch (Exception ex2)
                {
                    Console.WriteLine($"[DB Init] Error creating DB: {ex2.Message}");
                }
            }

            // Create admin user if not exists
            var adminExists = await context.Users.AnyAsync(u => u.Username == "admin");
            if (!adminExists)
            {
                var adminUser = new User
                {
                    Username = "admin",
                    Email = "admin@cafesite.com",
                    PasswordHash = HashPassword("admin123"),
                    Role = "Admin",
                    EmailConfirmed = true // Admin is pre-confirmed
                };
                context.Users.Add(adminUser);
                await context.SaveChangesAsync();
                Console.WriteLine("[DB Init] Admin user created");
            }

            var hasMenuItems = await context.MenuItems.AnyAsync();
            if (hasMenuItems)
            {
                Console.WriteLine("[DB Init] Menu already seeded");
                return; // DB has been seeded
            }

            var menuItems = new MenuItem[]
            {
                new MenuItem {
                    Name = "Espresso",
                    Description = "A bold, concentrated shot of coffee with rich flavor and a caramel finish.",
                    Price = 3.50m,
                    Category = "Coffee",
                    ImageUrl = "/images/espresso.jpg",
                    IsVegetarian = true,
                    IsVegan = true,
                    IsGlutenFree = true,
                    Rating = 4.8,
                    ReviewCount = 156
                },
                new MenuItem {
                    Name = "Cappuccino",
                    Description = "Espresso topped with steamed milk and a thick layer of foam.",
                    Price = 4.00m,
                    Category = "Coffee",
                    ImageUrl = "/images/cappuccino.jpg",
                    IsVegetarian = true,
                    IsVegan = false,
                    IsGlutenFree = true,
                    Rating = 4.6,
                    ReviewCount = 203
                },
                new MenuItem {
                    Name = "Vanilla Latte",
                    Description = "Smooth espresso with steamed milk and vanilla syrup.",
                    Price = 4.50m,
                    Category = "Coffee",
                    ImageUrl = "/images/latte.jpg",
                    IsVegetarian = true,
                    IsVegan = false,
                    IsGlutenFree = true,
                    Rating = 4.7,
                    ReviewCount = 189
                },
                new MenuItem {
                    Name = "Butter Croissant",
                    Description = "Flaky, golden pastry made with real butter.",
                    Price = 2.50m,
                    Category = "Pastry",
                    ImageUrl = "/images/croissant.jpg",
                    IsVegetarian = true,
                    IsVegan = false,
                    IsGlutenFree = false,
                    Rating = 4.5,
                    ReviewCount = 127
                },
                new MenuItem {
                    Name = "Blueberry Muffin",
                    Description = "Moist muffin packed with fresh blueberries.",
                    Price = 3.00m,
                    Category = "Pastry",
                    ImageUrl = "/images/muffin.jpg",
                    IsVegetarian = true,
                    IsVegan = false,
                    IsGlutenFree = false,
                    Rating = 4.4,
                    ReviewCount = 98
                },
                new MenuItem {
                    Name = "Turkey Club Sandwich",
                    Description = "Triple-decker sandwich with turkey, bacon, lettuce, and tomato.",
                    Price = 6.00m,
                    Category = "Food",
                    ImageUrl = "/images/sandwich.jpg",
                    IsVegetarian = false,
                    IsVegan = false,
                    IsGlutenFree = false,
                    Rating = 4.3,
                    ReviewCount = 76
                },
                new MenuItem {
                    Name = "Green Salad",
                    Description = "Fresh mixed greens with cherry tomatoes, cucumber, and balsamic vinaigrette.",
                    Price = 5.50m,
                    Category = "Food",
                    ImageUrl = "/images/salad.jpg",
                    IsVegetarian = true,
                    IsVegan = true,
                    IsGlutenFree = true,
                    Rating = 4.2,
                    ReviewCount = 54
                },
                new MenuItem {
                    Name = "Chocolate Chip Cookie",
                    Description = "Warm, chewy cookie loaded with chocolate chips.",
                    Price = 2.00m,
                    Category = "Pastry",
                    ImageUrl = "/images/cookie.jpg",
                    IsVegetarian = true,
                    IsVegan = false,
                    IsGlutenFree = false,
                    Rating = 4.6,
                    ReviewCount = 142
                }
            };

            foreach (var item in menuItems)
            {
                context.MenuItems.Add(item);
            }

            await context.SaveChangesAsync();
            Console.WriteLine("[DB Init] Menu items seeded");
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}

using CafeWebsite.Models;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using System.Data;
using System.Data.Common;
using Npgsql;

namespace CafeWebsite.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(CafeDbContext context)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            bool isDevelopment = environment == "Development";

            try
            {
                // Check if migration history exists
                bool historyExists = await TableExistsAsync(context, "__EFMigrationsHistory");
                bool usersExists = await TableExistsAsync(context, "Users");

                // Handle databases created without migrations (e.g., from EnsureCreated)
                if (!historyExists && usersExists)
                {
                    Console.WriteLine("[DB Init] Pre-migration database detected. Applying manual fixes...");
                    await AddMissingColumnsAsync(context);
                    await CreateBaselineMigrationHistoryAsync(context);
                    Console.WriteLine("[DB Init] Manual fix complete");
                }

                // Apply any pending migrations (should be none after baseline)
                await context.Database.MigrateAsync();
                Console.WriteLine("[DB Init] Database migrations applied successfully");
            }
            catch (Exception ex) when (isDevelopment)
            {
                Console.WriteLine($"[DB Init] Error: {ex}");
                // Check for common connection/auth errors
                var msg = ex.Message.ToLowerInvariant();
                if (msg.Contains("password") || msg.Contains("authentication") || msg.Contains("28p01"))
                {
                    Console.WriteLine("[DB Init] AUTHENTICATION FAILED: Check your database credentials (DATABASE_URL or ConnectionStrings__DefaultConnection).");
                }
                else if (msg.Contains("connection") || msg.Contains("timeout") || msg.Contains("network"))
                {
                    Console.WriteLine("[DB Init] CONNECTION FAILED: Verify database host/port and that the database is running.");
                }
                else if (msg.Contains("ssl") || msg.Contains("tls"))
                {
                    Console.WriteLine("[DB Init] SSL/TLS ERROR: Ensure SSL mode is set to require or adjust server certificate settings.");
                }
                else
                {
                    Console.WriteLine("[DB Init] UNEXPECTED ERROR: Review configuration and database state.");
                }

                // In development, fall back to EnsureCreated
                try
                {
                    await context.Database.EnsureCreatedAsync();
                    Console.WriteLine("[DB Init] Database created via EnsureCreated (development fallback)");
                }
                catch (Exception ex2)
                {
                    Console.WriteLine($"[DB Init] EnsureCreated failed: {ex2}");
                    throw;
                }
            }
            catch (Exception ex) when (!isDevelopment)
            {
                Console.WriteLine($"[DB Init] FATAL: {ex}");
                var msg = ex.Message.ToLowerInvariant();
                if (msg.Contains("password") || msg.Contains("authentication") || msg.Contains("28p01"))
                {
                    Console.WriteLine("[DB Init] AUTHENTICATION FAILED: Verify DATABASE_URL contains the correct credentials provided by Render.");
                }
                else if (msg.Contains("connection") || msg.Contains("timeout"))
                {
                    Console.WriteLine("[DB Init] CONNECTION FAILED: Check DATABASE_URL and ensure the database is accessible.");
                }
                throw;
            }

            // Seed admin user
            try
            {
                var adminExists = await context.Users.AnyAsync(u => u.Username == "admin");
                if (!adminExists)
                {
                    var adminUser = new User
                    {
                        Username = "admin",
                        Email = "admin@cafesite.com",
                        PasswordHash = HashPassword("admin123"),
                        Role = "Admin",
                        EmailConfirmed = true
                    };
                    context.Users.Add(adminUser);
                    await context.SaveChangesAsync();
                    Console.WriteLine("[DB Init] Admin user created");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB Init] Error creating admin: {ex.Message}");
                if (ex.InnerException?.Message.Contains("column") == true ||
                    ex.InnerException?.Message.Contains("does not exist") == true ||
                    ex.Message.Contains("column") ||
                    ex.Message.Contains("does not exist"))
                {
                    Console.WriteLine("[DB Init] Users table schema not ready - aborting");
                    throw;
                }
            }

            // Seed menu items if none exist
            var hasMenuItems = await context.MenuItems.AnyAsync();
            if (hasMenuItems)
            {
                Console.WriteLine("[DB Init] Menu already seeded");
                return;
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

        private static async Task<bool> TableExistsAsync(CafeDbContext context, string tableName)
        {
            var conn = context.Database.GetDbConnection();
            var shouldClose = conn.State == ConnectionState.Closed;
            if (shouldClose) await conn.OpenAsync();
            
            using var cmd = conn.CreateCommand();
            var connType = conn.GetType().Name;
            
            if (connType.Contains("Npgsql") || connType.Contains("PostgreSQL"))
            {
                cmd.CommandText = @"
                    SELECT EXISTS (
                        SELECT 1 FROM information_schema.tables 
                        WHERE table_name = @tableName AND table_schema = 'public'
                    )";
            }
            else // SQLite
            {
                cmd.CommandText = @"
                    SELECT COUNT(*) FROM sqlite_master 
                    WHERE type='table' AND name = @tableName
                ";
            }
            
            var param = cmd.CreateParameter();
            param.ParameterName = "@tableName";
            param.Value = tableName;
            cmd.Parameters.Add(param);
            
            var result = await cmd.ExecuteScalarAsync();
            if (shouldClose) conn.Close();
            
            if (result is bool b) return b;
            if (result is int i) return i > 0;
            if (result is long l) return l > 0;
            return Convert.ToBoolean(result);
        }

        private static async Task AddMissingColumnsAsync(CafeDbContext context)
        {
            var conn = context.Database.GetDbConnection();
            var shouldClose = conn.State == ConnectionState.Closed;
            if (shouldClose) await conn.OpenAsync();
            
            using var cmd = conn.CreateCommand();
            
            if (conn is Npgsql.NpgsqlConnection)
            {
                // PostgreSQL - add columns if not exists
                cmd.CommandText = @"
                    ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""EmailConfirmed"" boolean NOT NULL DEFAULT false;
                    ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""EmailConfirmationToken"" text NULL;
                    ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""TokenExpiration"" timestamp with time zone NULL;
                ";
                await cmd.ExecuteNonQueryAsync();
            }
            else // SQLite
            {
                // SQLite - try to add columns, ignore if they exist
                try
                {
                    cmd.CommandText = "ALTER TABLE Users ADD COLUMN EmailConfirmed INTEGER NOT NULL DEFAULT 0";
                    await cmd.ExecuteNonQueryAsync();
                }
                catch { }
                try
                {
                    cmd.CommandText = "ALTER TABLE Users ADD COLUMN EmailConfirmationToken TEXT NULL";
                    await cmd.ExecuteNonQueryAsync();
                }
                catch { }
                try
                {
                    cmd.CommandText = "ALTER TABLE Users ADD COLUMN TokenExpiration TEXT NULL";
                    await cmd.ExecuteNonQueryAsync();
                }
                catch { }
            }
            
            if (shouldClose) conn.Close();
        }

        private static async Task CreateBaselineMigrationHistoryAsync(CafeDbContext context)
        {
            var conn = context.Database.GetDbConnection();
            var shouldClose = conn.State == ConnectionState.Closed;
            if (shouldClose) await conn.OpenAsync();
            
            using var cmd = conn.CreateCommand();
            
            if (conn is Npgsql.NpgsqlConnection)
            {
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS ""__EFMigrationsHistory""
                    (""MigrationId"" text NOT NULL CONSTRAINT ""PK___EFMigrationsHistory"" PRIMARY KEY,
                     ""ProductVersion"" text NOT NULL)
                ";
                await cmd.ExecuteNonQueryAsync();

                var migrations = new[]
                {
                    ("20260403061139_InitialCreate", "8.0.0"),
                    ("20260427050249_AddEmailVerificationFieldsToUser", "8.0.0")
                };

                foreach (var (id, version) in migrations)
                {
                    cmd.CommandText = $@"
                        INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
                        SELECT '{id}', '{version}'
                        WHERE NOT EXISTS (SELECT 1 FROM ""__EFMigrationsHistory"" WHERE ""MigrationId"" = '{id}')
                    ";
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            else // SQLite
            {
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS __EFMigrationsHistory (
                        MigrationId TEXT NOT NULL PRIMARY KEY,
                        ProductVersion TEXT NOT NULL
                    )
                ";
                await cmd.ExecuteNonQueryAsync();

                var migrations = new[]
                {
                    ("20260403061139_InitialCreate", "8.0.0"),
                    ("20260427050249_AddEmailVerificationFieldsToUser", "8.0.0")
                };

                foreach (var (id, version) in migrations)
                {
                    cmd.CommandText = $@"
                        INSERT OR IGNORE INTO __EFMigrationsHistory (MigrationId, ProductVersion)
                        VALUES ('{id}', '{version}')
                    ";
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            
            if (shouldClose) conn.Close();
        }

        private static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }
    }
}

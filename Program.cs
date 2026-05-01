using CafeWebsite.Data;
using CafeWebsite.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Text.Json;

// Set environment early
var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

// Disable file watching in production to avoid inotify limit issues
if (environment != "Development")
{
    Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "1");
}

var builder = WebApplication.CreateBuilder(args);

// In production, configure app configuration without file watching
if (environment != "Development")
{
    builder.Configuration.Sources.Clear();
    builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                        .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
                        .AddEnvironmentVariables();
}

builder.Services.AddControllersWithViews();

// Register email sender service
builder.Services.AddSingleton<IEmailSender, EmailSender>();

// Get connection string from configuration (ConnectionStrings__DefaultConnection) or DATABASE_URL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")?.Trim();
var envConnection = Environment.GetEnvironmentVariable("DATABASE_URL")?.Trim();
var hasEnvVar = !string.IsNullOrEmpty(envConnection);

Console.WriteLine($"[DEBUG] Configuration connection string (first 50 chars): '{(connectionString != null ? connectionString.Substring(0, Math.Min(50, connectionString.Length)) : "null")}...'");
Console.WriteLine($"[DEBUG] DATABASE_URL env: '{envConnection ?? "null"}'");
Console.WriteLine($"[DEBUG] Has DATABASE_URL: {hasEnvVar}");

if (hasEnvVar)
{
    // Render provides DATABASE_URL as a PostgreSQL URI. Npgsql supports this format directly.
    // Ensure SSL mode is required for Render's hosted PostgreSQL.
    var url = envConnection!;
    if (!url.Contains("?ssl-mode=") && !url.Contains("&ssl-mode="))
    {
        url += (url.Contains("?") ? "&" : "?") + "ssl-mode=require";
    }
    connectionString = url;
    Console.WriteLine($"[DEBUG] Using DATABASE_URL as connection string with SSL enforced");
}
else if (string.IsNullOrEmpty(connectionString))
{
    // No connection string provided
    if (builder.Environment.IsDevelopment())
    {
        Console.WriteLine("[WARN] No database connection configured. Using SQLite fallback for development.");
        connectionString = "Data Source=dev.db";
    }
    else
    {
        var errorMsg = "DATABASE_URL environment variable or ConnectionStrings__DefaultConnection is not set. Please configure database connection in Render environment variables.";
        Console.WriteLine($"[ERROR] {errorMsg}");
        throw new InvalidOperationException(errorMsg);
    }
}

Console.WriteLine($"[DEBUG] Final connection string (first 50 chars): '{connectionString.Substring(0, Math.Min(50, connectionString.Length))}...'");

builder.Services.AddDbContext<CafeDbContext>(options =>
{
    if (connectionString.StartsWith("Data Source="))
    {
        options.UseSqlite(connectionString);
    }
    else
    {
        options.UseNpgsql(connectionString);
    }
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "IstoriaApp";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.SlidingExpiration = true;
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await DbInitializer.InitializeAsync(services.GetRequiredService<CafeDbContext>());
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider
    {
        Mappings =
        {
            [".mp4"] = "video/mp4"
        }
    },
    OnPrepareResponse = ctx =>
    {
        // Enable caching in production for better performance
        if (!app.Environment.IsDevelopment())
        {
            ctx.Context.Response.Headers.CacheControl = "public,max-age=31536000";
        }
    }
});

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

public static class SessionExtensions
{
    public static void SetObject<T>(this ISession session, string key, T value)
    {
        session.SetString(key, JsonSerializer.Serialize(value));
    }

    public static T? GetObject<T>(this ISession session, string key)
    {
        var value = session.GetString(key);
        return value == null ? default : JsonSerializer.Deserialize<T>(value);
    }
}
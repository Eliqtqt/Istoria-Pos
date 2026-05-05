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
var rawConnection = builder.Configuration.GetConnectionString("DefaultConnection");
var connectionString = rawConnection?.Trim();
var envConnection = Environment.GetEnvironmentVariable("DATABASE_URL")?.Trim();
var hasEnvVar = !string.IsNullOrEmpty(envConnection);

if (!hasEnvVar)
{
    // Fallback to RENDER_DATABASE_URL if DATABASE_URL not set (common misnaming)
    envConnection = Environment.GetEnvironmentVariable("RENDER_DATABASE_URL")?.Trim();
    if (!string.IsNullOrEmpty(envConnection))
    {
        hasEnvVar = true;
        Console.WriteLine("[DEBUG] Using RENDER_DATABASE_URL as DATABASE_URL fallback");
    }
}

Console.WriteLine($"[DEBUG] Configuration connection string (first 50 chars): '{(rawConnection != null ? rawConnection.Substring(0, Math.Min(50, rawConnection.Length)) : "null")}...'");
Console.WriteLine($"[DEBUG] DATABASE_URL env: '{envConnection ?? "null"}'");
Console.WriteLine($"[DEBUG] Has DATABASE_URL: {hasEnvVar}");

 if (hasEnvVar)
 {
     // Parse DATABASE_URL (PostgreSQL URI) and convert to Npgsql connection string
     var uri = new Uri(envConnection);
     var port = uri.Port > 0 ? uri.Port : 5432;
     var database = uri.AbsolutePath.TrimStart('/');
     var userInfo = uri.UserInfo.Split(':');
     var username = userInfo[0];
     var password = userInfo.Length > 1 ? userInfo[1] : "";
     
     var builder = new System.Text.StringBuilder();
     builder.Append($"Host={uri.Host}");
     builder.Append($";Port={port}");
     builder.Append($";Database={database}");
     builder.Append($";Username={username}");
     builder.Append($";Password={password}");
     builder.Append(";SslMode=Require");
     // Include Trust Server Certificate for Render (or if you have custom CA)
     builder.Append(";Trust Server Certificate=true");
     
     connectionString = builder.ToString();
     Console.WriteLine($"[DEBUG] Parsed DATABASE_URL into Npgsql connection string");
 }
else if (string.IsNullOrEmpty(connectionString) || connectionString.Contains("${") || connectionString.StartsWith("{"))
{
    // No valid connection string provided
    if (builder.Environment.IsDevelopment())
    {
        Console.WriteLine("[WARN] No database connection configured. Using SQLite fallback for development.");
        connectionString = "Data Source=dev.db";
    }
    else
    {
        var errorMsg = "DATABASE_URL environment variable or ConnectionStrings__DefaultConnection is not set or contains a placeholder. Please configure database connection in Render environment variables.";
        Console.WriteLine($"[ERROR] {errorMsg}");
        throw new InvalidOperationException(errorMsg);
    }
}
else if ((connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) || connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
         && !connectionString.Contains("ssl-mode="))
{
    // If the connection string is a URI but lacks ssl-mode, enforce it
    connectionString += (connectionString.Contains("?") ? "&" : "?") + "ssl-mode=require";
    Console.WriteLine($"[DEBUG] Added SSL mode requirement to URI connection string");
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
    var db = services.GetRequiredService<CafeDbContext>();
    db.Database.Migrate();
    await DbInitializer.InitializeAsync(db);
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
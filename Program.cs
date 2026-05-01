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

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var envConnection = Environment.GetEnvironmentVariable("DATABASE_URL");
var hasEnvVar = !string.IsNullOrEmpty(envConnection);

Console.WriteLine($"[DEBUG] appsettings connection: '{connectionString}'");
Console.WriteLine($"[DEBUG] DATABASE_URL env: '{envConnection}'");
Console.WriteLine($"[DEBUG] Has DATABASE_URL: {hasEnvVar}");

if (hasEnvVar)
{
    // Parse Render's URL: postgresql://user:pass@host:port/db
    var url = envConnection!;

    // Convert postgresql:// to postgres://
    url = url.Replace("postgresql://", "postgres://");
    
    try 
    {
        // Manually parse URL
        var withoutProto = url.Substring("postgres://".Length);
        var atIndex = withoutProto.IndexOf('@');
        if (atIndex > 0)
        {
            var userInfo = withoutProto.Substring(0, atIndex);
            var afterAt = withoutProto.Substring(atIndex + 1);
            
            var userParts = userInfo.Split(':');
            var user = userParts[0];
            var pass = userParts.Length > 1 ? userParts[1] : "";
            
            var slashIndex = afterAt.IndexOf('/');
            var hostPort = slashIndex > 0 ? afterAt.Substring(0, slashIndex) : afterAt;
            var db = slashIndex > 0 ? afterAt.Substring(slashIndex + 1) : "db";
            
            var colonIndex = hostPort.IndexOf(':');
            var host = colonIndex > 0 ? hostPort.Substring(0, colonIndex) : hostPort;
            var portStr = colonIndex > 0 ? hostPort.Substring(colonIndex + 1) : "5432";
            
            connectionString = $"Host={host};Port={portStr};Database={db};Username={user};Password={pass}";
            Console.WriteLine($"[DEBUG] Parsed: Host={host}, Port={portStr}, DB={db}, User={user}");
        }
        else
        {
            connectionString = url;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[DEBUG] Parse error: {ex.Message}");
        connectionString = url;
    }
}

if (string.IsNullOrEmpty(connectionString) || connectionString.Contains("${") || connectionString.StartsWith("{"))
{
    // Try to use a fallback SQLite for development, or provide clear error
    if (builder.Environment.IsDevelopment())
    {
        Console.WriteLine("[WARN] No database connection configured. Using SQLite fallback for development.");
        connectionString = "Data Source=dev.db";
    }
    else
    {
        var errorMsg = "DATABASE_URL environment variable is not set. Please configure it in your deployment environment.";
        Console.WriteLine($"[ERROR] {errorMsg}");
        throw new InvalidOperationException(errorMsg);
    }
}

Console.WriteLine($"[DEBUG] Final connection string: '{connectionString.Substring(0, Math.Min(20, connectionString.Length))}...'");

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
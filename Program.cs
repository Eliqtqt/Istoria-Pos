using CafeWebsite.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Disable file watching in production to avoid inotify limit
if (!builder.Environment.IsDevelopment())
{
    builder.Host.ConfigureAppConfiguration((hostingContext, config) =>
    {
        config.Sources.Clear();
        config.AddCommandLine(args);
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
              .AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: false)
              .AddEnvironmentVariables();
    });
}

builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var envConnection = Environment.GetEnvironmentVariable("DATABASE_URL");
var hasEnvVar = !string.IsNullOrEmpty(envConnection);

Console.WriteLine($"[DEBUG] appsettings connection: '{connectionString}'");
Console.WriteLine($"[DEBUG] DATABASE_URL env: '{envConnection}'");
Console.WriteLine($"[DEBUG] Has DATABASE_URL: {hasEnvVar}");

if (hasEnvVar)
{
    // Parse Render's URL: postgresql://user:pass@host:port/db
    var url = envConnection;
    
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
else if (string.IsNullOrEmpty(connectionString) || connectionString.StartsWith("${") || connectionString.StartsWith("{"))
{
    throw new InvalidOperationException("Database connection string is not configured. Please set DATABASE_URL environment variable.");
}

Console.WriteLine($"[DEBUG] Final connection string: '{connectionString.Substring(0, Math.Min(20, connectionString.Length))}...'");

builder.Services.AddDbContext<CafeWebsite.Data.CafeDbContext>(options =>
    options.UseNpgsql(connectionString));

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
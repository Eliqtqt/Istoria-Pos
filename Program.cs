using CafeWebsite.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var envConnection = Environment.GetEnvironmentVariable("DATABASE_URL");
var hasEnvVar = !string.IsNullOrEmpty(envConnection);

Console.WriteLine($"[DEBUG] appsettings connection: '{connectionString}'");
Console.WriteLine($"[DEBUG] DATABASE_URL env: '{envConnection}'");
Console.WriteLine($"[DEBUG] Has DATABASE_URL: {hasEnvVar}");

if (hasEnvVar)
{
    // Parse Render's postgresql:// URL and build proper connection string
    var url = envConnection;
    if (url.StartsWith("postgresql://"))
    {
        url = "postgres://" + url.Substring(11);
    }
    // Npgsql needs specific format
    try 
    {
        var uri = new Uri(url);
        var userInfo = uri.UserInfo.Split(':');
        var user = userInfo[0];
        var pass = userInfo.Length > 1 ? userInfo[1] : "";
        var host = uri.Host;
        var port = uri.Port > 0 ? uri.Port : 5432;
        var db = uri.AbsolutePath.TrimStart('/');
        connectionString = $"Host={host};Port={port};Database={db};Username={user};Password={pass}";
    }
    catch
    {
        // Fallback: just replace protocol
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
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    DbInitializer.Initialize(services.GetRequiredService<CafeDbContext>());
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();

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
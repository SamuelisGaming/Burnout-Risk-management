using Hamburgerz.Data;
using Hamburgerz.Models;
using Hamburgerz.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

LoadEnvironmentVariables(builder.Environment.ContentRootPath);
builder.Configuration.AddEnvironmentVariables();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString)
    ));

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddSingleton<JobRoleCatalogService>();

builder.Services.AddControllersWithViews();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "Hamburgerz.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        options.LoginPath = "/Login";
        options.LogoutPath = "/Login/Logout";
    });

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseSession();

app.Use(async (context, next) =>
{
    var isAuthenticated = context.User.Identity?.IsAuthenticated == true;
    var hasSessionUser = context.Session.GetInt32("UserId") != null;

    if (!hasSessionUser && isAuthenticated)
    {
        var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (int.TryParse(userIdClaim, out var userId))
        {
            var dbContext = context.RequestServices.GetRequiredService<AppDbContext>();
            var user = await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(currentUser => currentUser.Id == userId);

            if (user != null)
            {
                context.Session.SetInt32("UserId", user.Id);
                context.Session.SetString("Username", user.Username);
                context.Session.SetString("UserType", user.UserType);
                context.Session.SetString("Email", user.Email);
            }
            else
            {
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        }
        else
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }

    await next();
});

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

static void LoadEnvironmentVariables(string contentRootPath)
{
    var envPath = Path.Combine(contentRootPath, ".env");

    if (!File.Exists(envPath))
    {
        return;
    }

    foreach (var line in File.ReadAllLines(envPath))
    {
        var trimmedLine = line.Trim();

        if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith('#'))
        {
            continue;
        }

        if (trimmedLine.StartsWith("export ", StringComparison.OrdinalIgnoreCase))
        {
            trimmedLine = trimmedLine["export ".Length..].TrimStart();
        }

        var separatorIndex = trimmedLine.IndexOf('=');

        if (separatorIndex <= 0)
        {
            continue;
        }

        var key = trimmedLine[..separatorIndex].Trim();
        var value = trimmedLine[(separatorIndex + 1)..].Trim();

        if (value.Length >= 2)
        {
            var firstChar = value[0];
            var lastChar = value[^1];

            if ((firstChar == '"' && lastChar == '"') || (firstChar == '\'' && lastChar == '\''))
            {
                value = value[1..^1];
            }
        }

        if (!string.IsNullOrWhiteSpace(key) && string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
        {
            Environment.SetEnvironmentVariable(key, value);
        }
    }
}

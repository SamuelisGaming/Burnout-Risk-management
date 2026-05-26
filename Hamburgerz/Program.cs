using Hamburgerz.Data;
using Hamburgerz.Helpers;
using Hamburgerz.Models;
using Hamburgerz.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

using Microsoft.AspNetCore.Mvc.Razor; // lokalizacijai prie url copy paste ?ui-culture=lt-LT
using Microsoft.AspNetCore.Localization;
using System.Globalization;

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
builder.Services.AddSingleton<MeasurementQuestionCatalog>();
builder.Services.AddScoped<MeasurementScoringService>();

var emailSettings = new Hamburgerz.Services.EmailSettings();
builder.Configuration.GetSection("Email").Bind(emailSettings);
builder.Services.AddSingleton(emailSettings);
builder.Services.AddScoped<EmailService>();

builder.Services.AddLocalization (options =>
{
    options.ResourcesPath = "Resources";
});

//builder.Services.AddControllersWithViews();
builder.Services.AddControllersWithViews().AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix);

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new []
    {
        new CultureInfo("en-US"),
        new CultureInfo("lt-LT")
    };
    options.DefaultRequestCulture = new RequestCulture("lt-LT");
    options.SupportedUICultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

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

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("UserTypeSchema");
    await UserTypeSchema.EnsureAsync(dbContext, logger);
}

var localizationOptions = app.Services.GetRequiredService<
    Microsoft.Extensions.Options.IOptions<RequestLocalizationOptions>>().Value;

app.UseRequestLocalization(localizationOptions);

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
    var sessionUserId = context.Session.GetInt32("UserId");
    var hasSessionUser = sessionUserId != null;

    if (hasSessionUser || isAuthenticated)
    {
        var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userId = sessionUserId;

        if (userId == null && int.TryParse(userIdClaim, out var parsedUserId))
        {
            userId = parsedUserId;
        }

        if (userId.HasValue)
        {
            var dbContext = context.RequestServices.GetRequiredService<AppDbContext>();
            var user = await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(currentUser => currentUser.Id == userId.Value);

            if (user != null)
            {
                context.Session.SetInt32("UserId", user.Id);
                context.Session.SetString("Username", user.Username);
                context.Session.SetString("UserType", UserAccess.NormalizeUserType(user.UserType));
                context.Session.SetString("Email", user.Email);

                if (!user.IsEmailVerified)
                {
                    var path = context.Request.Path.Value ?? string.Empty;
                    var isAllowed =
                        path.StartsWith("/VerifyEmail", StringComparison.OrdinalIgnoreCase) ||
                        path.StartsWith("/Login/Logout", StringComparison.OrdinalIgnoreCase) ||
                        path.StartsWith("/css", StringComparison.OrdinalIgnoreCase) ||
                        path.StartsWith("/js", StringComparison.OrdinalIgnoreCase) ||
                        path.StartsWith("/lib", StringComparison.OrdinalIgnoreCase) ||
                        path.StartsWith("/favicon", StringComparison.OrdinalIgnoreCase);

                    if (!isAllowed)
                    {
                        context.Response.Redirect("/VerifyEmail/Pending");
                        return;
                    }
                }
            }
            else
            {
                context.Session.Clear();
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

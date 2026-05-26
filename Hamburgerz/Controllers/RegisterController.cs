using Hamburgerz.Data;
using Hamburgerz.Helpers;
using Hamburgerz.Models;
using Hamburgerz.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Hamburgerz.Controllers
{
    public class RegisterController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly JobRoleCatalogService _jobRoleCatalog;
        private readonly EmailService _emailService;
        private readonly ILogger<RegisterController> _logger;

        public RegisterController(
            AppDbContext context,
            IPasswordHasher<User> passwordHasher,
            JobRoleCatalogService jobRoleCatalog,
            EmailService emailService,
            ILogger<RegisterController> logger)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _jobRoleCatalog = jobRoleCatalog;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = new RegisterViewModel();
            await PopulateCountriesAsync(model);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(RegisterViewModel model)
        {
            await PopulateCountriesAsync(model);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var countryExists = await _context.Countries.AnyAsync(country => country.Id == model.CountryID);
            if (!countryExists)
            {
                ModelState.AddModelError(nameof(model.CountryID), IsEnglish() ? "Select a valid country." : "Pasirinkite tinkamą šalį.");
                return View(model);
            }

            var emailExists = await _context.Users.AnyAsync(u => u.Email == model.Email);
            if (emailExists)
            {
                ModelState.AddModelError("Email", IsEnglish() ? "This email is already in use." : "Toks el. paštas jau naudojamas.");
                return View(model);
            }

            var usernameExists = await _context.Users.AnyAsync(u => u.Username == model.Username);
            if (usernameExists)
            {
                ModelState.AddModelError("Username", IsEnglish() ? "This username is already in use." : "Toks slapyvardis jau naudojamas.");
                return View(model);
            }

            var normalizedGender = model.Gender switch
            {
                "male" => "Vyras",
                "female" => "Moteris",
                "other" => "Kita",
                _ => string.Empty
            };

            if (string.IsNullOrEmpty(normalizedGender))
            {
                ModelState.AddModelError("Gender", IsEnglish() ? "Select a valid gender." : "Pasirinkite tinkamą lytį.");
                return View(model);
            }

            /*var resolvedJobRole = _jobRoleCatalog.TryResolveCanonicalTitle(model.JobRole);
            if (resolvedJobRole == null)
            {
                ModelState.AddModelError(nameof(model.JobRole), IsEnglish() ? "Choose a job role from the suggestions." : "Pasirinkite darbo poziciją iš pasiūlymų sąrašo.");
                return View(model);
            }*/

            // Accept any job role, but if it matches a catalog entry, use the canonical title
            var resolvedJobRole = _jobRoleCatalog.TryResolveCanonicalTitle(model.JobRole);
            var jobRoleToSave = resolvedJobRole ?? model.JobRole?.Trim();

            var user = new User
            {
                Username = model.Username,
                Email = model.Email,
                Gender = normalizedGender,
                BirthDate = model.BirthDate?.Date,
                CountryID = model.CountryID,
                JobRole = jobRoleToSave, //resolvedJobRole,
                ExperienceYears = model.ExperienceYears,
                CompanySize = NormalizeOptionalText(model.CompanySize),
                WorkEnvironment = NormalizeOptionalText(model.WorkEnvironment),
                IsEmailVerified = false,
                UserType = UserAccess.User
            };

            user.PasswordHashed = _passwordHasher.HashPassword(user, model.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var verificationToken = new EmailVerificationToken
            {
                UserId = user.Id,
                Token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                CreatedAt = DateTime.UtcNow
            };

            _context.EmailVerificationTokens.Add(verificationToken);
            await _context.SaveChangesAsync();

            var verifyUrl = Url.Action("Index", "VerifyEmail", new { token = verificationToken.Token }, Request.Scheme)!;

            try
            {
                await _emailService.SendVerificationEmailAsync(user.Email, user.Username, verifyUrl, IsEnglish());
                _logger.LogInformation("Verification email sent to {Email}", user.Email);
                HttpContext.Session.SetString("LastVerificationEmailSent", DateTimeOffset.UtcNow.ToString("O"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send verification email to {Email}", user.Email);
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, UserAccess.NormalizeUserType(user.UserType))
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties { IsPersistent = true, AllowRefresh = true });

            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("UserType", UserAccess.NormalizeUserType(user.UserType));
            HttpContext.Session.SetString("Email", user.Email);

            return RedirectToAction("Pending", "VerifyEmail");
        }

        private async Task PopulateCountriesAsync(RegisterViewModel model)
        {
            model.Countries = await _context.Countries
                .AsNoTracking()
                .OrderBy(country => country.Name)
                .Select(country => new SelectListItem
                {
                    Value = country.Id.ToString(),
                    Text = country.Name
                })
                .ToListAsync();
        }

        private static string? NormalizeOptionalText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Trim();
        }

        private static bool IsEnglish() =>
            System.Globalization.CultureInfo.CurrentUICulture.Name.Equals("en-US", StringComparison.OrdinalIgnoreCase);
    }
}

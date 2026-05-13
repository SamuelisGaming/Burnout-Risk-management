using Hamburgerz.Data;
using Hamburgerz.Helpers;
using Hamburgerz.Models;
using Hamburgerz.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Hamburgerz.Controllers
{
    public class RegisterController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly JobRoleCatalogService _jobRoleCatalog;

        public RegisterController(
            AppDbContext context,
            IPasswordHasher<User> passwordHasher,
            JobRoleCatalogService jobRoleCatalog)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _jobRoleCatalog = jobRoleCatalog;
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

            var resolvedJobRole = _jobRoleCatalog.TryResolveCanonicalTitle(model.JobRole);
            if (resolvedJobRole == null)
            {
                ModelState.AddModelError(nameof(model.JobRole), IsEnglish() ? "Choose a job role from the suggestions." : "Pasirinkite darbo poziciją iš pasiūlymų sąrašo.");
                return View(model);
            }

            var user = new User
            {
                Username = model.Username,
                Email = model.Email,
                Gender = normalizedGender,
                BirthDate = model.BirthDate?.Date,
                CountryID = model.CountryID,
                JobRole = resolvedJobRole,
                ExperienceYears = model.ExperienceYears,
                CompanySize = NormalizeOptionalText(model.CompanySize),
                WorkEnvironment = NormalizeOptionalText(model.WorkEnvironment),
                IsEmailVerified = false,
                UserType = UserAccess.User
            };

            user.PasswordHashed = _passwordHasher.HashPassword(user, model.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = IsEnglish() ? "Registration successful. You can now log in." : "Registracija sėkminga. Dabar galite prisijungti.";
            return RedirectToAction("Index", "Login");
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

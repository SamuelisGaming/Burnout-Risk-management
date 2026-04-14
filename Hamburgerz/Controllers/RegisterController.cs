using Hamburgerz.Data;
using Hamburgerz.Models;
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

        public RegisterController(AppDbContext context, IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
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
                ModelState.AddModelError(nameof(model.CountryID), "Pasirinkite tinkama sali");
                return View(model);
            }

            var emailExists = await _context.Users.AnyAsync(u => u.Email == model.Email);
            if (emailExists)
            {
                ModelState.AddModelError("Email", "Toks el. paštas jau naudojamas");
                return View(model);
            }

            var usernameExists = await _context.Users.AnyAsync(u => u.Username == model.Username);
            if (usernameExists)
            {
                ModelState.AddModelError("Username", "Toks slapyvardis jau naudojamas");
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
                ModelState.AddModelError("Gender", "Pasirinkite tinkamą lytį");
                return View(model);
            }

            var user = new User
            {
                Username = model.Username,
                Email = model.Email,
                Gender = normalizedGender,
                BirthDate = model.BirthDate?.Date,
                CountryID = model.CountryID,
                JobRole = NormalizeOptionalText(model.JobRole),
                ExperienceYears = model.ExperienceYears,
                CompanySize = NormalizeOptionalText(model.CompanySize),
                WorkEnvironment = NormalizeOptionalText(model.WorkEnvironment),
                IsEmailVerified = false,
                UserType = "user"
            };

            user.PasswordHashed = _passwordHasher.HashPassword(user, model.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Registracija sėkminga. Dabar galite prisijungti.";
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
    }
}

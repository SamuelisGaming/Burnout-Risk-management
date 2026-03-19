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
        /*
        [HttpGet]
        public IActionResult Index()
        {
            return View(new RegisterViewModel());
        }*/

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = new RegisterViewModel
            {
                Countries = await _context.Countries
                    .Select(c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Name
                    })
                    .ToListAsync()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Countries = await _context.Countries
                    .Select(c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Name
                    })
                    .ToListAsync();
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
                "male" => "Male",
                "female" => "Female",
                "other" => "Other",
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
                CountryID = model.CountryID,
                IsEmailVerified = false,
                UserType = "user"
            };

            user.PasswordHashed = _passwordHasher.HashPassword(user, model.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Registracija sėkminga. Dabar galite prisijungti.";
            return RedirectToAction("Index", "Login");
        }
    }
}
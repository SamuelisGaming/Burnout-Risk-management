using Hamburgerz.Data;
using Hamburgerz.Models;
using Hamburgerz.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hamburgerz.Controllers
{
    public class ForgotPasswordController : Controller
    {
        private readonly AppDbContext _context;
        private readonly EmailService _emailService;
        private readonly IPasswordHasher<User> _passwordHasher;

        public ForgotPasswordController(AppDbContext context, EmailService emailService, IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _emailService = emailService;
            _passwordHasher = passwordHasher;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user != null)
            {
                var existing = await _context.PasswordResetTokens
                    .Where(t => t.UserId == user.Id && !t.Used)
                    .ToListAsync();
                _context.PasswordResetTokens.RemoveRange(existing);

                var token = new PasswordResetToken
                {
                    UserId = user.Id,
                    Token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
                    ExpiresAt = DateTime.UtcNow.AddHours(1),
                    CreatedAt = DateTime.UtcNow
                };

                _context.PasswordResetTokens.Add(token);
                await _context.SaveChangesAsync();

                var resetUrl = Url.Action("Reset", "ForgotPassword", new { token = token.Token }, Request.Scheme)!;
                bool isEnglish = IsEnglish();

                try
                {
                    await _emailService.SendPasswordResetEmailAsync(user.Email, user.Username, resetUrl, isEnglish);
                }
                catch
                {
                    // silently ignore – don't reveal whether email was sent
                }
            }

            TempData["ResetSent"] = true;
            return RedirectToAction("Sent");
        }

        [HttpGet]
        public IActionResult Sent()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Reset(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return RedirectToAction("Index", "Login");
            }

            var record = await _context.PasswordResetTokens
                .FirstOrDefaultAsync(t => t.Token == token && !t.Used && t.ExpiresAt > DateTime.UtcNow);

            if (record == null)
            {
                TempData["ResetError"] = true;
                return View("ResetExpired");
            }

            return View(new ResetPasswordViewModel { Token = token });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reset(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var record = await _context.PasswordResetTokens
                .FirstOrDefaultAsync(t => t.Token == model.Token && !t.Used && t.ExpiresAt > DateTime.UtcNow);

            if (record == null)
            {
                TempData["ResetError"] = true;
                return View("ResetExpired");
            }

            if (!PasswordRules.IsStrong(model.Password))
            {
                ModelState.AddModelError(nameof(model.Password), PasswordRules.RequirementsMessage);
                return View(model);
            }

            var user = await _context.Users.FindAsync(record.UserId);
            if (user == null)
            {
                return View("ResetExpired");
            }

            user.PasswordHashed = _passwordHasher.HashPassword(user, model.Password);
            record.Used = true;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = IsEnglish()
                ? "Password changed successfully. You can now log in."
                : "Slaptažodis sėkmingai pakeistas. Dabar galite prisijungti.";

            return RedirectToAction("Index", "Login");
        }

        private static bool IsEnglish() =>
            System.Globalization.CultureInfo.CurrentUICulture.Name.Equals("en-US", StringComparison.OrdinalIgnoreCase);
    }
}

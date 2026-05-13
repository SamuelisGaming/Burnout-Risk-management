using Hamburgerz.Data;
using Hamburgerz.Models;
using Hamburgerz.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hamburgerz.Controllers
{
    public class VerifyEmailController : Controller
    {
        private readonly AppDbContext _context;
        private readonly EmailService _emailService;
        private readonly ILogger<VerifyEmailController> _logger;

        public VerifyEmailController(AppDbContext context, EmailService emailService, ILogger<VerifyEmailController> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return RedirectToAction("Pending");
            }

            var record = await _context.EmailVerificationTokens
                .FirstOrDefaultAsync(t => t.Token == token);

            if (record == null || record.ExpiresAt < DateTime.UtcNow)
            {
                if (record != null)
                {
                    _context.EmailVerificationTokens.Remove(record);
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction("Pending");
            }

            var user = await _context.Users.FindAsync(record.UserId);
            if (user == null)
            {
                return RedirectToAction("Pending");
            }

            user.IsEmailVerified = true;
            _context.EmailVerificationTokens.Remove(record);
            await _context.SaveChangesAsync();

            bool isEnglish = System.Globalization.CultureInfo.CurrentUICulture.Name
                .Equals("en-US", StringComparison.OrdinalIgnoreCase);
            TempData["ToastSuccess"] = isEnglish
                ? "Your email has been verified successfully."
                : "El. paštas sėkmingai patvirtintas.";

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Pending()
        {
            var lastSentStr = HttpContext.Session.GetString("LastVerificationEmailSent");
            if (lastSentStr != null && DateTimeOffset.TryParse(lastSentStr, out var lastSent))
            {
                var secondsLeft = (int)(180 - (DateTimeOffset.UtcNow - lastSent).TotalSeconds);
                if (secondsLeft > 0)
                    TempData["CooldownSeconds"] = secondsLeft;
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Resend()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Index", "Login");

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null || user.IsEmailVerified)
                return RedirectToAction("Index", "Home");

            var lastSentStr = HttpContext.Session.GetString("LastVerificationEmailSent");
            if (lastSentStr != null && DateTimeOffset.TryParse(lastSentStr, out var lastSent))
            {
                var secondsLeft = (int)(180 - (DateTimeOffset.UtcNow - lastSent).TotalSeconds);
                if (secondsLeft > 0)
                {
                    TempData["CooldownSeconds"] = secondsLeft;
                    return RedirectToAction("Pending");
                }
            }

            var existing = await _context.EmailVerificationTokens
                .Where(t => t.UserId == userId.Value)
                .ToListAsync();
            _context.EmailVerificationTokens.RemoveRange(existing);

            var token = new EmailVerificationToken
            {
                UserId = userId.Value,
                Token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                CreatedAt = DateTime.UtcNow
            };

            _context.EmailVerificationTokens.Add(token);
            await _context.SaveChangesAsync();

            var verifyUrl = Url.Action("Index", "VerifyEmail", new { token = token.Token }, Request.Scheme)!;
            bool isEnglish = System.Globalization.CultureInfo.CurrentUICulture.Name
                .Equals("en-US", StringComparison.OrdinalIgnoreCase);

            try
            {
                await _emailService.SendVerificationEmailAsync(user.Email, user.Username, verifyUrl, isEnglish);
                _logger.LogInformation("Verification email sent to {Email}", user.Email);
                HttpContext.Session.SetString("LastVerificationEmailSent", DateTimeOffset.UtcNow.ToString("O"));
                TempData["Resent"] = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send verification email to {Email}", user.Email);
            }

            return RedirectToAction("Pending");
        }
    }
}

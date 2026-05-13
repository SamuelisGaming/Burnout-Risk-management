using Hamburgerz.Data;
using Hamburgerz.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hamburgerz.Controllers
{
    public class BugReportController : Controller
    {
        private readonly AppDbContext _context;
        private readonly EmailService _emailService;
        private readonly ILogger<BugReportController> _logger;

        public BugReportController(AppDbContext context, EmailService emailService, ILogger<BugReportController> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit([FromForm] string topic, [FromForm] string description, [FromForm] string deviceInfo)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized();

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
                return Unauthorized();

            topic = string.IsNullOrWhiteSpace(topic) ? "Unknown" : topic.Trim();
            description = string.IsNullOrWhiteSpace(description) ? "(no description)" : description.Trim();
            deviceInfo = string.IsNullOrWhiteSpace(deviceInfo) ? "(unknown)" : deviceInfo.Trim();

            try
            {
                await _emailService.SendBugReportEmailAsync(topic, description, user.Username, user.Email, deviceInfo);
                _logger.LogInformation("Bug report from {User} — topic: {Topic}", user.Username, topic);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send bug report email from {User}", user.Username);
                return StatusCode(500);
            }

            return Ok();
        }
    }
}

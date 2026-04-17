using Hamburgerz.Data;
using Hamburgerz.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Google.GenAI;
using static Google.Apis.Requests.BatchRequest;

namespace Hamburgerz.Controllers
{
    public class DataController : Controller
    {
        private readonly AppDbContext _context;
        

    public DataController(AppDbContext context)
        {
            _context = context;

        }

        [HttpGet]
        public async Task<IActionResult> DataEntry()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Index", "Login");
            }

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId.Value);

            if (user == null)
            {
                return RedirectToAction("Logout", "Login");
            }

            var model = await BuildMeasurementEntryViewModelAsync(user);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DataEntry(MeasurementEntryViewModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Index", "Login");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId.Value);

            if (user == null)
            {
                return RedirectToAction("Logout", "Login");
            }

            if (!ModelState.IsValid)
            {
                var invalidModel = await BuildMeasurementEntryViewModelAsync(user, model);
                return View(invalidModel);
            }

            var riskData = new RiskData
            {
                UserId = user.Id,
                Gender = user.Gender,
                JobRole = NormalizeOptionalText(user.JobRole),
                ExperienceYears = user.ExperienceYears,
                CompanySize = NormalizeOptionalText(user.CompanySize),
                WorkEnvironment = NormalizeOptionalText(user.WorkEnvironment),
                WorkHours = model.WorkHours!.Value,
                MeetingsPerDay = model.MeetingsPerDay!.Value,
                InternetSpeed = model.InternetSpeed!.Value,
                SleepHours = model.SleepHours!.Value,
                ExerciseHours = model.ExerciseHours!.Value,
                ScreenTime = model.ScreenTime!.Value,
                StressLevel = model.StressLevel,
                MoodScore = model.MoodScore,
                DisconnectScore = model.DisconnectScore,
                FocusScore = model.FocusScore,
                TimeStamp = DateTime.Now,
                Suggestion = null

            };

            _context.RiskData.Add(riskData);
            await _context.SaveChangesAsync();

            return RedirectToAction("Result", "Profile", new { id = riskData.ID });
        }

        private async Task<MeasurementEntryViewModel> BuildMeasurementEntryViewModelAsync(
            User user,
            MeasurementEntryViewModel? submittedModel = null)
        {
            var country = user.CountryID.HasValue
                ? await _context.Countries
                    .AsNoTracking()
                    .Where(c => c.Id == user.CountryID.Value)
                    .Select(c => c.Name)
                    .FirstOrDefaultAsync()
                : null;

            return new MeasurementEntryViewModel
            {
                BirthDate = user.BirthDate,
                Gender = user.Gender,
                Country = country ?? string.Empty,
                JobRole = user.JobRole ?? string.Empty,
                ExperienceYears = user.ExperienceYears,
                CompanySize = user.CompanySize ?? string.Empty,
                WorkEnvironment = user.WorkEnvironment ?? string.Empty,
                WorkHours = submittedModel?.WorkHours,
                MeetingsPerDay = submittedModel?.MeetingsPerDay,
                InternetSpeed = submittedModel?.InternetSpeed,
                SleepHours = submittedModel?.SleepHours,
                ExerciseHours = submittedModel?.ExerciseHours,
                ScreenTime = submittedModel?.ScreenTime,
                StressLevel = submittedModel?.StressLevel ?? string.Empty
                
            };
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

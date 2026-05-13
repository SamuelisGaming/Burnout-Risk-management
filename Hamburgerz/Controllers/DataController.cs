using Hamburgerz.Data;
using Hamburgerz.Helpers;
using Hamburgerz.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hamburgerz.Controllers
{
    public class DataController : Controller
    {
        private const int RequiredQuestionCount = 19;

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

            var todayMeasurement = await GetTodayMeasurementQuery(user.Id)
                .AsNoTracking()
                .FirstOrDefaultAsync();
            var measurementCount = await _context.RiskData.CountAsync(r => r.UserId == user.Id);
            var model = await BuildMeasurementEntryViewModelAsync(user, existingMeasurement: todayMeasurement);
            ApplyMeasurementAccess(model, user.UserType, measurementCount, todayMeasurement != null);
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

            var todayMeasurement = await GetTodayMeasurementQuery(user.Id)
                .FirstOrDefaultAsync();
            var measurementCount = await _context.RiskData.CountAsync(r => r.UserId == user.Id);
            var isEditingTodayMeasurement = todayMeasurement != null;

            if (!UserAccess.HasPremiumFeatures(user.UserType)
                && !isEditingTodayMeasurement
                && measurementCount >= UserAccess.FreeMeasurementLimit)
            {
                var limitedModel = await BuildMeasurementEntryViewModelAsync(user, model);
                ApplyMeasurementAccess(limitedModel, user.UserType, measurementCount, false);
                TempData["MeasurementLimitMessage"] = IsEnglish()
                    ? $"The free plan allows up to {UserAccess.FreeMeasurementLimit} saved measurements. Premium users do not have this limit."
                    : $"Nemokamas planas leidžia išsaugoti iki {UserAccess.FreeMeasurementLimit} matavimų. Premium vartotojams šis limitas netaikomas.";
                return View(limitedModel);
            }

            if (model.Q == null || model.Q.Count < RequiredQuestionCount)
            {
                ModelState.AddModelError(nameof(model.Q), IsEnglish()
                    ? "Answer all questionnaire questions."
                    : "Atsakykite į visus klausimyno klausimus.");
            }

            if (!ModelState.IsValid)
            {
                var invalidModel = await BuildMeasurementEntryViewModelAsync(user, model, todayMeasurement);
                ApplyMeasurementAccess(invalidModel, user.UserType, measurementCount, isEditingTodayMeasurement);
                return View(invalidModel);
            }

            var answers = model.Q!;
            int burnoutScore = (int)(answers.Average() * 25);
            float productivityScore = 100;

            for(int i = 6; i<13; i++)
            {
                productivityScore -= (float)answers[i] / 7 * 25;
            }

            var riskData = todayMeasurement ?? new RiskData
            {
                UserId = user.Id,
            };

            riskData.Gender = user.Gender;
            riskData.JobRole = NormalizeOptionalText(user.JobRole);
            riskData.ExperienceYears = user.ExperienceYears;
            riskData.CompanySize = NormalizeOptionalText(user.CompanySize);
            riskData.WorkEnvironment = NormalizeOptionalText(user.WorkEnvironment);
            riskData.WorkHours = model.WorkHours!.Value;
            riskData.MeetingsPerDay = model.MeetingsPerDay!.Value;
            //riskData.InternetSpeed = model.InternetSpeed!.Value;
            riskData.SleepHours = model.SleepHours!.Value;
            riskData.ExerciseHours = model.ExerciseHours!.Value;
            riskData.ScreenTime = model.ScreenTime!.Value;
            riskData.StressLevel = model.StressLevel;
            riskData.MoodScore = model.MoodScore;
            riskData.BurnoutRisk = burnoutScore;
            riskData.ProductivityScore = (int)productivityScore;
            riskData.DisconnectScore = model.DisconnectScore;
            riskData.FocusScore = model.FocusScore;
            riskData.TimeStamp = DateTime.Now;
            riskData.Suggestion = null;

            if (todayMeasurement == null)
            {
                _context.RiskData.Add(riskData);
            }
            else
            {
                await InvalidateAnalysisCacheAsync(user.Id);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Result", "Profile", new { id = riskData.ID });
        }

        private async Task<MeasurementEntryViewModel> BuildMeasurementEntryViewModelAsync(
            User user,
            MeasurementEntryViewModel? submittedModel = null,
            RiskData? existingMeasurement = null)
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
                WorkHours = submittedModel?.WorkHours ?? existingMeasurement?.WorkHours,
                MeetingsPerDay = submittedModel?.MeetingsPerDay ?? existingMeasurement?.MeetingsPerDay,
                InternetSpeed = submittedModel?.InternetSpeed ?? existingMeasurement?.InternetSpeed,
                SleepHours = submittedModel?.SleepHours ?? existingMeasurement?.SleepHours,
                ExerciseHours = submittedModel?.ExerciseHours ?? existingMeasurement?.ExerciseHours,
                ScreenTime = submittedModel?.ScreenTime ?? existingMeasurement?.ScreenTime,
                StressLevel = submittedModel?.StressLevel ?? existingMeasurement?.StressLevel ?? string.Empty,
                Q = submittedModel?.Q ?? new List<int>(),
                MoodScore = submittedModel?.MoodScore ?? existingMeasurement?.MoodScore,
                DisconnectScore = submittedModel?.DisconnectScore ?? existingMeasurement?.DisconnectScore,
                FocusScore = submittedModel?.FocusScore ?? existingMeasurement?.FocusScore,
                ExistingMeasurementId = existingMeasurement?.ID,
                IsEditingTodayMeasurement = existingMeasurement != null,
                ExistingMeasurementTimeStamp = existingMeasurement?.TimeStamp
                
            };
        }

        private static void ApplyMeasurementAccess(
            MeasurementEntryViewModel model,
            string? userType,
            int measurementCount,
            bool isEditingTodayMeasurement)
        {
            var normalizedType = UserAccess.NormalizeUserType(userType);

            model.UserType = normalizedType;
            model.MeasurementCount = measurementCount;
            model.IsEditingTodayMeasurement = isEditingTodayMeasurement;
            model.MeasurementLimit = UserAccess.HasPremiumFeatures(normalizedType)
                ? null
                : UserAccess.FreeMeasurementLimit;
            model.CanCreateMeasurement = isEditingTodayMeasurement
                || !model.MeasurementLimit.HasValue
                || measurementCount < model.MeasurementLimit.Value;
        }

        private IQueryable<RiskData> GetTodayMeasurementQuery(int userId)
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            return _context.RiskData
                .Where(r => r.UserId == userId && r.TimeStamp >= today && r.TimeStamp < tomorrow)
                .OrderByDescending(r => r.TimeStamp);
        }

        private async Task InvalidateAnalysisCacheAsync(int userId)
        {
            var cache = await _context.AnalysisCache.FirstOrDefaultAsync(c => c.UserId == userId);
            if (cache != null)
            {
                _context.AnalysisCache.Remove(cache);
            }
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

using Hamburgerz.Data;
using Hamburgerz.Helpers;
using Hamburgerz.Models;
using Hamburgerz.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hamburgerz.Controllers
{
    public class DataController : Controller
    {
        private const int DailyQuestionCount = 9;

        private readonly AppDbContext _context;
        private readonly MeasurementQuestionCatalog _questionCatalog;
        private readonly MeasurementScoringService _scoringService;

        public DataController(
            AppDbContext context,
            MeasurementQuestionCatalog questionCatalog,
            MeasurementScoringService scoringService)
        {
            _context = context;
            _questionCatalog = questionCatalog;
            _scoringService = scoringService;
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
            var exactQuestionKeys = todayMeasurement == null
                ? null
                : await GetMeasurementQuestionKeysAsync(todayMeasurement.ID);

            var measurementCount = await _context.RiskData.CountAsync(r => r.UserId == user.Id);
            var model = await BuildMeasurementEntryViewModelAsync(
                user,
                existingMeasurement: todayMeasurement,
                exactQuestionKeys: exactQuestionKeys);

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
            var postedQuestionKeys = NormalizePostedQuestionKeys(model.QuestionKeys);

            if (!UserAccess.HasPremiumFeatures(user.UserType)
                && !isEditingTodayMeasurement
                && measurementCount >= UserAccess.FreeMeasurementLimit)
            {
                var limitedModel = await BuildMeasurementEntryViewModelAsync(
                    user,
                    model,
                    todayMeasurement,
                    postedQuestionKeys);
                ApplyMeasurementAccess(limitedModel, user.UserType, measurementCount, false);
                TempData["MeasurementLimitMessage"] = IsEnglish()
                    ? $"The free plan allows up to {UserAccess.FreeMeasurementLimit} saved measurements. Premium users do not have this limit."
                    : $"Nemokamas planas leidzia issaugoti iki {UserAccess.FreeMeasurementLimit} matavimu. Premium vartotojams sis limitas netaikomas.";
                return View(limitedModel);
            }

            var validQuestionKeys = ValidateQuestionAnswers(user, postedQuestionKeys, model.QuestionScores);
            ValidateDailyAnswers(model);

            if (!ModelState.IsValid)
            {
                var invalidModel = await BuildMeasurementEntryViewModelAsync(
                    user,
                    model,
                    todayMeasurement,
                    validQuestionKeys.Count > 0 ? validQuestionKeys : postedQuestionKeys);
                ApplyMeasurementAccess(invalidModel, user.UserType, measurementCount, isEditingTodayMeasurement);
                return View(invalidModel);
            }

            var submittedScores = validQuestionKeys.ToDictionary(
                key => key,
                key => model.QuestionScores[key]!.Value);
            var latestAnswers = await GetLatestQuestionAnswersAsync(user.Id, todayMeasurement?.ID);
            var score = _scoringService.CalculateScore(user, model, submittedScores, latestAnswers, DateTime.Today);

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
            riskData.InternetSpeed = model.InternetSpeed ?? todayMeasurement?.InternetSpeed ?? 0;
            riskData.SleepHours = model.SleepHours!.Value;
            riskData.ExerciseHours = model.ExerciseHours!.Value;
            riskData.ScreenTime = model.ScreenTime!.Value;
            riskData.StressLevel = model.StressLevel;
            riskData.MoodScore = model.MoodScore;
            riskData.BurnoutRisk = score.BurnoutScore;
            riskData.ProductivityScore = score.ProductivityScore;
            riskData.DisconnectScore = model.DisconnectScore;
            riskData.FocusScore = model.FocusScore;
            riskData.ScoreVersion = 2;
            riskData.BurnoutCoverage = score.Coverage;
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
            await ReplaceMeasurementAnswersAsync(riskData, submittedScores);

            return RedirectToAction("Result", "Profile", new { id = riskData.ID });
        }

        private async Task<MeasurementEntryViewModel> BuildMeasurementEntryViewModelAsync(
            User user,
            MeasurementEntryViewModel? submittedModel = null,
            RiskData? existingMeasurement = null,
            IReadOnlyCollection<string>? exactQuestionKeys = null)
        {
            var country = user.CountryID.HasValue
                ? await _context.Countries
                    .AsNoTracking()
                    .Where(c => c.Id == user.CountryID.Value)
                    .Select(c => c.Name)
                    .FirstOrDefaultAsync()
                : null;

            var latestAnswers = await GetLatestQuestionAnswersAsync(user.Id);
            var selection = _scoringService.SelectQuestions(
                user,
                latestAnswers,
                DateTime.Today,
                exactQuestionKeys);
            var submittedScores = submittedModel?.QuestionScores ?? new Dictionary<string, int?>();
            var questions = selection.Questions
                .Select(question =>
                {
                    var selectedScore = submittedScores.TryGetValue(question.Key, out var submittedScore)
                        ? submittedScore
                        : existingMeasurement != null && latestAnswers.TryGetValue(question.Key, out var answer)
                            ? answer.Score
                            : null;

                    latestAnswers.TryGetValue(question.Key, out var latestAnswer);
                    return _questionCatalog.ToViewModel(question, selectedScore, DateTime.Today, latestAnswer);
                })
                .ToList();

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
                StressLevel = submittedModel?.StressLevel ?? NormalizeStressForForm(existingMeasurement?.StressLevel),
                Q = submittedModel?.Q ?? new List<int>(),
                Questions = questions,
                QuestionKeys = questions.Select(question => question.Key).ToList(),
                QuestionScores = questions.ToDictionary(
                    question => question.Key,
                    question => question.SelectedScore),
                IsFirstQuestionnaire = selection.IsFirstQuestionnaire,
                DailyQuestionCount = DailyQuestionCount,
                QuestionOrderSeed = $"{user.Id}:{DateTime.Today:yyyyMMdd}:{string.Join('|', questions.Select(question => question.Key))}",
                MoodScore = submittedModel?.MoodScore ?? existingMeasurement?.MoodScore,
                DisconnectScore = submittedModel?.DisconnectScore ?? existingMeasurement?.DisconnectScore,
                FocusScore = submittedModel?.FocusScore ?? existingMeasurement?.FocusScore,
                ExistingMeasurementId = existingMeasurement?.ID,
                IsEditingTodayMeasurement = existingMeasurement != null,
                ExistingMeasurementTimeStamp = existingMeasurement?.TimeStamp
            };
        }

        private List<string> ValidateQuestionAnswers(
            User user,
            IReadOnlyCollection<string> postedQuestionKeys,
            IDictionary<string, int?> submittedScores)
        {
            var applicableKeys = _questionCatalog
                .GetApplicableQuestions(user)
                .Select(question => question.Key)
                .ToHashSet(StringComparer.Ordinal);
            var validQuestionKeys = postedQuestionKeys
                .Where(applicableKeys.Contains)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (validQuestionKeys.Count == 0)
            {
                ModelState.AddModelError(nameof(MeasurementEntryViewModel.QuestionScores), IsEnglish()
                    ? "Answer the burnout questions shown today."
                    : "Atsakykite i siandien rodomus perdegimo klausimus.");
                return validQuestionKeys;
            }

            foreach (var key in validQuestionKeys)
            {
                if (!submittedScores.TryGetValue(key, out var score) || !score.HasValue || score.Value < 0 || score.Value > 4)
                {
                    ModelState.AddModelError($"QuestionScores[{key}]", IsEnglish()
                        ? "Choose one answer."
                        : "Pasirinkite viena atsakyma.");
                }
            }

            return validQuestionKeys;
        }

        private void ValidateDailyAnswers(MeasurementEntryViewModel model)
        {
            if (string.IsNullOrEmpty(model.StressLevel))
            {
                ModelState.AddModelError(nameof(model.StressLevel), IsEnglish()
                    ? "Choose today's stress level."
                    : "Pasirinkite siandienos streso lygi.");
            }

            if (!model.MoodScore.HasValue)
            {
                ModelState.AddModelError(nameof(model.MoodScore), IsEnglish()
                    ? "Choose today's energy level."
                    : "Pasirinkite siandienos energijos lygi.");
            }

            if (!model.DisconnectScore.HasValue)
            {
                ModelState.AddModelError(nameof(model.DisconnectScore), IsEnglish()
                    ? "Choose how well you disconnected after work."
                    : "Pasirinkite, kaip pavyko atsiriboti po darbo.");
            }

            if (!model.FocusScore.HasValue)
            {
                ModelState.AddModelError(nameof(model.FocusScore), IsEnglish()
                    ? "Choose today's focus level."
                    : "Pasirinkite siandienos susikaupimo lygi.");
            }
        }

        private async Task<Dictionary<string, MeasurementAnswer>> GetLatestQuestionAnswersAsync(
            int userId,
            int? excludeRiskDataId = null)
        {
            var query = _context.MeasurementAnswers
                .AsNoTracking()
                .Where(answer => answer.UserId == userId);

            if (excludeRiskDataId.HasValue)
            {
                query = query.Where(answer => answer.RiskDataId != excludeRiskDataId.Value);
            }

            var answers = await query
                .OrderByDescending(answer => answer.AnsweredAt)
                .ThenByDescending(answer => answer.Id)
                .ToListAsync();

            return answers
                .GroupBy(answer => answer.QuestionKey)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        }

        private async Task<List<string>> GetMeasurementQuestionKeysAsync(int riskDataId)
        {
            return await _context.MeasurementAnswers
                .AsNoTracking()
                .Where(answer => answer.RiskDataId == riskDataId)
                .OrderBy(answer => answer.Id)
                .Select(answer => answer.QuestionKey)
                .ToListAsync();
        }

        private async Task ReplaceMeasurementAnswersAsync(RiskData riskData, IReadOnlyDictionary<string, int> submittedScores)
        {
            var existingAnswers = await _context.MeasurementAnswers
                .Where(answer => answer.RiskDataId == riskData.ID)
                .ToListAsync();

            if (existingAnswers.Count > 0)
            {
                _context.MeasurementAnswers.RemoveRange(existingAnswers);
            }

            foreach (var (questionKey, score) in submittedScores)
            {
                _context.MeasurementAnswers.Add(new MeasurementAnswer
                {
                    RiskDataId = riskData.ID,
                    UserId = riskData.UserId,
                    QuestionKey = questionKey,
                    Score = score,
                    AnsweredAt = riskData.TimeStamp
                });
            }

            await _context.SaveChangesAsync();
        }

        private static List<string> NormalizePostedQuestionKeys(IEnumerable<string>? keys)
        {
            return (keys ?? Array.Empty<string>())
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .Select(key => key.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToList();
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

        private static string NormalizeStressForForm(string? value)
        {
            var normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
            if (normalized.Contains("auk") || normalized.Contains("high"))
            {
                return "High";
            }

            if (normalized.Contains("vid") || normalized.Contains("med"))
            {
                return "Medium";
            }

            if (normalized.Contains("\u017eem") || normalized.Contains("zem") || normalized.Contains("low"))
            {
                return "Low";
            }

            return value ?? string.Empty;
        }

        private static bool IsEnglish() =>
            System.Globalization.CultureInfo.CurrentUICulture.Name.Equals("en-US", StringComparison.OrdinalIgnoreCase);
    }
}

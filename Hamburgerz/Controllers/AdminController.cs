using System.Linq.Expressions;
using Google.GenAI;
using Hamburgerz.Data;
using Hamburgerz.Helpers;
using Hamburgerz.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hamburgerz.Controllers
{
    public class AdminController : Controller
    {
        private static readonly Expression<Func<RiskData, RiskMeasurement>> MeasurementProjection = r => new RiskMeasurement
        {
            ID = r.ID,
            TimeStamp = r.TimeStamp,
            Gender = r.Gender,
            JobRole = r.JobRole,
            ExperienceYears = r.ExperienceYears,
            CompanySize = r.CompanySize,
            WorkHours = r.WorkHours,
            MeetingsPerDay = r.MeetingsPerDay,
            InternetSpeed = r.InternetSpeed,
            WorkEnvironment = r.WorkEnvironment,
            SleepHours = r.SleepHours,
            ExerciseHours = r.ExerciseHours,
            ScreenTime = r.ScreenTime,
            StressLevel = r.StressLevel,
            ProductivityScore = r.ProductivityScore,
            BurnoutRisk = r.BurnoutRisk,
            AISummary = r.Suggestion,
            MoodScore = r.MoodScore,
            DisconnectScore = r.DisconnectScore,
            FocusScore = r.FocusScore
        };

        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public AdminController(AppDbContext context, IConfiguration configuration, IServiceScopeFactory serviceScopeFactory)
        {
            _context = context;
            _configuration = configuration;
            _serviceScopeFactory = serviceScopeFactory;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard(string? q = null)
        {
            if (!IsCurrentUserAdmin())
            {
                return RedirectToAction("Index", "Home");
            }

            var query = _context.Users.AsNoTracking();
            var normalizedSearch = q?.Trim();

            if (!string.IsNullOrWhiteSpace(normalizedSearch))
            {
                var lowered = normalizedSearch.ToLower();
                var hasId = int.TryParse(normalizedSearch, out var searchedId);

                query = query.Where(user =>
                    (hasId && user.Id == searchedId)
                    || user.Email.ToLower().Contains(lowered)
                    || user.Username.ToLower().Contains(lowered));
            }

            var users = await query
                .OrderBy(user => user.Id)
                .Select(user => new AdminUserListItemViewModel
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    UserType = UserAccess.NormalizeUserType(user.UserType),
                    MeasurementCount = _context.RiskData.Count(r => r.UserId == user.Id),
                    LastMeasurementDate = _context.RiskData
                        .Where(r => r.UserId == user.Id)
                        .Select(r => (DateTime?)r.TimeStamp)
                        .Max()
                })
                .ToListAsync();

            ViewBag.SearchQuery = normalizedSearch ?? string.Empty;
            return View(users);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            if (!IsCurrentUserAdmin())
            {
                return RedirectToAction("Index", "Home");
            }

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            var country = user.CountryID.HasValue
                ? await _context.Countries
                    .AsNoTracking()
                    .Where(c => c.Id == user.CountryID.Value)
                    .Select(c => c.Name)
                    .FirstOrDefaultAsync()
                : null;

            var summary = await _context.RiskData
                .AsNoTracking()
                .Where(r => r.UserId == user.Id)
                .GroupBy(_ => 1)
                .Select(group => new
                {
                    Count = group.Count(),
                    LastMeasurementDate = group.Max(x => (DateTime?)x.TimeStamp)
                })
                .FirstOrDefaultAsync();

            var model = new AdminUserDetailsViewModel
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                UserType = UserAccess.NormalizeUserType(user.UserType),
                Gender = user.Gender,
                BirthDate = user.BirthDate,
                Country = country ?? string.Empty,
                JobRole = user.JobRole ?? string.Empty,
                ExperienceYears = user.ExperienceYears,
                CompanySize = user.CompanySize ?? string.Empty,
                WorkEnvironment = user.WorkEnvironment ?? string.Empty,
                MeasurementCount = summary?.Count ?? 0,
                LastMeasurementDate = summary?.LastMeasurementDate
            };

            return View("User", model);
        }

        [HttpGet]
        public async Task<IActionResult> History(int userId, int page = 1, string? risk = null, string? stress = null, string? period = null)
        {
            if (!IsCurrentUserAdmin())
            {
                return RedirectToAction("Index", "Home");
            }

            const int pageSize = 8;
            var userExists = await _context.Users.AsNoTracking().AnyAsync(u => u.Id == userId);
            if (!userExists)
            {
                return NotFound();
            }

            var measurementsQuery = _context.RiskData
                .AsNoTracking()
                .Where(r => r.UserId == userId);

            var totalMeasurementCount = await measurementsQuery.CountAsync();
            var riskFilter = NormalizeHistoryFilter(risk, "low", "medium", "high");
            var stressFilter = NormalizeHistoryFilter(stress, "low", "medium", "high");
            var periodFilter = NormalizeHistoryFilter(period, "7d", "30d", "3m", "1y");

            measurementsQuery = ApplyHistoryFilters(measurementsQuery, riskFilter, stressFilter, periodFilter);

            var filteredCount = await measurementsQuery.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(filteredCount / (double)pageSize));
            var currentPage = Math.Min(Math.Max(page, 1), totalPages);

            var measurements = await measurementsQuery
                .OrderByDescending(r => r.TimeStamp)
                .Select(MeasurementProjection)
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = currentPage;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = filteredCount;
            ViewBag.TotalMeasurementCount = totalMeasurementCount;
            ViewBag.PageSize = pageSize;
            ViewBag.RiskFilter = riskFilter;
            ViewBag.StressFilter = stressFilter;
            ViewBag.PeriodFilter = periodFilter;
            ViewBag.HasActiveHistoryFilters = riskFilter != "all" || stressFilter != "all" || periodFilter != "all";
            ViewBag.HistoryController = "Admin";
            ViewBag.HistoryAction = nameof(History);
            ViewBag.ResultAction = nameof(Measurement);
            ViewBag.HistoryUserId = userId;
            ViewBag.ReadOnlyHistory = true;
            ViewBag.HasPremiumFeatures = true;

            return View("~/Views/Profile/History.cshtml", measurements);
        }

        [HttpGet]
        public async Task<IActionResult> Analysis(int userId)
        {
            if (!IsCurrentUserAdmin())
            {
                return RedirectToAction("Index", "Home");
            }

            var userExists = await _context.Users.AsNoTracking().AnyAsync(u => u.Id == userId);
            if (!userExists)
            {
                return NotFound();
            }

            var measurements = await _context.RiskData
                .AsNoTracking()
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.TimeStamp)
                .Select(MeasurementProjection)
                .ToListAsync();

            ViewBag.HasAiAccess = true;
            ViewBag.AnalysisController = "Admin";
            ViewBag.AnalysisAction = nameof(Analysis);
            ViewBag.AnalysisUserId = userId;
            ViewBag.AnalysisEndpoint = Url.Action(nameof(GetAnalysis), "Admin", new { userId });

            return View("~/Views/Profile/Analysis.cshtml", measurements);
        }

        [HttpGet]
        public async Task<IActionResult> Measurement(int userId, int id)
        {
            if (!IsCurrentUserAdmin())
            {
                return RedirectToAction("Index", "Home");
            }

            var measurement = await _context.RiskData
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.UserId == userId && r.ID == id);

            if (measurement == null)
            {
                return NotFound();
            }

            var birthDate = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => u.BirthDate)
                .FirstOrDefaultAsync();

            ViewBag.HasAiAccess = true;
            ViewBag.ReadOnlyMeasurement = true;
            ViewBag.BackController = "Admin";
            ViewBag.BackAction = nameof(History);
            ViewBag.BackUserId = userId;
            ViewBag.AiSuggestionUrl = Url.Action(nameof(GetAiSuggestion), "Admin", new { userId, id });

            return View("~/Views/Profile/Result.cshtml", MapToMeasurement(measurement, birthDate));
        }

        [HttpGet]
        public async Task<IActionResult> GetAiSuggestion(int userId, int id)
        {
            if (!IsCurrentUserAdmin())
            {
                return Unauthorized();
            }

            var data = await _context.RiskData.FirstOrDefaultAsync(r => r.UserId == userId && r.ID == id);
            if (data == null) return NotFound();

            if (!string.IsNullOrEmpty(data.Suggestion))
            {
                return Json(new { suggestion = data.Suggestion });
            }

            var apiKey = GetGeminiApiKey();
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return Json(new { suggestion = "AI is not configured. Add BURNOUT_GEMINI_API to Hamburgerz/.env." });
            }

            var prompt = $@"You are the insight engine for a personal wellness app.

Entry data:
- Sleep: {data.SleepHours}h | Work: {data.WorkHours}h | Exercise: {data.ExerciseHours}h
- Screen time: {data.ScreenTime}h | Meetings/day: {data.MeetingsPerDay} | Stress: {data.StressLevel}

Write 4-5 short sentences in Lithuanian using informal ""tu"" form. Mention the notable pattern and two realistic actions. No medical claims.";

            try
            {
                var client = new Client(apiKey: apiKey);
                var text = await client.Models.GenerateContentAsync(model: "gemini-2.5-flash", contents: prompt);
                var result = text.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

                data.Suggestion = result ?? "Analysis complete, but no text generated.";
                await _context.SaveChangesAsync();

                return Json(new { suggestion = data.Suggestion });
            }
            catch (Exception)
            {
                return Json(new { suggestion = "AI is temporarily unavailable. Please refresh." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAnalysis(int userId)
        {
            if (!IsCurrentUserAdmin())
            {
                return Unauthorized();
            }

            var userExists = await _context.Users.AsNoTracking().AnyAsync(u => u.Id == userId);
            if (!userExists)
            {
                return NotFound();
            }

            var currentCount = await _context.RiskData.CountAsync(r => r.UserId == userId);
            if (currentCount == 0)
            {
                return Json(new { status = "empty" });
            }

            var requestedCulture = ProfileController.GetAnalysisCulture();
            var cached = await _context.AnalysisCache.FirstOrDefaultAsync(c => c.UserId == userId);
            if (cached != null && cached.MeasurementCount == currentCount)
            {
                if (ProfileController.TryReadAnalysisCache(cached.Content, requestedCulture, out var localizedPeriods, out var cultureStatus, out var cultureGeneratedAt, out var cultureErrorMessage))
                {
                    if (cultureStatus == "ready" && localizedPeriods != null)
                    {
                        return Json(new { status = "ready", periods = localizedPeriods });
                    }

                    if (cultureStatus == "generating" && cultureGeneratedAt.HasValue && DateTime.Now - cultureGeneratedAt.Value < TimeSpan.FromMinutes(10))
                    {
                        return Json(new { status = "generating" });
                    }

                    if (cultureStatus == "failed")
                    {
                        return Json(new { status = "error", message = cultureErrorMessage ?? "Generation failed." });
                    }
                }

                if (cached.Content == "__legacy_analysis_cache__" && cached.Status == "ready")
                {
                    try
                    {
                        var periods = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(cached.Content);
                        return Json(new { status = "ready", periods });
                    }
                    catch
                    {
                    }
                }
                if (cached.Status == "generating" && DateTime.Now - cached.GeneratedAt < TimeSpan.FromMinutes(10))
                {
                    return Json(new { status = "generating" });
                }
                else if (cached.Status == "failed")
                {
                    return Json(new { status = "error", message = cached.Content ?? "Generation failed." });
                }
            }

            var apiKey = GetGeminiApiKey();
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return Json(new { status = "error", message = ProfileController.GetAnalysisCulture() == "en-US" ? "AI is not configured." : "AI nėra sukonfigūruotas." });
            }

            if (cached == null)
            {
                cached = new AnalysisCache { UserId = userId };
                _context.AnalysisCache.Add(cached);
            }

            cached.MeasurementCount = currentCount;
            cached.GeneratedAt = DateTime.Now;
            cached.Status = "generating";
            cached.Content = ProfileController.UpsertAnalysisCacheContent(cached.Content, requestedCulture, "generating");
            await _context.SaveChangesAsync();

            var cacheId = cached.Id;
            var capturedApiKey = apiKey;
            var capturedCulture = requestedCulture;

            _ = Task.Run(async () =>
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                try
                {
                    var content = await ProfileController.BuildAnalysisContentAsync(userId, capturedApiKey, ctx, capturedCulture);
                    var record = await ctx.AnalysisCache.FindAsync(cacheId);
                    if (record != null)
                    {
                        record.Status = "ready";
                        record.Content = ProfileController.UpsertAnalysisCacheContent(record.Content, capturedCulture, "ready", content);
                        await ctx.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    var record = await ctx.AnalysisCache.FindAsync(cacheId);
                    if (record != null)
                    {
                        record.Status = "failed";
                        record.Content = ProfileController.UpsertAnalysisCacheContent(record.Content, capturedCulture, "failed", errorMessage: ex.Message);
                        await ctx.SaveChangesAsync();
                    }
                }
            });

            return Json(new { status = "generating" });
        }

        private bool IsCurrentUserAdmin() =>
            HttpContext.Session.IsAdmin();

        private string? GetGeminiApiKey()
        {
            var configuredApiKey = _configuration["BURNOUT_GEMINI_API"];

            if (!string.IsNullOrWhiteSpace(configuredApiKey))
            {
                return configuredApiKey;
            }

            configuredApiKey = _configuration["Gemini:ApiKey"];

            if (!string.IsNullOrWhiteSpace(configuredApiKey))
            {
                return configuredApiKey;
            }

            return Environment.GetEnvironmentVariable("BURNOUT_GEMINI_API", EnvironmentVariableTarget.User);
        }

        private static RiskMeasurement MapToMeasurement(RiskData measurement, DateTime? birthDate = null)
        {
            return new RiskMeasurement
            {
                ID = measurement.ID,
                TimeStamp = measurement.TimeStamp,
                BirthDate = birthDate,
                Gender = measurement.Gender,
                JobRole = measurement.JobRole,
                ExperienceYears = measurement.ExperienceYears,
                CompanySize = measurement.CompanySize,
                WorkHours = measurement.WorkHours,
                MeetingsPerDay = measurement.MeetingsPerDay,
                InternetSpeed = measurement.InternetSpeed,
                WorkEnvironment = measurement.WorkEnvironment,
                SleepHours = measurement.SleepHours,
                ExerciseHours = measurement.ExerciseHours,
                ScreenTime = measurement.ScreenTime,
                StressLevel = measurement.StressLevel,
                ProductivityScore = measurement.ProductivityScore,
                BurnoutRisk = measurement.BurnoutRisk,
                AISummary = measurement.Suggestion,
                MoodScore = measurement.MoodScore,
                DisconnectScore = measurement.DisconnectScore,
                FocusScore = measurement.FocusScore
            };
        }

        private static string NormalizeHistoryFilter(string? value, params string[] allowedValues)
        {
            var normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
            return allowedValues.Contains(normalized) ? normalized : "all";
        }

        private static IQueryable<RiskData> ApplyHistoryFilters(
            IQueryable<RiskData> query,
            string riskFilter,
            string stressFilter,
            string periodFilter)
        {
            query = riskFilter switch
            {
                "low" => query.Where(r =>
                    (r.BurnoutRisk <= 1f && r.BurnoutRisk < 0.4f)
                    || (r.BurnoutRisk > 1f && r.BurnoutRisk < 40f)),
                "medium" => query.Where(r =>
                    (r.BurnoutRisk <= 1f && r.BurnoutRisk >= 0.4f && r.BurnoutRisk < 0.7f)
                    || (r.BurnoutRisk > 1f && r.BurnoutRisk >= 40f && r.BurnoutRisk < 70f)),
                "high" => query.Where(r =>
                    (r.BurnoutRisk <= 1f && r.BurnoutRisk >= 0.7f)
                    || (r.BurnoutRisk > 1f && r.BurnoutRisk >= 70f)),
                _ => query
            };

            query = stressFilter switch
            {
                "low" => query.Where(r => r.StressLevel.Contains("Žem") || r.StressLevel.Contains("Zem") || r.StressLevel.Contains("Low")),
                "medium" => query.Where(r => r.StressLevel.Contains("Vid") || r.StressLevel.Contains("Med")),
                "high" => query.Where(r => r.StressLevel.Contains("Auk") || r.StressLevel.Contains("High")),
                _ => query
            };

            var periodStart = periodFilter switch
            {
                "7d" => DateTime.Now.AddDays(-7),
                "30d" => DateTime.Now.AddDays(-30),
                "3m" => DateTime.Now.AddMonths(-3),
                "1y" => DateTime.Now.AddYears(-1),
                _ => (DateTime?)null
            };

            if (periodStart.HasValue)
            {
                query = query.Where(r => r.TimeStamp >= periodStart.Value);
            }

            return query;
        }
    }
}

using System.Linq.Expressions;
using Google.GenAI;
using Hamburgerz.Data;
using Hamburgerz.Models;
using Hamburgerz.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Hamburgerz.Controllers
{
    public class ProfileController : Controller
    {
        private const long MaxAvatarSizeBytes = 5 * 1024 * 1024;

        private static readonly HashSet<string> AllowedAvatarContentTypes =
        [
            "image/jpeg",
            "image/png",
            "image/webp",
            "image/gif",
            "image/bmp"
        ];

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
        private readonly JobRoleCatalogService _jobRoleCatalog;

        public ProfileController(AppDbContext context, IConfiguration configuration, IServiceScopeFactory serviceScopeFactory, JobRoleCatalogService jobRoleCatalog)
        {
            _context = context;
            _configuration = configuration;
            _serviceScopeFactory = serviceScopeFactory;
            _jobRoleCatalog = jobRoleCatalog;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
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

            var model = await BuildProfilePageViewModelAsync(user);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ProfilePageViewModel model)
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

            if (model.CountryID.HasValue)
            {
                var countryExists = await _context.Countries.AnyAsync(country => country.Id == model.CountryID.Value);
                if (!countryExists)
                {
                    ModelState.AddModelError(nameof(model.CountryID), "Pasirinkite tinkamą šalį.");
                }
            }

            if (model.BirthDate.HasValue && !IsBirthDateInAllowedRange(model.BirthDate.Value))
            {
                ModelState.AddModelError(nameof(model.BirthDate), "Choose a valid birth date.");
            }

            string? resolvedJobRole = null;
            if (!string.IsNullOrWhiteSpace(model.JobRole))
            {
                resolvedJobRole = _jobRoleCatalog.TryResolveCanonicalTitle(model.JobRole);
                var isKeepingLegacyJobRole =
                    resolvedJobRole == null
                    && string.Equals(
                        model.JobRole.Trim(),
                        user.JobRole?.Trim(),
                        StringComparison.OrdinalIgnoreCase);

                if (resolvedJobRole == null && !isKeepingLegacyJobRole)
                {
                    ModelState.AddModelError(nameof(model.JobRole), "Select a job role from the suggestion list.");
                }

                if (isKeepingLegacyJobRole)
                {
                    resolvedJobRole = user.JobRole?.Trim();
                }
            }

            if (!ModelState.IsValid)
            {
                var invalidModel = await BuildProfilePageViewModelAsync(user, model);
                return View(invalidModel);
            }

            user.BirthDate = NormalizeBirthDate(model.BirthDate);
            user.CountryID = model.CountryID;
            user.JobRole = resolvedJobRole;
            user.ExperienceYears = model.ExperienceYears;
            user.CompanySize = NormalizeOptionalText(model.CompanySize);
            user.WorkEnvironment = NormalizeOptionalText(model.WorkEnvironment);

            await _context.SaveChangesAsync();

            TempData["ProfileSuccess"] = "Profilio duomenys atnaujinti.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("Profile/Avatar")]
        public async Task<IActionResult> Avatar(long? v = null)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return NotFound();
            }

            var avatar = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == userId.Value)
                .Select(u => new
                {
                    u.ProfileImage,
                    u.ProfileImageType
                })
                .FirstOrDefaultAsync();

            if (avatar?.ProfileImage == null || avatar.ProfileImage.Length == 0)
            {
                return NotFound();
            }

            var contentType = string.IsNullOrWhiteSpace(avatar.ProfileImageType)
                ? "image/jpeg"
                : avatar.ProfileImageType;

            return File(avatar.ProfileImage, contentType);
        }

        [HttpPost("Profile/UploadAvatar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAvatar(IFormFile? avatar)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Unauthorized(new { message = "Prisijunkite ir bandykite dar kartą." });
            }

            if (avatar == null || avatar.Length == 0)
            {
                return BadRequest(new { message = "Pasirinkite paveikslėlį." });
            }

            if (avatar.Length > MaxAvatarSizeBytes)
            {
                return BadRequest(new { message = "Paveikslėlis per didelis." });
            }

            if (!AllowedAvatarContentTypes.Contains(avatar.ContentType))
            {
                return BadRequest(new { message = "Nepalaikomas paveikslėlio formatas." });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId.Value);
            if (user == null)
            {
                return Unauthorized(new { message = "Vartotojas nerastas." });
            }

            await using var stream = new MemoryStream();
            await avatar.CopyToAsync(stream);

            user.ProfileImage = stream.ToArray();
            user.ProfileImageType = avatar.ContentType;

            await _context.SaveChangesAsync();

            return Json(new
            {
                avatarUrl = Url.Action(nameof(Avatar), new
                {
                    v = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                })
            });
        }

        [HttpGet]
        public async Task<IActionResult> History(int page = 1)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            const int pageSize = 8;

            if (userId == null)
            {
                return RedirectToAction("Index", "Login");
            }

            var measurementsQuery = _context.RiskData
                .Where(r => r.UserId == userId.Value);

            var totalCount = await measurementsQuery.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
            var currentPage = Math.Min(Math.Max(page, 1), totalPages);

            var measurements = await measurementsQuery
                .OrderByDescending(r => r.TimeStamp)
                .Select(MeasurementProjection)
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = currentPage;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;
            ViewBag.PageSize = pageSize;

            return View(measurements);
        }

        [HttpGet]
        public async Task<IActionResult> Analysis()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction("Index", "Login");
            }

            var measurements = await _context.RiskData
                .Where(r => r.UserId == userId.Value)
                .OrderByDescending(r => r.TimeStamp)
                .Select(MeasurementProjection)
                .ToListAsync();

            return View(measurements);
        }

        [HttpGet]
        public async Task<IActionResult> Result(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction("Index", "Login");
            }

            var measurement = await _context.RiskData
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.UserId == userId.Value && r.ID == id);

            if (measurement == null)
            {
                return NotFound();
            }

            var birthDate = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == userId.Value)
                .Select(u => u.BirthDate)
                .FirstOrDefaultAsync();

            return View(MapToMeasurement(measurement, birthDate));
        }

        [HttpGet]
        public async Task<IActionResult> GetAiSuggestion(int id)
        {
            var data = await _context.RiskData.FindAsync(id);
            if (data == null) return NotFound();

            // If we already generated it, just return it
            if (!string.IsNullOrEmpty(data.Suggestion))
            {
                return Json(new { suggestion = data.Suggestion });
            }

            var apiKey = GetGeminiApiKey();

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return Json(new { suggestion = "AI is not configured. Add BURNOUT_GEMINI_API to Hamburgerz/.env." });
            }

            var client = new Client(apiKey: apiKey);

            var moodText = data.MoodScore switch {
                4 => "puikiai (4/4)", 3 => "normaliai (3/4)", 2 => "pavargęs (2/4)", 1 => "tikrai sunkiai (1/4)", _ => "nenurodyta"
            };
            var disconnectText = data.DisconnectScore switch {
                3 => "atsijungė lengvai (3/3)", 2 => "iš dalies (2/3)", 1 => "sunkiai pavyko (1/3)", _ => "nenurodyta"
            };
            var focusText = data.FocusScore switch {
                3 => "lengvai (3/3)", 2 => "šiaip taip (2/3)", 1 => "labai sunkiai (1/3)", _ => "nenurodyta"
            };

            var prompt = "";
            if(System.Globalization.CultureInfo.CurrentUICulture.Name == "lt-LT")
            {
                Console.WriteLine("Eta lietuviu fr fr");
                prompt = $@"You are the insight engine for a personal wellness app.

Entry data:
- Sleep: {data.SleepHours}h | Work: {data.WorkHours}h | Exercise: {data.ExerciseHours}h
- Screen time: {data.ScreenTime}h | Meetings/day: {data.MeetingsPerDay} | Stress: {data.StressLevel}
- Mood: {moodText} | Disconnect after work: {disconnectText} | Focus: {focusText}

Write 4-5 sentences in Lithuanian (use ""tu"" form, informal):
1-3. The most notable patterns in this data — reference actual numbers, explain what they mean for energy and recovery.
4-5. Two specific, realistic actions.

No intro, no headers, no job/profession references, no medical claims. Calm, direct, personal.";
            }
            else if (System.Globalization.CultureInfo.CurrentUICulture.Name == "en-US")
            {
                Console.WriteLine("Eta Anglu fr fr");
                prompt = $@"You are the insight engine for a personal wellness app.

Entry data:
- Sleep: {data.SleepHours}h | Work: {data.WorkHours}h | Exercise: {data.ExerciseHours}h
- Screen time: {data.ScreenTime}h | Meetings/day: {data.MeetingsPerDay} | Stress: {data.StressLevel}
- Mood: {moodText} | Disconnect after work: {disconnectText} | Focus: {focusText}

Write 4-5 sentences in English (use ""you"" form, informal):
1-3. The most notable patterns in this data — reference actual numbers, explain what they mean for energy and recovery.
4-5. Two specific, realistic actions.

No intro, no headers, no job/profession references, no medical claims. Calm, direct, personal.";
                
            }

                

            try
            {
                var text = await client.Models.GenerateContentAsync(model: "gemini-2.5-flash", contents: prompt);
                var result = text.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

                // Save to DB so it's permanent
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
        public async Task<IActionResult> GetAnalysis()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Unauthorized();

            var currentCount = await _context.RiskData.CountAsync(r => r.UserId == userId.Value);
            if (currentCount == 0)
                return Json(new { status = "empty" });

            var cached = await _context.AnalysisCache
                .FirstOrDefaultAsync(c => c.UserId == userId.Value);

            if (cached != null && cached.MeasurementCount == currentCount)
            {
                if (cached.Status == "ready" && !string.IsNullOrEmpty(cached.Content))
                {
                    try
                    {
                        var periods = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(cached.Content);
                        return Json(new { status = "ready", periods });
                    }
                    catch { /* malformed — fall through to regenerate */ }
                }
                else if (cached.Status == "generating" && DateTime.Now - cached.GeneratedAt < TimeSpan.FromMinutes(10))
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
                return Json(new { status = "error", message = "AI nera sukonfigūruotas." });

            if (cached == null)
            {
                cached = new AnalysisCache { UserId = userId.Value };
                _context.AnalysisCache.Add(cached);
            }
            cached.MeasurementCount = currentCount;
            cached.GeneratedAt = DateTime.Now;
            cached.Status = "generating";
            cached.Content = null;
            await _context.SaveChangesAsync();

            var cacheId = cached.Id;
            var capturedUserId = userId.Value;
            var capturedApiKey = apiKey;

            _ = Task.Run(async () =>
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                try
                {
                    var content = await BuildAnalysisContentAsync(capturedUserId, capturedApiKey, ctx);
                    var record = await ctx.AnalysisCache.FindAsync(cacheId);
                    if (record != null)
                    {
                        record.Status = "ready";
                        record.Content = content;
                        await ctx.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    var record = await ctx.AnalysisCache.FindAsync(cacheId);
                    if (record != null)
                    {
                        record.Status = "failed";
                        record.Content = ex.Message;
                        await ctx.SaveChangesAsync();
                    }
                }
            });

            return Json(new { status = "generating" });
        }

        private static async Task<string> BuildAnalysisContentAsync(int userId, string apiKey, AppDbContext ctx)
        {
            var allItems = await ctx.RiskData
                .Where(r => r.UserId == userId)
                .OrderBy(r => r.TimeStamp)
                .ToListAsync();

            var now = DateTime.Now;
            var windows = new (string key, DateTime? from)[]
            {
                ("7d",  now.AddDays(-7)),
                ("30d", now.AddDays(-30)),
                ("3m",  now.AddMonths(-3)),
                ("6m",  now.AddMonths(-6)),
                ("1y",  now.AddYears(-1)),
                ("all", null)
            };

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("User lifestyle data by time window (personal wellness app):");
            sb.AppendLine();
            sb.AppendLine("Score scales (use these to describe feelings, not raw numbers):");
            sb.AppendLine("  mood 1-4:        1=had a rough day, 2=felt tired, 3=felt ok, 4=felt great");
            sb.AppendLine("  disconnect 1-3:  1=struggled to stop thinking about work, 2=partially switched off, 3=switched off easily");
            sb.AppendLine("  focus 1-3:       1=very hard to focus, 2=manageable, 3=focused easily");
            sb.AppendLine();

            foreach (var (key, from) in windows)
            {
                var items = from.HasValue
                    ? allItems.Where(x => x.TimeStamp >= from.Value).ToList()
                    : allItems;

                sb.Append($"[{key}]");
                if (items.Count == 0) { sb.AppendLine(" no data"); continue; }

                var avgSleep    = items.Average(x => (double)x.SleepHours);
                var minSleep    = items.Min(x => (double)x.SleepHours);
                var maxSleep    = items.Max(x => (double)x.SleepHours);
                var avgWork     = items.Average(x => (double)x.WorkHours);
                var minWork     = items.Min(x => (double)x.WorkHours);
                var maxWork     = items.Max(x => (double)x.WorkHours);
                var avgExercise = items.Average(x => (double)x.ExerciseHours);
                var avgScreen   = items.Average(x => (double)x.ScreenTime);
                var hiPct       = (int)Math.Round(items.Count(x => (x.StressLevel ?? "").Contains("Aukštas"))   * 100.0 / items.Count);
                var midPct      = (int)Math.Round(items.Count(x => (x.StressLevel ?? "").Contains("Vidutinis")) * 100.0 / items.Count);

                sb.AppendLine($" {items.Count} entries | sleep avg={avgSleep:0.1}h min={minSleep:0.1}h max={maxSleep:0.1}h | work avg={avgWork:0.1}h min={minWork:0.1}h max={maxWork:0.1}h | exercise avg={avgExercise:0.1}h | screen avg={avgScreen:0.1}h | stress {hiPct}% high {midPct}% medium");

                var moods = items.Where(x => x.MoodScore.HasValue).ToList();
                var discs = items.Where(x => x.DisconnectScore.HasValue).ToList();
                var focs  = items.Where(x => x.FocusScore.HasValue).ToList();
                if (moods.Count > 0 || discs.Count > 0 || focs.Count > 0)
                {
                    sb.Append("      ");
                    if (moods.Count > 0) sb.Append($"mood avg={moods.Average(x => (double)x.MoodScore!.Value):0.1}/4 ");
                    if (discs.Count > 0) sb.Append($"disconnect avg={discs.Average(x => (double)x.DisconnectScore!.Value):0.1}/3 ");
                    if (focs.Count > 0)  sb.Append($"focus avg={focs.Average(x => (double)x.FocusScore!.Value):0.1}/3");
                    sb.AppendLine();
                }
            }
            if (System.Globalization.CultureInfo.CurrentUICulture.Name == "lt-LT")
            {
                sb.Append(@"
You are the insight engine for a personal wellness app. Turn the data above into short, personal insights.

TONE: Speak directly to the user as ""you"" (Lithuanian ""tu"" form). Calm, warm, no drama.
STYLE:
  insight = 3-5 sentences. Start with the most notable pattern, then explain what it means for energy and recovery, then compare values or highlight a trend if relevant.
  action = 2 concrete, specific suggestions (2 sentences). Reference the actual data.

RULES:
- Use real numbers for sleep/work/exercise/screen (e.g. ""your average sleep was 6.1h"")
- For mood/disconnect/focus: NEVER say ""avg 2.8/4"" — always describe using the scale (e.g. ""you mostly felt ok, sometimes tired"")
- Each period's insight must feel distinct — focus on what's unique about that time window
- NEVER repeat the same sentence structure, opening phrase, or pattern across periods — vary vocabulary, rhythm, and angle
- Shorter windows = focus on recent signals; longer windows = focus on trends and patterns over time
- No profession or job role references
- No medical claims
- No fear or alarm language
- Use null for any period with no data

Return ONLY this JSON, no markdown, no explanation:
{""7d"":{""insight"":""..."",""action"":""...""},""30d"":{""insight"":""..."",""action"":""...""},""3m"":{""insight"":""..."",""action"":""...""},""6m"":{""insight"":""..."",""action"":""...""},""1y"":{""insight"":""..."",""action"":""...""},""all"":{""insight"":""..."",""action"":""...""}}

Good insight example: Tavo miegas siа savaitę vidurkis buvo 6.1h — gerokai mažiau nei rekomenduojama 7h. Kartu darbo valandos siekė vidutiniškai 10.4h, o ekrano laikas — 8.2h, kas palieka labai mažai laiko tikram atsigavimui. Žemiausia miego reikšmė buvo 5.5h, kas rodo, kad bent kelios naktys buvo tikrai trumpos.
Good action example: Pabandyk vieną savaitės vakarą nustatyti miego laikmatį 30 min. anksčiau nei įprastai. Ekrano laiką po 21:00 sumažink bent per pusę — tai ženkliai pagerina užmigimą.

Bad example (avoid):
insight: Sis darbuotojas rodo lėtinio perdegimo požymius dėl per ilgų darbo valandų.
action: Rekomenduojama kreiptis į gydytoją.

ONCE AGAIN, ALL IN LITHUANIAN!!");
            }
            else if (System.Globalization.CultureInfo.CurrentUICulture.Name == "en-US")
            {
                sb.Append(@"
You are the insight engine for a personal wellness app. Turn the data above into short, personal insights.

TONE: Speak directly to the user as ""you"". Calm, warm, no drama.
STYLE:
  insight = 3-5 sentences. Start with the most notable pattern, then explain what it means for energy and recovery, then compare values or highlight a trend if relevant.
  action = 2 concrete, specific suggestions (2 sentences). Reference the actual data.

RULES:
- Use real numbers for sleep/work/exercise/screen (e.g. ""your average sleep was 6.1h"")
- For mood/disconnect/focus: NEVER say ""avg 2.8/4"" — always describe using the scale (e.g. ""you mostly felt ok, sometimes tired"")
- Each period's insight must feel distinct — focus on what's unique about that time window
- NEVER repeat the same sentence structure, opening phrase, or pattern across periods — vary vocabulary, rhythm, and angle
- Shorter windows = focus on recent signals; longer windows = focus on trends and patterns over time
- No profession or job role references
- No medical claims
- No fear or alarm language
- Use null for any period with no data

Return ONLY this JSON, no markdown, no explanation:
{""7d"":{""insight"":""..."",""action"":""...""},""30d"":{""insight"":""..."",""action"":""...""},""3m"":{""insight"":""..."",""action"":""...""},""6m"":{""insight"":""..."",""action"":""...""},""1y"":{""insight"":""..."",""action"":""...""},""all"":{""insight"":""..."",""action"":""...""}}

Good insight example: Your average sleep this week was 6.1 hours—well below the recommended 7 hours. At the same time, your average work hours were 10.4 hours, and your screen time was 8.2 hours, which leaves very little time for proper rest. The lowest sleep duration was 5.5 hours, indicating that at least a few nights were really short.
Good action example: Try setting your sleep timer 30 minutes earlier than usual one evening this week. Cut your screen time after 9:00 PM by at least half—this significantly improves your ability to fall asleep.

Bad example (avoid):
insight: This employee is showing signs of chronic burnout due to excessive working hours.
action: It is recommended that they see a doctor.");
            }

            var client = new Client(apiKey: apiKey);
            var response = await client.Models.GenerateContentAsync(model: "gemini-2.5-flash", contents: sb.ToString());
            var raw = response.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? "";

            raw = raw.Trim();
            if (raw.StartsWith("```"))
                raw = System.Text.RegularExpressions.Regex.Replace(raw, @"```[a-z]*\n?", "").Replace("```", "").Trim();

            System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(raw);
            return raw;
        }

        [HttpPost("Profile/UpdateTimestamp")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTimestamp(int id, DateTime? timeStamp, string? originalTime = null)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction("Index", "Login");
            }

            if (!ModelState.IsValid || timeStamp == null)
            {
                TempData["MeasurementError"] = "Please select a valid date.";
                return RedirectToAction(nameof(Result), new { id });
            }

            var measurement = await _context.RiskData
                .FirstOrDefaultAsync(r => r.UserId == userId.Value && r.ID == id);

            if (measurement == null)
            {
                return NotFound();
            }

            var updatedTimestamp = timeStamp.Value;

            if (!string.IsNullOrWhiteSpace(originalTime)
                && updatedTimestamp.TimeOfDay == TimeSpan.Zero
                && TimeSpan.TryParse(originalTime, out var preservedTime))
            {
                updatedTimestamp = updatedTimestamp.Date.Add(preservedTime);
            }

            measurement.TimeStamp = updatedTimestamp;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Result), new { id });
        }

        [HttpPost("Profile/DeleteMeasurement")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMeasurement(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction("Index", "Login");
            }

            var measurement = await _context.RiskData
                .FirstOrDefaultAsync(r => r.UserId == userId.Value && r.ID == id);

            if (measurement == null)
            {
                return NotFound();
            }

            _context.RiskData.Remove(measurement);
            await _context.SaveChangesAsync();

            TempData["MeasurementSuccess"] = "Measurement deleted.";
            return RedirectToAction(nameof(History));
        }

        private async Task<ProfilePageViewModel> BuildProfilePageViewModelAsync(User user, ProfilePageViewModel? submittedModel = null)
        {
            var effectiveCountryId = submittedModel == null ? user.CountryID : submittedModel.CountryID;
            var countryName = effectiveCountryId.HasValue
                ? await _context.Countries
                    .AsNoTracking()
                    .Where(country => country.Id == effectiveCountryId.Value)
                    .Select(country => country.Name)
                    .FirstOrDefaultAsync()
                : null;

            var measurementSummary = await _context.RiskData
                .AsNoTracking()
                .Where(r => r.UserId == user.Id)
                .GroupBy(_ => 1)
                .Select(group => new
                {
                    Count = group.Count(),
                    LastMeasurementDate = group.Max(x => (DateTime?)x.TimeStamp)
                })
                .FirstOrDefaultAsync();

            var model = new ProfilePageViewModel
            {
                Username = user.Username,
                Email = user.Email,
                Gender = user.Gender,
                BirthDate = submittedModel == null ? user.BirthDate : NormalizeBirthDate(submittedModel.BirthDate),
                CountryID = effectiveCountryId,
                Country = countryName ?? string.Empty,
                JobRole = submittedModel == null ? user.JobRole ?? string.Empty : submittedModel.JobRole ?? string.Empty,
                ExperienceYears = submittedModel == null ? user.ExperienceYears : submittedModel.ExperienceYears,
                CompanySize = submittedModel == null ? user.CompanySize ?? string.Empty : submittedModel.CompanySize ?? string.Empty,
                WorkEnvironment = submittedModel == null ? user.WorkEnvironment ?? string.Empty : submittedModel.WorkEnvironment ?? string.Empty,
                MeasurementCount = measurementSummary?.Count ?? 0,
                LastMeasurementDate = measurementSummary?.LastMeasurementDate
            };

            await PopulateCountriesAsync(model);
            return model;
        }

        private async Task PopulateCountriesAsync(ProfilePageViewModel model)
        {
            if (System.Globalization.CultureInfo.CurrentUICulture.Name == "lt-LT")
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
            else if (System.Globalization.CultureInfo.CurrentUICulture.Name == "en-US")
            {
                model.Countries = await _context.CountriesEN
                    .AsNoTracking()
                    .OrderBy(country => country.Name)
                    .Select(country => new SelectListItem
                    {
                        Value = country.Id.ToString(),
                        Text = country.Name
                    })
                    .ToListAsync();
            }

                
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
                AISummary = measurement.Suggestion
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

        private static DateTime? NormalizeBirthDate(DateTime? birthDate) =>
            birthDate?.Date;

        private static bool IsBirthDateInAllowedRange(DateTime birthDate)
        {
            var today = DateTime.Today;
            var normalizedBirthDate = birthDate.Date;
            return normalizedBirthDate >= today.AddYears(-100) && normalizedBirthDate <= today.AddYears(-14);
        }
    }
}

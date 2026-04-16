using System.Linq.Expressions;
using Hamburgerz.Data;
using Hamburgerz.Models;
using Hamburgerz.Services;
using Microsoft.AspNetCore.Mvc;
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
            BurnoutRisk = r.BurnoutRisk
        };

        private readonly AppDbContext _context;
        private readonly JobRoleCatalogService _jobRoleCatalog;

        public ProfileController(AppDbContext context, JobRoleCatalogService jobRoleCatalog)
        {
            _context = context;
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
                BurnoutRisk = measurement.BurnoutRisk
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

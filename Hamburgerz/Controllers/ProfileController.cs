using Hamburgerz.Data;
using Hamburgerz.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hamburgerz.Controllers
{
    public class ProfileController : Controller
    {
        private readonly AppDbContext _context;

        public ProfileController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction("Login", "Login");
            }

            var sessionUsername = HttpContext.Session.GetString("Username") ?? string.Empty;
            var sessionEmail = HttpContext.Session.GetString("Email") ?? string.Empty;

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId.Value);

            var userCountry = user?.CountryID is int countryId
                ? await _context.Countries
                    .AsNoTracking()
                    .Where(country => country.Id == countryId)
                    .Select(country => country.Name)
                    .FirstOrDefaultAsync()
                : null;

            var measurementsQuery = _context.RiskData
                .AsNoTracking()
                .Where(r => r.UserId == userId.Value);

            var latestMeasurement = await measurementsQuery
                .OrderByDescending(r => r.TimeStamp)
                .FirstOrDefaultAsync();

            var measurementCount = await measurementsQuery.CountAsync();

            var model = new ProfilePageViewModel
            {
                Username = !string.IsNullOrWhiteSpace(user?.Username) ? user.Username : sessionUsername,
                Email = !string.IsNullOrWhiteSpace(user?.Email) ? user.Email : sessionEmail,
                Gender = !string.IsNullOrWhiteSpace(user?.Gender) ? user.Gender : (latestMeasurement?.Gender ?? string.Empty),
                Age = latestMeasurement?.Age,
                Country = !string.IsNullOrWhiteSpace(userCountry) ? userCountry : (latestMeasurement?.Country ?? string.Empty),
                JobRole = latestMeasurement?.JobRole ?? string.Empty,
                ExperienceYears = latestMeasurement?.ExperienceYears,
                CompanySize = latestMeasurement?.CompanySize ?? string.Empty,
                WorkEnvironment = latestMeasurement?.WorkEnvironment ?? string.Empty,
                InternetSpeed = latestMeasurement?.InternetSpeed,
                MeasurementCount = measurementCount,
                LastMeasurementDate = latestMeasurement?.TimeStamp
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> History(int page = 1)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            const int pageSize = 8;

            if (userId == null)
            {
                return RedirectToAction("Login", "Login");
            }

            var measurementsQuery = _context.RiskData
                .Where(r => r.UserId == userId.Value);

            var totalCount = await measurementsQuery.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
            var currentPage = Math.Min(Math.Max(page, 1), totalPages);

            var measurements = await measurementsQuery
                .OrderByDescending(r => r.TimeStamp)
                .Select(r => new RiskMeasurement
                {
                    ID = r.ID,
                    TimeStamp = r.TimeStamp,
                    Age = r.Age,
                    Gender = r.Gender,
                    Country = r.Country,
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
                })
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
                return RedirectToAction("Login", "Login");
            }

            var measurements = await _context.RiskData
                .Where(r => r.UserId == userId.Value)
                .OrderByDescending(r => r.TimeStamp)
                .Select(r => new RiskMeasurement
                {
                    ID = r.ID,
                    TimeStamp = r.TimeStamp,
                    Age = r.Age,
                    Gender = r.Gender,
                    Country = r.Country,
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
                })
                .ToListAsync();

            return View(measurements);
        }

        [HttpGet]
        public async Task<IActionResult> Result(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction("Login", "Login");
            }

            var measurement = await _context.RiskData
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.UserId == userId.Value && r.ID == id);

            if (measurement == null)
            {
                return NotFound();
            }

            return View(MapToMeasurement(measurement));
        }

        [HttpPost("Profile/UpdateTimestamp")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTimestamp(int id, DateTime? timeStamp, string? originalTime = null)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction("Login", "Login");
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
                return RedirectToAction("Login", "Login");
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

        private static RiskMeasurement MapToMeasurement(RiskData r)
        {
            return new RiskMeasurement
            {
                ID = r.ID,
                TimeStamp = r.TimeStamp,
                Age = r.Age,
                Gender = r.Gender,
                Country = r.Country,
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
        }
    }
}

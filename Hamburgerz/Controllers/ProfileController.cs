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

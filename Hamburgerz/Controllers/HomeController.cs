using System.Diagnostics;
using Hamburgerz.Data;
using Hamburgerz.Helpers;
using Hamburgerz.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hamburgerz.Controllers
{
    public class HomeController : Controller
    {
        private static readonly string[] BurnoutFacts =
        [
            "Perdegimas dažniausiai neatsiranda staiga, o kaupiasi per mažus pasikartojančius signalus, todėl reguliarūs trumpi matavimai dažnai pasako daugiau nei vienas pavienis įrašas.",
            "Kai miegas trumpesnis kelias dienas iš eilės, streso ir produktyvumo pokyčiai neretai pasimato anksčiau nei pradedi tai aiškiai jausti darbo dienoje.",
            "Net keli trumpi check-in per savaitę gali padėti daug greičiau pamatyti rutiną nei vienas didelis matavimas kartą per mėnesį.",
            "Istorija tampa naudingiausia tada, kai matavimai daromi nuosekliai, nes tada analysis pradeda rodyti ne atsitiktinius taškus, o tikras tendencijas."
        ];

        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;

        public HomeController(ILogger<HomeController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var model = new HomePageViewModel
            {
                IsLoggedIn = HttpContext.Session.IsLoggedIn(),
                Username = HttpContext.Session.GetUsername() ?? string.Empty,
                UserType = HttpContext.Session.GetUserType() ?? string.Empty
            };

            if (!model.IsLoggedIn)
            {
                return View(model);
            }

            var userId = HttpContext.Session.GetUserId();
            if (userId == null)
            {
                return View(model);
            }

            var today = DateTime.Today;
            var weekStart = today.AddDays(-6);
            var monthStart = new DateTime(today.Year, today.Month, 1);

            var measurementsQuery = _context.RiskData
                .AsNoTracking()
                .Where(r => r.UserId == userId.Value);

            model.MeasurementCount = await measurementsQuery.CountAsync();
            model.MeasurementsThisWeek = await measurementsQuery.CountAsync(r => r.TimeStamp >= weekStart);
            model.MeasurementsThisMonth = await measurementsQuery.CountAsync(r => r.TimeStamp >= monthStart);

            model.LatestMeasurement = await measurementsQuery
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
                .FirstOrDefaultAsync();

            model.LastMeasurementDate = model.LatestMeasurement?.TimeStamp;

            if (model.LastMeasurementDate.HasValue)
            {
                model.DaysSinceLastMeasurement = (today - model.LastMeasurementDate.Value.Date).Days;
                model.HasMeasurementToday = model.LastMeasurementDate.Value.Date == today;
            }

            model.FactTitle = "Burnout fact";
            model.FactText = GetBurnoutFact(userId.Value, model.MeasurementCount, today.DayOfYear);

            ApplyStatus(model);

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private static void ApplyStatus(HomePageViewModel model)
        {
            if (model.LastMeasurementDate == null)
            {
                model.StatusTitle = "Dar neturite nė vieno matavimo";
                model.StatusText = "Pasidarykite savo pirmą matavimą.";
                model.StatusClass = "status-empty";
                return;
            }

            if (model.HasMeasurementToday)
            {
                model.StatusTitle = "Šiandienos matavimas jau atliktas";
                model.StatusText = $"Paskutinis matavimas atliktas {model.LastMeasurementDate.Value:HH:mm}.";
                model.StatusClass = "status-fresh";
                return;
            }

            var relativeLabel = model.DaysSinceLastMeasurement == 1
                ? "vakar"
                : $"prieš {model.DaysSinceLastMeasurement} dienas";

            model.StatusTitle = "Šiandien dar neturite matavimo";
            model.StatusText = $"Paskutinis matavimas buvo {relativeLabel}, todėl verta pasidaryti naują matavimą ir atsinaujinti statistiką.";
            model.StatusClass = "status-stale";
        }

        private static string GetBurnoutFact(int userId, int measurementCount, int dayOfYear)
        {
            var seed = (uint)HashCode.Combine(userId, measurementCount, dayOfYear);
            return BurnoutFacts[(int)(seed % BurnoutFacts.Length)];
        }
    }
}

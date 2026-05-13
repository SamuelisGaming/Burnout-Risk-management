using System.Globalization;

namespace Hamburgerz.Models
{
    public class HomePageViewModel
    {
        public bool IsLoggedIn { get; set; }
        public string Username { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;

        public int MeasurementCount { get; set; }
        public int MeasurementsThisWeek { get; set; }
        public int MeasurementsThisMonth { get; set; }
        public int? MeasurementLimit { get; set; }
        public bool CanCreateMeasurement { get; set; } = true;
        public bool HasPremiumFeatures { get; set; }

        public DateTime? LastMeasurementDate { get; set; }
        public bool HasMeasurementToday { get; set; }
        public int? DaysSinceLastMeasurement { get; set; }

        public RiskMeasurement? LatestMeasurement { get; set; }

        public string StatusTitle { get; set; } = string.Empty;
        public string StatusText { get; set; } = string.Empty;
        public string StatusClass { get; set; } = "status-empty";

        public string FactTitle { get; set; } = string.Empty;
        public string FactText { get; set; } = string.Empty;

        public string DisplayName => string.IsNullOrWhiteSpace(Username)
            ? IsEnglish ? "user" : "vartotojau"
            : Username;

        public int RemainingMeasurements =>
            MeasurementLimit.HasValue
                ? Math.Max(0, MeasurementLimit.Value - MeasurementCount)
                : int.MaxValue;

        public string LastMeasurementDateLabel => LastMeasurementDate?.ToString("yyyy-MM-dd") ?? (IsEnglish ? "Not yet" : "Dar nėra");

        public string LastMeasurementDateTimeLabel => LastMeasurementDate?.ToString("yyyy-MM-dd HH:mm") ?? (IsEnglish ? "Not yet" : "Dar nėra");

        public string LastMeasurementRelativeLabel
        {
            get
            {
                if (!LastMeasurementDate.HasValue)
                {
                    return IsEnglish ? "No saved measurements yet" : "Dar nėra išsaugotų matavimų";
                }

                if (HasMeasurementToday)
                {
                    return IsEnglish ? "Completed today" : "Atliktas šiandien";
                }

                if (DaysSinceLastMeasurement == 1)
                {
                    return IsEnglish ? "Last measurement was yesterday" : "Paskutinis matavimas buvo vakar";
                }

                return IsEnglish
                    ? $"Last measurement was {DaysSinceLastMeasurement} days ago"
                    : $"Paskutinis matavimas buvo prieš {DaysSinceLastMeasurement} dienas";
            }
        }

        private static bool IsEnglish =>
            CultureInfo.CurrentUICulture.Name.Equals("en-US", StringComparison.OrdinalIgnoreCase);
    }
}

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

        public DateTime? LastMeasurementDate { get; set; }
        public bool HasMeasurementToday { get; set; }
        public int? DaysSinceLastMeasurement { get; set; }

        public RiskMeasurement? LatestMeasurement { get; set; }

        public string StatusTitle { get; set; } = string.Empty;
        public string StatusText { get; set; } = string.Empty;
        public string StatusClass { get; set; } = "status-empty";

        public string FactTitle { get; set; } = string.Empty;
        public string FactText { get; set; } = string.Empty;

        public string DisplayName => string.IsNullOrWhiteSpace(Username) ? "vartotojau" : Username;

        public string LastMeasurementDateLabel => LastMeasurementDate?.ToString("yyyy-MM-dd") ?? "Dar nėra";

        public string LastMeasurementDateTimeLabel => LastMeasurementDate?.ToString("yyyy-MM-dd HH:mm") ?? "Dar nėra";

        public string LastMeasurementRelativeLabel
        {
            get
            {
                if (!LastMeasurementDate.HasValue)
                {
                    return "Dar nėra išsaugotų matavimų";
                }

                if (HasMeasurementToday)
                {
                    return "Atliktas šiandien";
                }

                if (DaysSinceLastMeasurement == 1)
                {
                    return "Paskutinis matavimas buvo vakar";
                }

                return $"Paskutinis matavimas buvo prieš {DaysSinceLastMeasurement} dienas";
            }
        }
    }
}

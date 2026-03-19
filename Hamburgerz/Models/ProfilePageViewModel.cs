namespace Hamburgerz.Models
{
    public class ProfilePageViewModel
    {
        public string Username { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Gender { get; set; } = string.Empty;

        public int? Age { get; set; }

        public string Country { get; set; } = string.Empty;

        public string JobRole { get; set; } = string.Empty;

        public int? ExperienceYears { get; set; }

        public string CompanySize { get; set; } = string.Empty;

        public string WorkEnvironment { get; set; } = string.Empty;

        public int? InternetSpeed { get; set; }

        public int MeasurementCount { get; set; }

        public DateTime? LastMeasurementDate { get; set; }

        public string AvatarInitial =>
            !string.IsNullOrWhiteSpace(Username)
                ? Username.Substring(0, 1).ToUpperInvariant()
                : "?";
    }
}

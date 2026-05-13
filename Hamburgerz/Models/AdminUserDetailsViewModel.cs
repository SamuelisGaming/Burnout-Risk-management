namespace Hamburgerz.Models
{
    public class AdminUserDetailsViewModel
    {
        public int Id { get; set; }

        public string Username { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string UserType { get; set; } = "user";

        public string Gender { get; set; } = string.Empty;

        public DateTime? BirthDate { get; set; }

        public string Country { get; set; } = string.Empty;

        public string JobRole { get; set; } = string.Empty;

        public int? ExperienceYears { get; set; }

        public string CompanySize { get; set; } = string.Empty;

        public string WorkEnvironment { get; set; } = string.Empty;

        public int MeasurementCount { get; set; }

        public DateTime? LastMeasurementDate { get; set; }
    }
}

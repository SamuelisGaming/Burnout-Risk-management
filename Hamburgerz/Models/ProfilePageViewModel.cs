using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Hamburgerz.Models
{
    public class ProfilePageViewModel
    {
        public string Username { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Gender { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? BirthDate { get; set; }

        public int? CountryID { get; set; }

        public string Country { get; set; } = string.Empty;

        [StringLength(80, ErrorMessage = "Job role must be 80 characters or less.")]
        public string JobRole { get; set; } = string.Empty;

        public int? ExperienceYears { get; set; }

        public string CompanySize { get; set; } = string.Empty;

        public string WorkEnvironment { get; set; } = string.Empty;

        public int MeasurementCount { get; set; }

        public DateTime? LastMeasurementDate { get; set; }

        public List<SelectListItem> Countries { get; set; } = new();

        public string AvatarInitial =>
            !string.IsNullOrWhiteSpace(Username)
                ? Username.Substring(0, 1).ToUpperInvariant()
                : "?";
    }
}

using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Hamburgerz.Models
{
    public class RegisterViewModel : IValidatableObject
    {
        private static readonly HashSet<string> AllowedCompanySizes = new()
        {
            "Maža",
            "Vidutinė",
            "Didelė"
        };

        private static readonly HashSet<string> AllowedWorkEnvironments = new()
        {
            "Nuotolinis",
            "Ofisinis",
            "Hibridinis"
        };

        [Required(ErrorMessage = "Iveskite el. pasta")]
        [EmailAddress(ErrorMessage = "Neteisingas el. pasto formatas")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Iveskite slapyvardi")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "Slapyvardis turi buti 3-20 simboliu")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Pasirinkite lyti")]
        public string Gender { get; set; } = string.Empty;

        [Required(ErrorMessage = "Iveskite gimimo data")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? BirthDate { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Pasirinkite sali")]
        public int CountryID { get; set; }

        [Required(ErrorMessage = "Iveskite darbo pozicija")]
        [StringLength(30, ErrorMessage = "Darbo pozicija turi buti iki 30 simboliu")]
        public string JobRole { get; set; } = string.Empty;

        [Required(ErrorMessage = "Iveskite patirties metus")]
        [Range(0, 80, ErrorMessage = "Patirties metai turi buti tarp 0 ir 80")]
        public int? ExperienceYears { get; set; }

        [Required(ErrorMessage = "Pasirinkite kompanijos dydi")]
        public string CompanySize { get; set; } = string.Empty;

        [Required(ErrorMessage = "Pasirinkite darbo pobudi")]
        public string WorkEnvironment { get; set; } = string.Empty;

        public List<SelectListItem> Countries { get; set; } = new();

        [Required(ErrorMessage = "Iveskite slaptazodi")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Pakartokite slaptazodi")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Slaptazodziai nesutampa")]
        public string PasswordRepeat { get; set; } = string.Empty;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (BirthDate.HasValue)
            {
                var today = DateTime.Today;
                var earliestAllowedDate = today.AddYears(-100);
                var latestAllowedDate = today.AddYears(-14);
                var normalizedBirthDate = BirthDate.Value.Date;

                if (normalizedBirthDate < earliestAllowedDate || normalizedBirthDate > latestAllowedDate)
                {
                    yield return new ValidationResult(
                        "Gimimo data turi atitikti 14-100 metu intervala.",
                        new[] { nameof(BirthDate) });
                }
            }

            if (!string.IsNullOrWhiteSpace(Password) && !PasswordRules.IsStrong(Password))
            {
                yield return new ValidationResult(
                    PasswordRules.RequirementsMessage,
                    new[] { nameof(Password) });
            }

            if (!string.IsNullOrWhiteSpace(CompanySize) && !AllowedCompanySizes.Contains(CompanySize))
            {
                yield return new ValidationResult(
                    "Pasirinkite tinkama kompanijos dydi",
                    new[] { nameof(CompanySize) });
            }

            if (!string.IsNullOrWhiteSpace(WorkEnvironment) && !AllowedWorkEnvironments.Contains(WorkEnvironment))
            {
                yield return new ValidationResult(
                    "Pasirinkite tinkama darbo pobudi",
                    new[] { nameof(WorkEnvironment) });
            }

            JobRole = (JobRole ?? string.Empty).Trim();
            BirthDate = BirthDate?.Date;
        }
    }
}

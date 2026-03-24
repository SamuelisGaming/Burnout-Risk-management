using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Hamburgerz.Models
{
    public class RegisterViewModel : IValidatableObject
    {
        [Required(ErrorMessage = "Iveskite el. pasta")]
        [EmailAddress(ErrorMessage = "Neteisingas el. pasto formatas")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Iveskite slapyvardi")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "Slapyvardis turi buti 3-20 simboliu")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Pasirinkite lyti")]
        public string Gender { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Pasirinkite sali")]
        public int CountryID { get; set; }

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
            if (!string.IsNullOrWhiteSpace(Password) && !PasswordRules.IsStrong(Password))
            {
                yield return new ValidationResult(
                    PasswordRules.RequirementsMessage,
                    new[] { nameof(Password) });
            }
        }
    }
}

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Hamburgerz.Models
{
    public class RegisterViewModel : IValidatableObject
    {
        [Required(ErrorMessage = "Iveskite el. paštą")]
        [EmailAddress(ErrorMessage = "Neteisingas el. pašto formatas")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Iveskite slapyvardį")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "Slapyvardis turi būti 3-20 simbolių")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Pasirinkite lytį")]
        public string Gender { get; set; } = string.Empty;

        [Required(ErrorMessage = "Įveskite slaptažodį")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Pakartokite slaptažodį")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Slaptažodžiai nesutampa")]
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

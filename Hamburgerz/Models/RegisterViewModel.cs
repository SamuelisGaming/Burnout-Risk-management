using Hamburgerz.PasswordValidation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Hamburgerz.Models
{    
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Įveskite el. paštą")]
        [EmailAddress(ErrorMessage = "Neteisingas el. pašto formatas")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Įveskite slapyvardį")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "Slapyvardis turi būti 3-20 simbolių")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Pasirinkite lytį")]
        public string Gender { get; set; } = string.Empty;

        [Required(ErrorMessage = "Pasirinkite šalį")]
        public int CountryID { get; set; }
        public List<SelectListItem> Countries { get; set; } = new();


        [Required(ErrorMessage = "Įveskite slaptažodį")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Slaptažodis turi būti bent 6 simbolių")]
        [PasswordComplexity(ErrorMessage = "Slaptažodis turi turėti didžiąją, mažąją raidę, skaičių ir specialų simbolį")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Pakartokite slaptažodį")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "")]
        public string PasswordRepeat { get; set; } = string.Empty;
    }
}
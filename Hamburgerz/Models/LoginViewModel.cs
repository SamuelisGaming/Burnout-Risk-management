using System.ComponentModel.DataAnnotations;

namespace Hamburgerz.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Įveskite el. paštą")]
        [EmailAddress(ErrorMessage = "Neteisingas el. pašto formatas")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Įveskite slaptažodį")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
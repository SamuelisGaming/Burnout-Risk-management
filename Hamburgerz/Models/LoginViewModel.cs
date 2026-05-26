using System.ComponentModel.DataAnnotations;

namespace Hamburgerz.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Įveskite el. paštą arba slapyvardį")]
        //[EmailAddress(ErrorMessage = "Neteisingas el. pašto formatas arba slapyvardis")]
        public string Login { get; set; } = string.Empty;

        [Required(ErrorMessage = "Įveskite slaptažodį")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
using System.ComponentModel.DataAnnotations;

namespace Hamburgerz.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "El. paštas privalomas")]
        [EmailAddress]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Slapyvardis privalomas")]
        public string Username { get; set; } = "";

        [Required(ErrorMessage = "Slaptažodis privalomas")]
        public string Password { get; set; } = "";

        // hashtag lyčių lygybė
        [Required(ErrorMessage = "Lytis privaloma")]
        public string Gender{ get; set; } = "";

        // birth optional
        public string Birth { get; set; } = "";


        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}

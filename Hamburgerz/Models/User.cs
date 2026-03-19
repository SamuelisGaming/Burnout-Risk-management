using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hamburgerz.Models
{
    [Table("users")]
    public class User
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("countryid")]
        public int CountryID { get; set; }

        [Required]
        [MaxLength(20)]
        [Column("username")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Column("gender")]
        public string Gender { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        [Column("password_hashed")]
        public string PasswordHashed { get; set; } = string.Empty;

        [Column("is_email_verified")]
        public bool IsEmailVerified { get; set; }

        [Required]
        [Column("user_type")]
        public string UserType { get; set; } = "user";
    }
}
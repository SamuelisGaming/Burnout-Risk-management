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

        [Column("countryid")]
        public int? CountryID { get; set; }

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

        [Column("birth_date", TypeName = "date")]
        public DateTime? BirthDate { get; set; }

        [MaxLength(30)]
        [Column("job_role")]
        public string? JobRole { get; set; }

        [Column("experience_years")]
        public int? ExperienceYears { get; set; }

        [Column("company_size")]
        public string? CompanySize { get; set; }

        [Column("work_environment")]
        public string? WorkEnvironment { get; set; }

        [Column("profile_image")]
        public byte[]? ProfileImage { get; set; }

        [MaxLength(20)]
        [Column("profile_image_type")]
        public string? ProfileImageType { get; set; }

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

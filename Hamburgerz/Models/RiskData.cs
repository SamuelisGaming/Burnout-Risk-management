using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hamburgerz.Models
{
    [Table("riskdata")]
    public class RiskData
    {
        [Key]
        [Column("ID")]
        public int ID { get; set; }

        [Column("gender")]
        public string? Gender { get; set; }

        [Column("job_role")]
        public string? JobRole { get; set; }

        [Column("experience_years")]
        public int? ExperienceYears { get; set; }

        [Column("company_size")]
        public string? CompanySize { get; set; }

        [Required(ErrorMessage = "Įrašykite darbo valandas")]
        [Column("work_hours")]
        public float WorkHours { get; set; }

        [Required(ErrorMessage = "Įrašykite susirinkimų skaičių")]
        [Column("meetings_per_day")]
        public int MeetingsPerDay { get; set; }

        [Required(ErrorMessage = "Įrašykite interneto greitį")]
        [Column("internet_speed")]
        public int InternetSpeed { get; set; }

        [Column("work_environment")]
        public string? WorkEnvironment { get; set; }

        [Required(ErrorMessage = "Įrašykite miego valandas")]
        [Column("sleep_hours")]
        public float SleepHours { get; set; }

        [Required(ErrorMessage = "Įrašykite sporto valandas")]
        [Column("exercise_hours")]
        public float ExerciseHours { get; set; }

        [Required(ErrorMessage = "Įrašykite ekrano valandas")]
        [Column("screen_time")]
        public float ScreenTime { get; set; }

        [Required(ErrorMessage = "Pažymėkite streso lygį")]
        [Column("stress_level")]
        public string StressLevel { get; set; } = string.Empty;

        [Column("productivity_score")]
        public int ProductivityScore { get; set; }

        [Column("burnout_risk")]
        public float BurnoutRisk { get; set; }

        [Column("Time_stamp")]
        public DateTime TimeStamp { get; set; } = DateTime.Now;

        [Column("fk_userID")]
        public int UserId { get; set; }

        [Column("suggestion")]
        public string? Suggestion { get; set; } = string.Empty ;
    }
}

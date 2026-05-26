using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hamburgerz.Models
{
    [Table("measurement_answers")]
    public class MeasurementAnswer
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("risk_data_id")]
        public int RiskDataId { get; set; }

        [Column("fk_userID")]
        public int UserId { get; set; }

        [Required]
        [MaxLength(80)]
        [Column("question_key")]
        public string QuestionKey { get; set; } = string.Empty;

        [Column("score")]
        public int Score { get; set; }

        [Column("answered_at")]
        public DateTime AnsweredAt { get; set; } = DateTime.Now;
    }
}

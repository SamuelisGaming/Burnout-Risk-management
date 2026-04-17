using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hamburgerz.Models
{
    [Table("analysis_cache")]
    public class AnalysisCache
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("measurement_count")]
        public int MeasurementCount { get; set; }

        [Column("generated_at")]
        public DateTime GeneratedAt { get; set; } = DateTime.Now;

        [Column("status")]
        public string Status { get; set; } = "generating";

        [Column("content")]
        public string? Content { get; set; }
    }
}

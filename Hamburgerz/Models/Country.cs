using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hamburgerz.Models
{
    [Table("countries")]
    public class Country
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [MaxLength(120)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;
    }
}

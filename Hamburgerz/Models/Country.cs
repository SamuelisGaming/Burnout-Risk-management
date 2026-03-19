using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hamburgerz.Models
{
    public class Country
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}

namespace Hamburgerz.Models
{
    public class AdminUserListItemViewModel
    {
        public int Id { get; set; }

        public string Username { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string UserType { get; set; } = "user";

        public int MeasurementCount { get; set; }

        public DateTime? LastMeasurementDate { get; set; }
    }
}

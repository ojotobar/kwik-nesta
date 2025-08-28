using NotificationService.Workers.Models.Enums;

namespace NotificationService.Workers.Models
{
    public class UserLeanDto
    {
        public string Id { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public AspNetUserStatus Status { get; set; }
    }
}

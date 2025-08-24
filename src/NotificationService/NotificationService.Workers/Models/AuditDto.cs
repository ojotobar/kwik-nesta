namespace NotificationService.Workers.Models
{
    public class AuditDto
    {
        public string PerformedBy { get; set; } = string.Empty;
        public string PerformedOn { get; set;} = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public DateTime TimeStamp { get; set; }
    }
}

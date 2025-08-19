using System.ComponentModel;

namespace NotificationService.Workers.Models.Enums
{
    public enum RabbitMqRoutingKey
    {
        [Description("account.email")]
        AccountEmail
    }
}

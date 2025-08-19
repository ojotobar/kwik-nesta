using NotificationService.Workers.Models;

namespace NotificationService.Workers.Services.Interfaces
{
    public interface IEmailSenders
    {
        Task SendAccountActivationEmail(EmailNotification notification);
        Task SendPasswordResetEmail(EmailNotification notification);
    }
}

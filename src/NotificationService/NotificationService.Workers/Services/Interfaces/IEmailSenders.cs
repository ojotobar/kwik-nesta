using NotificationService.Workers.Models;

namespace NotificationService.Workers.Services.Interfaces
{
    public interface IEmailSenders
    {
        Task SendAccountActivationEmail(EmailNotification notification);
        Task SendAccountDeactivationEmail(EmailNotification notification);
        Task SendAccountReactivationEmail(EmailNotification notification);
        Task SendAccountReactivationNotificationEmail(EmailNotification notification);
        Task SendAccountSuspensionEmail(EmailNotification notification);
        Task SendAdminRectivationNotificationEmail(EmailNotification notification);
        Task SendPasswordResetEmail(EmailNotification notification);
        Task SendPasswordResetNotificationEmail(EmailNotification notification);
    }
}

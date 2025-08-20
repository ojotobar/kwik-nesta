using NotificationService.Workers.Models;
using NotificationService.Workers.Models.Enums;
using NotificationService.Workers.Services.Interfaces;

namespace NotificationService.Workers.Handlers
{
    public class MessageHandler : IMessageHandler
    {
        private readonly ILogger<MessageHandler> _logger;
        private readonly IEmailSenders _emailSenders;

        public MessageHandler(ILogger<MessageHandler> logger, IEmailSenders emailSenders)
        {
            _logger = logger;
            _emailSenders = emailSenders;
            _logger = logger;
        }

        public async Task HandleAsync(EmailNotification message)
        {
           if(message != null)
           {
                switch (message.Type)
                {
                    case EmailType.AccountActivation:
                        await _emailSenders.SendAccountActivationEmail(message);
                        break;
                    case EmailType.PasswordReset:
                        await _emailSenders.SendPasswordResetEmail(message);
                        break;
                    case EmailType.PasswordResetNotification:
                        await _emailSenders.SendPasswordResetNotificationEmail(message);
                        break;
                    case EmailType.AccountDeactivation:
                        await _emailSenders.SendAccountDeactivationEmail(message);
                        break;
                    case EmailType.AccountReactivation:
                        await _emailSenders.SendAccountReactivationEmail(message);
                        break;
                    case EmailType.AccountSuspension:
                        await _emailSenders.SendAccountSuspensionEmail(message);
                        break;
                    case EmailType.AccountReactivationNotification:
                        await _emailSenders.SendAccountReactivationNotificationEmail(message);
                        break;
                    case EmailType.AdminAccountReactivation:
                        await _emailSenders.SendAdminRectivationNotificationEmail(message);
                        break;
                }
            }
            else
           {
                _logger.LogWarning($"Message content came null");
           }
        }
    }
}

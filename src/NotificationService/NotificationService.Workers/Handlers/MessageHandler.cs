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
                }
           }
           else
           {
                _logger.LogWarning($"Message content came null");
           }
        }
    }
}

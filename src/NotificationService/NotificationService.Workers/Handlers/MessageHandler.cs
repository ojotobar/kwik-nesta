using DiagnosKit.Core.Logging.Contracts;
using EFCore.CrudKit.Library.Data.Interfaces;
using KwikNesta.Contracts.Enums;
using KwikNesta.Contracts.Models;
using NotificationService.Workers.Services.Interfaces;

namespace NotificationService.Workers.Handlers
{
    public class MessageHandler : IMessageHandler
    {
        private readonly ILoggerManager _logger;
        private readonly IEmailSenders _emailSenders;
        private readonly IEFCoreMongoCrudKit _mongoCrudKit;

        public MessageHandler(ILoggerManager logger, IEmailSenders emailSenders,
            IEFCoreMongoCrudKit mongoCrudKit)
        {
            _logger = logger;
            _emailSenders = emailSenders;
            _mongoCrudKit = mongoCrudKit;
            _logger = logger;
        }

        public async Task HandleAsync(NotificationMessage message)
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
                _logger.LogWarn($"Message content came null");
           }
        }

        public async Task HandleAsync(AuditLog message)
        {
            try
            {
                if (message != null)
                {
                    await _mongoCrudKit.InsertAsync(message);
                    _logger.LogInfo("Audit trail successfully added. Action Performed: {Action}", message.Action);
                }
                else
                {
                    _logger.LogWarn($"Message content came null");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred: {Message}", ex.Message);
            }
        }
    }
}

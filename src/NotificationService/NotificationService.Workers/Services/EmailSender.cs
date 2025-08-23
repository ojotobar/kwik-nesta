using CSharpTypes.Extensions.Object;
using CSharpTypes.Extensions.String;
using DRY.MailJetClient.Library;
using KwikNesta.Contracts.Enums;
using KwikNesta.Contracts.Models;
using NotificationService.Workers.Services.Interfaces;

namespace NotificationService.Workers.Services
{
    public class EmailSender : IEmailSenders
    {
        private readonly IMailjetClientService _mailJet;
        private readonly ILogger<EmailSender> _logger;
        private readonly string _templateRoot;

        public EmailSender(IMailjetClientService mailJet, IHostEnvironment env, 
            ILogger<EmailSender> logger)
        {
            _mailJet = mailJet;
            _logger = logger;
            _templateRoot = Path.Combine(env.ContentRootPath, "wwwroot", "templates");
        }

        public async Task SendAccountActivationEmail(NotificationMessage notification)
        {
            var valid = ValidatePayload(notification);
            if (!valid)
            {
                _logger.LogWarning("Invalid notification payload");
            }

            var template = LoadTemplate("account-activation");
            if (template.IsNullOrEmpty())
            {
                _logger.LogWarning("The template returned an empty string");
            }

            var body = template.Replace("{{FirstName}}", notification.ReceipientName)
                .Replace("{{OTP}}", notification.Otp?.Value)
                .Replace("{{validity}}", notification.Otp?.Span.ToString())
                .Replace("{{Year}}", DateTime.UtcNow.Year.ToString());

            var isSent = await _mailJet.SendAsync(notification.EmailAddress, body, notification.Subject);
            if (isSent)
            {
                _logger.LogInformation($"Account activation email successfully sent to {notification.EmailAddress}");
            }
            else
            {
                _logger.LogError($"Account activation email failed for {notification.EmailAddress}");
            }
        }

        public async Task SendPasswordResetEmail(NotificationMessage notification)
        {
            var valid = ValidatePayload(notification);
            if (!valid)
            {
                _logger.LogWarning("Invalid notification payload");
            }

            var template = LoadTemplate("password-reset");
            if (template.IsNullOrEmpty())
            {
                _logger.LogWarning("The template returned an empty string");
            }

            var body = template.Replace("{{FirstName}}", notification.ReceipientName)
                .Replace("{{OTP}}", notification.Otp?.Value)
                .Replace("{{validity}}", notification.Otp?.Span.ToString())
                .Replace("{{Year}}", DateTime.UtcNow.Year.ToString());

            var isSent = await _mailJet.SendAsync(notification.EmailAddress, body, notification.Subject);
            if (isSent)
            {
                _logger.LogInformation($"Password reset email successfully sent to {notification.EmailAddress}");
            }
            else
            {
                _logger.LogError($"Password reset email failed for {notification.EmailAddress}");
            }
        }

        public async Task SendPasswordResetNotificationEmail(NotificationMessage notification)
        {
            var valid = ValidatePayload(notification);
            if (!valid)
            {
                _logger.LogWarning("Invalid notification payload");
            }

            var template = LoadTemplate("password-reset-notification");
            if (template.IsNullOrEmpty())
            {
                _logger.LogWarning("The template returned an empty string");
            }

            var body = template.Replace("{{FirstName}}", notification.ReceipientName)
                .Replace("{{Year}}", DateTime.UtcNow.Year.ToString());

            var isSent = await _mailJet.SendAsync(notification.EmailAddress, body, notification.Subject);
            if (isSent)
            {
                _logger.LogInformation($"Password reset notification email successfully sent to {notification.EmailAddress}");
            }
            else
            {
                _logger.LogError($"Password reset notification email failed for {notification.EmailAddress}");
            }
        }

        public async Task SendAccountDeactivationEmail(NotificationMessage notification)
        {
            var valid = ValidatePayload(notification);
            if (!valid)
            {
                _logger.LogWarning("Invalid notification payload");
            }

            var template = LoadTemplate(notification.Type.GetDescription());
            if (template.IsNullOrEmpty())
            {
                _logger.LogWarning("The template returned an empty string");
            }

            var now = DateTime.UtcNow;
            var body = template.Replace("{{FirstName}}", notification.ReceipientName)
                .Replace("{{DeactivationDate}}", now.ToString("d"))
                .Replace("{{Year}}", now.Year.ToString());

            var isSent = await _mailJet.SendAsync(notification.EmailAddress, body, notification.Subject);
            if (isSent)
            {
                _logger.LogInformation($"Account deactivation notification email successfully sent to {notification.EmailAddress}");
            }
            else
            {
                _logger.LogError($"Account deactivation notification email failed for {notification.EmailAddress}");
            }
        }

        public async Task SendAccountReactivationEmail(NotificationMessage notification)
        {
            var valid = ValidatePayload(notification);
            if (!valid)
            {
                _logger.LogWarning("Invalid notification payload");
            }

            var template = LoadTemplate(notification.Type.GetDescription());
            if (template.IsNullOrEmpty())
            {
                _logger.LogWarning("The template returned an empty string");
            }

            var now = DateTime.UtcNow;
            var body = template.Replace("{{FirstName}}", notification.ReceipientName)
                .Replace("{{OTP}}", notification.Otp?.Value)
                .Replace("{{validity}}", notification.Otp?.Span.ToString())
                .Replace("{{Year}}", now.Year.ToString());

            var isSent = await _mailJet.SendAsync(notification.EmailAddress, body, notification.Subject);
            if (isSent)
            {
                _logger.LogInformation($"Account reactivation OTP email successfully sent to {notification.EmailAddress}");
            }
            else
            {
                _logger.LogError($"Account reactivation OTP email failed for {notification.EmailAddress}");
            }
        }

        public async Task SendAccountReactivationNotificationEmail(NotificationMessage notification)
        {
            var valid = ValidatePayload(notification);
            if (!valid)
            {
                _logger.LogWarning("Invalid notification payload");
            }

            var template = LoadTemplate(notification.Type.GetDescription());
            if (template.IsNullOrEmpty())
            {
                _logger.LogWarning("The template returned an empty string");
            }

            var body = template.Replace("{{FirstName}}", notification.ReceipientName)
                .Replace("{{Year}}", DateTime.UtcNow.Year.ToString());

            var isSent = await _mailJet.SendAsync(notification.EmailAddress, body, notification.Subject);
            if (isSent)
            {
                _logger.LogInformation($"Account reactivation notification email successfully sent to {notification.EmailAddress}");
            }
            else
            {
                _logger.LogError($"Account reactivation notification email failed for {notification.EmailAddress}");
            }
        }

        public async Task SendAccountSuspensionEmail(NotificationMessage notification)
        {
            var valid = ValidatePayload(notification);
            if (!valid)
            {
                _logger.LogWarning("Invalid notification payload");
            }

            var template = LoadTemplate(notification.Type.GetDescription());
            if (template.IsNullOrEmpty())
            {
                _logger.LogWarning("The template returned an empty string");
            }

            var body = template.Replace("{{FirstName}}", notification.ReceipientName)
                .Replace("{{Reason}}", notification.Reason)
                .Replace("{{Year}}", DateTime.UtcNow.Year.ToString());

            var isSent = await _mailJet.SendAsync(notification.EmailAddress, body, notification.Subject);
            if (isSent)
            {
                _logger.LogInformation($"Account suspension notification email successfully sent to {notification.EmailAddress}");
            }
            else
            {
                _logger.LogError($"Account suspension notification email failed for {notification.EmailAddress}");
            }
        }

        public async Task SendAdminRectivationNotificationEmail(NotificationMessage notification)
        {
            var valid = ValidatePayload(notification);
            if (!valid)
            {
                _logger.LogWarning("Invalid notification payload");
            }

            var template = LoadTemplate(notification.Type.GetDescription());
            if (template.IsNullOrEmpty())
            {
                _logger.LogWarning("The template returned an empty string");
            }

            var body = template.Replace("{{FirstName}}", notification.ReceipientName)
                .Replace("{{Year}}", DateTime.UtcNow.Year.ToString());

            var isSent = await _mailJet.SendAsync(notification.EmailAddress, body, notification.Subject);
            if (isSent)
            {
                _logger.LogInformation($"Account suspension lift notification email successfully sent to {notification.EmailAddress}");
            }
            else
            {
                _logger.LogError($"Account suspension lift notification email failed for {notification.EmailAddress}");
            }
        }

        private string LoadTemplate(string templateName)
        {
            var path = Path.Combine(_templateRoot, $"{templateName}.html");
            if(File.Exists(path))
            {
                return File.ReadAllText(path);
            }

            return string.Empty;   
        }

        private bool ValidatePayload(NotificationMessage notification)
        {
            if (notification == null)
            {
                return false;
            }

            if(notification.ReceipientName.IsNullOrEmpty() || notification.EmailAddress.IsNullOrEmpty())
            {
                return false;
            }

            if(notification.Type is EmailType.AccountActivation or EmailType.PasswordReset or EmailType.AccountReactivation)
            {
                if (notification.Otp.IsNull() || (notification.Otp != null && notification.Otp.Value.IsNullOrEmpty()))
                {
                    return false;
                }
            }

            if(notification.Type is EmailType.AccountSuspension && !string.IsNullOrWhiteSpace(notification.Reason))
            {
                return false;
            }

            return true;
        }
    }
}

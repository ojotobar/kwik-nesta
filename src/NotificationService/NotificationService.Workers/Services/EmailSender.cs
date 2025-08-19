using CSharpTypes.Extensions.Object;
using CSharpTypes.Extensions.String;
using DRY.MailJetClient.Library;
using NotificationService.Workers.Models;
using NotificationService.Workers.Models.Enums;
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

        public async Task SendAccountActivationEmail(EmailNotification notification)
        {
            var template = LoadTemplate("account-activation");
            if (template.IsNullOrEmpty())
            {
                _logger.LogWarning("The template returned an empty string");
            }

            var body = template.Replace("{{FirstName}}", notification.ReceipientName)
                .Replace("{{OTP}}", notification.Otp?.Value)
                .Replace("{{validity}}", notification.Otp?.Span.ToString())
                .Replace("{{Year}}", DateTime.UtcNow.Year.ToString());

            var valid = ValidatePayload(notification);
            if(!valid)
            {
                _logger.LogWarning("Invalid notification payload");
            }

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

        public async Task SendPasswordResetEmail(EmailNotification notification)
        {
            var template = LoadTemplate("password-reset");
            if (template.IsNullOrEmpty())
            {
                _logger.LogWarning("The template returned an empty string");
            }

            var body = template.Replace("{{FirstName}}", notification.ReceipientName)
                .Replace("{{OTP}}", notification.Otp?.Value)
                .Replace("{{validity}}", notification.Otp?.Span.ToString())
                .Replace("{{Year}}", DateTime.UtcNow.Year.ToString());

            var valid = ValidatePayload(notification);
            if (!valid)
            {
                _logger.LogWarning("Invalid notification payload");
            }

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

        public async Task SendPasswordResetNotificationEmail(EmailNotification notification)
        {
            var template = LoadTemplate("password-reset-notification");
            if (template.IsNullOrEmpty())
            {
                _logger.LogWarning("The template returned an empty string");
            }

            var body = template.Replace("{{FirstName}}", notification.ReceipientName)
                .Replace("{{Year}}", DateTime.UtcNow.Year.ToString());

            var valid = ValidatePayload(notification);
            if (!valid)
            {
                _logger.LogWarning("Invalid notification payload");
            }

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

        private string LoadTemplate(string templateName)
        {
            var path = Path.Combine(_templateRoot, $"{templateName}.html");
            if(File.Exists(path))
            {
                return File.ReadAllText(path);
            }

            return string.Empty;   
        }

        private bool ValidatePayload(EmailNotification notification)
        {
            if (notification == null)
            {
                return false;
            }

            if(notification.ReceipientName.IsNullOrEmpty() || notification.EmailAddress.IsNullOrEmpty())
            {
                return false;
            }

            if(notification.Type is EmailType.AccountActivation or EmailType.PasswordReset)
            {
                if (notification.Otp.IsNull() || (notification.Otp != null && notification.Otp.Value.IsNullOrEmpty()))
                {
                    return false;
                }
            }

            return true;
        }
    }
}

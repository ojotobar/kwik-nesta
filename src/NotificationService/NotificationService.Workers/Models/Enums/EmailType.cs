using System.ComponentModel;

namespace NotificationService.Workers.Models.Enums
{
    public enum EmailType
    {
        [Description("account-activation")]
        AccountActivation,
        [Description("account-deactivation")]
        AccountDeactivation,
        [Description("account-reactivation")]
        AccountReactivation,
        [Description("account-reactivation-notification")]
        AccountReactivationNotification,
        [Description("account-suspension")]
        AccountSuspension,
        [Description("admin-account-reactivation")]
        AdminAccountReactivation,
        [Description("password-reset")]
        PasswordReset,
        [Description("password-reset-notification")]
        PasswordResetNotification
    }
}

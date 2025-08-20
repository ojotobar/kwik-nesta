using System.ComponentModel;

namespace IdentityService.Domain.Enums
{
    public enum EmailType
    {
        [Description("Activate Your Account")]
        AccountActivation,
        [Description("Account Deactivation")]
        AccountDeactivation,
        [Description("Reactivate Your Account")]
        AccountReactivation,
        [Description("Notice of Account Reactivation")]
        AccountReactivationNotification,
        [Description("Account Suspension")]
        AccountSuspension,
        [Description("Notice of Account Reactivation")]
        AdminAccountReactivation,
        [Description("Reset Your Passowrd")]
        PasswordReset,
        [Description("Notice of Password Change")]
        PasswordResetNotification
    }
}

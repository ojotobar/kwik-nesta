using IdentityService.Domain.Enums;

namespace IdentityService.Application.Validations
{
    internal class ValidationExtensions
    {
        private static readonly List<SystemRoles> _adminRoles = new List<SystemRoles>
        {
            SystemRoles.SuperAdmin, SystemRoles.Admin
        };

        internal static bool IsAValidRole(SystemRoles role, bool isNotAdmin)
        {
            return !_adminRoles.Contains(role) && isNotAdmin  || _adminRoles.Contains(role) && !isNotAdmin;
        }

        internal static bool IsAMatch(string password, string comparePassword)
        {
            return password.ToLower().Equals(comparePassword.ToLower());
        }

        internal static bool IsAValidStatusForUpdate(UserStatus status)
        {
            return status != UserStatus.PendingVerification;
        }
    }
}

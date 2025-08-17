using System.ComponentModel;

namespace IdentityService.Domain.Enums
{
    public enum Gender
    {
        [Description("Others")]
        Others,
        [Description("Male")]
        Male,
        [Description("Female")]
        Female
    }
}

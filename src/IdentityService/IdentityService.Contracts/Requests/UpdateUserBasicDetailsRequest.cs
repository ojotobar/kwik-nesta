using IdentityService.Domain.Enums;

namespace IdentityService.Contracts.Requests
{
    public class UpdateUserBasicDetailsRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string OtherName { get; set; } = string.Empty;
        public Gender Gender { get; set; }
    }
}

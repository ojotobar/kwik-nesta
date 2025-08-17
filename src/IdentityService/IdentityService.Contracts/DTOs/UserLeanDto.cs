namespace IdentityService.Contracts.DTOs
{
    public class UserLeanDto
    {
        public string Id { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? MiddleName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
    }
}

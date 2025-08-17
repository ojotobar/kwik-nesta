using CSharpTypes.Extensions.Enumeration;
using IdentityService.Contracts.DTOs;
using IdentityService.Contracts.Requests;
using IdentityService.Domain.Entities;

namespace IdentityService.Application.Extensions
{
    internal static class ObjectMapper
    {
        public static AppUser Map(this RegistrationRequest request)
        {
            return new AppUser
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                OtherName = request.MiddleName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                UserName = request.Email,
                Gender = request.Gender
            };
        }

        public static OtpEntry Map(this AppUser user, string hash, string salt, int span = 10)
        {
            return new OtpEntry
            {
                UserId = user.Id,
                OtpHash = hash,
                OtpSalt = salt,
                ExpiresAt = DateTime.UtcNow.AddMinutes(span)
            };
        }

        public static UserLeanDto Map(this AppUser user)
        {
            return new UserLeanDto
            {
                Id = user.Id,
                Email = user.Email!,
                PhoneNumber = user.PhoneNumber!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                MiddleName = user.OtherName,
                IsActive = user.IsActive,
                Gender = user.Gender.GetDescription()
            };
        }
    }
}

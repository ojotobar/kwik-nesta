using CSharpTypes.Extensions.Enumeration;
using IdentityService.Contracts.DTOs;
using IdentityService.Contracts.Requests;
using IdentityService.Domain.Entities;
using IdentityService.Domain.Enums;
using static System.Net.WebRequestMethods;

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

        public static OtpEntry Map(this AppUser user, string hash, string salt, OtpType otpType = OtpType.AccountVerification, string? token = null, int span = 10)
        {
            return new OtpEntry
            {
                UserId = user.Id,
                OtpHash = hash,
                OtpSalt = salt,
                ExpiresAt = DateTime.UtcNow.AddMinutes(span),
                Type = otpType,
                Token = token
            };
        }

        public static EmailNotification Map(this AppUser user, string subject, 
            string otp, DateTime expires, EmailType emailType = EmailType.AccountActivation)
        {
            return new EmailNotification
            {
                EmailAddress = user.Email!,
                ReceipientName = user.FirstName,
                Type = emailType,
                Subject = subject,
                Otp = new OtpData
                {
                    Value = otp,
                    Span = (int)Math.Ceiling(expires.Subtract(DateTime.UtcNow).TotalMinutes)
                }
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

using API.Common.Response.Model.Responses;
using IdentityService.Application.Validations;
using IdentityService.Contracts.Requests;

namespace IdentityService.Application.Services.Interfaces
{
    public interface IUserService
    {
        Task<ApiBaseResponse> GetLoggedInUserLeanAsync();
        Task<ApiBaseResponse> RegisterAsync(RegistrationRequest request, bool forAdmin = false);
        Task<ApiBaseResponse> ResendOtpAsync(OtpResendRequest request);
        Task<ApiBaseResponse> RequestPasswordResetAsync(EmailPayload request);
        Task<ApiBaseResponse> UpdateUserLastLogin(string id);
        Task<ApiBaseResponse> ValidateUser(LoginRequest request);
        Task<ApiBaseResponse> VerifyAccountAsync(OtpVerificationRequest request);
        Task<ApiBaseResponse> PasswordResetAsync(PasswordResetRequest request);
        Task<ApiBaseResponse> ChangePasswordAsync(PasswordChangeRequest request);
        Task<ApiBaseResponse> UpdateBasicDetails(UpdateUserBasicDetailsRequest request);
        Task<ApiBaseResponse> SuspendUserAccountAsync(UserSuspensionRequest request);
        Task<ApiBaseResponse> LiftAccountSuspensionAsync(string userId);
        Task<ApiBaseResponse> DeactivateAccountAsync(string userId);
        Task<ApiBaseResponse> RequestAccountReactivationAsync(EmailPayload request);
        Task<ApiBaseResponse> ReactivateAccountAsync(OtpVerificationRequest request);
    }
}

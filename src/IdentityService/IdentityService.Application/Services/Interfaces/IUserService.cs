using API.Common.Response.Model.Responses;
using IdentityService.Contracts.Requests;

namespace IdentityService.Application.Services.Interfaces
{
    public interface IUserService
    {
        Task<ApiBaseResponse> GetLoggedInUserLeanAsync();
        Task<ApiBaseResponse> RegisterAsync(RegistrationRequest request, bool forAdmin = false);
        Task<ApiBaseResponse> UpdateUserLastLogin(string id);
        Task<ApiBaseResponse> ValidateUser(LoginRequest request);
        Task<ApiBaseResponse> VerifyAccountAsync(AccountVerificationRequest request);
    }
}

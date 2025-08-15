using API.Common.Response.Model.Responses;
using IdentityService.Contracts.Requests;

namespace IdentityService.Application.Services.Interfaces
{
    public interface IUserService
    {
        Task<ApiBaseResponse> ValidateUser(LoginRequest request);
    }
}

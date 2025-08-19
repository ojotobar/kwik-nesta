using API.Common.Response.Model.Responses;
using IdentityService.Contracts.Requests;
using IdentityService.Domain.Entities;

namespace IdentityService.Application.Services.Interfaces
{
    public interface ITokenService
    {
        string CreateAccessToken(AppUser user, string[] roles);
        Task<string> CreateAndSaveRefreshTokenAsync(string userId);
        Task<ApiBaseResponse> RefreshTokenAsync(RefreshTokenRequest request);
        Task<RefreshToken?> ValidateRefreshTokenAsync(string token);
    }
}

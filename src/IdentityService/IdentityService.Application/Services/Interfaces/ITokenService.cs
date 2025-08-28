using API.Common.Response.Model.Responses;
using IdentityService.Contracts.Requests;
using IdentityService.Domain.Entities;

namespace IdentityService.Application.Services.Interfaces
{
    public interface ITokenService
    {
        string CreateAccessToken(AppUser user, string[] roles, string validAudience);
        Task<string> CreateAndSaveRefreshTokenAsync(string userId);
        Task<ApiBaseResponse> RefreshTokenAsync(RefreshTokenRequest request, string validAudience);
        Task<RefreshToken?> ValidateRefreshTokenAsync(string token);
    }
}

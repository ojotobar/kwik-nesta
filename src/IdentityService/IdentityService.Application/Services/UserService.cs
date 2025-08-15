using API.Common.Response.Model.Responses;
using IdentityService.Application.Services.Interfaces;
using IdentityService.Application.Validations;
using IdentityService.Contracts.Requests;
using IdentityService.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace IdentityService.Application.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;

        public UserService(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<ApiBaseResponse> ValidateUser(LoginRequest request)
        {
            var validation = new LoginValidator().Validate(request);
            if (!validation.IsValid)
            {
                return new BadRequestResponse(validation.Errors.FirstOrDefault()?.ErrorMessage ?? "Invalid input");
            }

            var user = await _userManager.FindByNameAsync(request.UserName);
            if (user == null)
            {
                return new NotFoundResponse("User not found");
            }

            var check = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            if (!check.Succeeded)
            {
                return new ForbiddenResponse("Wrong password");
            }

            var roles = (await _userManager.GetRolesAsync(user)).ToArray();
            if (roles == null || roles.Length == 0)
            {
                return new UnauthorizedResponse("User have no assigned role.");
            }

            return new OkResponse<(AppUser User, string[] Roles)>((user, roles));
        }

    }
}

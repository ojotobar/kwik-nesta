using EFCore.CrudKit.Library.Data.Interfaces;
using IdentityService.Application.Services.Interfaces;
using IdentityService.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace IdentityService.Application.Services
{
    public class ServiceManager : IServiceManager
    {
        private readonly Lazy<ITokenService> _tokenService;
        private readonly Lazy<IUserService> _userService;

        public ServiceManager(IEFCoreCrudKit crudKit, RSA rSA, IOptions<Jwt> options, UserManager<AppUser> userManager, 
            SignInManager<AppUser> signInManager)
        {
            _tokenService = new Lazy<ITokenService>(() =>
                new TokenService(crudKit, options, rSA));
            _userService = new Lazy<IUserService>(() =>
                new UserService(userManager, signInManager));
        }

        public ITokenService Token => _tokenService.Value;

        public IUserService User => _userService.Value;
    }
}

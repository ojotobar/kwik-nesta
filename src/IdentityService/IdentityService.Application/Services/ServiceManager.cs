using CrossQueue.Hub.Services.Interfaces;
using EFCore.CrudKit.Library.Data.Interfaces;
using IdentityService.Application.Services.Interfaces;
using IdentityService.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace IdentityService.Application.Services
{
    public class ServiceManager : IServiceManager
    {
        private readonly Lazy<ITokenService> _tokenService;
        private readonly Lazy<IUserService> _userService;

        public ServiceManager(IEFCoreCrudKit crudKit, IOptions<Jwt> options, UserManager<AppUser> userManager, 
            SignInManager<AppUser> signInManager, IHttpContextAccessor contextAccessor, IEFCoreCrudKit eFCoreCrud, 
            IRabbitMQPubSub pubSub)
        {
            _tokenService = new Lazy<ITokenService>(() =>
                new TokenService(crudKit, options));
            _userService = new Lazy<IUserService>(() =>
                new UserService(userManager, signInManager, contextAccessor, crudKit, pubSub));
        }

        public ITokenService Token => _tokenService.Value;

        public IUserService User => _userService.Value;
    }
}

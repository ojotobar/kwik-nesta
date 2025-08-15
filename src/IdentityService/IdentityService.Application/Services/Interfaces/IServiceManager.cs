namespace IdentityService.Application.Services.Interfaces
{
    public interface IServiceManager
    {
        ITokenService Token { get; }
        IUserService User { get; }
    }
}

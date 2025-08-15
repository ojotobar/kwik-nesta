namespace IdentityService.Domain.Interfaces
{
    public interface IIdentityService
    {
        Task<(bool Succeeded, string Token)> RegisterAsync(string email, string password, string firstName, string lastName);
        Task<(bool Succeeded, string Token)> LoginAsync(string email, string password);
    }
}
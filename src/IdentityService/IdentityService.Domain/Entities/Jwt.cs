namespace IdentityService.Domain.Entities
{
    public class Jwt
    {
        public string PrivateKey { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public int Span { get; set; } = 1;
        public string Issuer { get; set; } = string.Empty;
        public List<string> Audience { get; set; } = [];
    }
}

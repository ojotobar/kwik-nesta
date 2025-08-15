using EFCore.CrudKit.Library.Data.Interfaces;
using IdentityService.Application.Services.Interfaces;
using IdentityService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace IdentityService.Application.Services
{
    public class TokenService : ITokenService
    {
        private readonly IEFCoreCrudKit _db;
        private readonly IOptions<Jwt> config;
        private readonly Jwt _config;
        private readonly RSA _rsa;

        public TokenService(IEFCoreCrudKit db, IOptions<Jwt> config, RSA rsa)
        {
            _db = db;
            this.config = config;
            _config = config.Value;
            _rsa = rsa;
        }

        public string CreateAccessToken(AppUser user, string[] roles)
        {
            var now = DateTime.UtcNow;
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Iss, _config.Issuer),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, user.UserName ?? ""),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim("client_id", _config.ClientId) // optional
            };
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = now.Add(TimeSpan.FromHours(_config.Span)),
                IssuedAt = now,
                Issuer = _config.Issuer,
                Audience = _config.Audience.Count == 1 ? _config.Audience.First() : null, // If single audience set Audience
                SigningCredentials = new SigningCredentials(new RsaSecurityKey(_rsa), SecurityAlgorithms.RsaSha256)
            };

            var handler = new JwtSecurityTokenHandler();
            var token = handler.CreateJwtSecurityToken(tokenDescriptor);

            if (_config.Audience.Count > 1)
            {
                // add 'aud' as array manually
                var jwt = new JwtSecurityToken(
                    issuer: _config.Issuer,
                    audience: null,
                    claims: token.Claims,
                    notBefore: token.ValidFrom,
                    expires: token.ValidTo,
                    signingCredentials: token.SigningCredentials
                );
                var payloadDict = jwt.Payload;
                payloadDict["aud"] = _config.Audience;
                return handler.WriteToken(jwt);
            }

            return handler.WriteToken(token);
        }

        public async Task<string> CreateAndSaveRefreshTokenAsync(string userId)
        {
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            var hash = ComputeHash(token);
            var record = new RefreshToken
            {
                Id = Guid.NewGuid(),
                TokenHash = hash,
                UserId = userId,
                ClientId = _config.ClientId,
                ExpiresAt = DateTimeOffset.UtcNow.Add(TimeSpan.FromDays(_config.Span))
            };

            await _db.InsertAsync(record);
            return token;
        }

        public async Task<RefreshToken?> ValidateRefreshTokenAsync(string token)
        {
            var hash = ComputeHash(token);
            var rec = await _db.AsQueryable<RefreshToken>(r => r.TokenHash == hash && r.ClientId == _config.ClientId, false)
                .FirstOrDefaultAsync();

            if (rec == null || rec.RevokedAt != null || rec.ExpiresAt < DateTimeOffset.UtcNow)
            {
                return null;
            }
            return rec;
        }

        private string ComputeHash(string token)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(bytes);
        }
    }
}

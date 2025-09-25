


using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EV_ChargingStationBooking_system_EAD.Api.Common;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EV_ChargingStationBooking_system_EAD.Api.Services
{
    public interface IJwtTokenService
    {
        string CreateToken(string userId, string username, string role);
    }
    public sealed class JwtTokenService : IJwtTokenService
    {
        private readonly JwtOptions _opt;
        private readonly SigningCredentials _creds;

        public JwtTokenService(IOptions<JwtOptions> opt)
        {
            _opt = opt.Value;
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.Key));
            _creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        }

        public string CreateToken(string userId, string username, string role)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(JwtRegisteredClaimNames.UniqueName, username),
                new Claim(ClaimTypes.Role, role)
            };

            var token = new JwtSecurityToken(
                issuer: _opt.Issuer,
                audience: _opt.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_opt.ExpiresMinutes),
                signingCredentials: _creds);

            return new JwtSecurityTokenHandler().WriteToken(token);   
        }
    }
}
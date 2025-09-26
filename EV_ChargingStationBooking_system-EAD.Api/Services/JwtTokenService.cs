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
        // username is email for staff, NIC for owners; role is Title-case (Backoffice|Operator|EvOwner)
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
            // Normalize role to Title-case for the token
            string normalizedRole = role switch
            {
                "BACKOFFICE" or "backoffice" or "Backoffice" => "Backoffice",
                "OPERATOR"   or "operator"   or "Operator"   => "Operator",
                "EV_OWNER"   or "ev_owner"   or "EvOwner"    => "EvOwner",
                _ => role
            };

            var claims = new[]
            {
                // Use username as 'sub' (your requested format)
                new Claim(JwtRegisteredClaimNames.Sub, username),
                // Keep unique_name too (handy in code)
                new Claim(JwtRegisteredClaimNames.UniqueName, username),
                // Standard ASP.NET Core role claim
                new Claim(ClaimTypes.Role, normalizedRole)
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

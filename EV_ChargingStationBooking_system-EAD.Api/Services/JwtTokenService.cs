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

            if (string.IsNullOrWhiteSpace(_opt.Key) || _opt.Key.Length < 32)
                throw new InvalidOperationException("Jwt:Key must be a long random string (>=32 chars).");

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

            var now = DateTime.UtcNow;

            var claims = new List<Claim>
            {
                // Standard subject should be the stable user id
                new Claim(JwtRegisteredClaimNames.Sub, userId),

                // App-friendly names (your Me() reads "unique_name")
                new Claim(JwtRegisteredClaimNames.UniqueName, username),
                new Claim(ClaimTypes.Name, username),

                // Role claim recognized by ASP.NET Core
                new Claim(ClaimTypes.Role, normalizedRole),

                // Good practice metadata
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
                new Claim(JwtRegisteredClaimNames.Iat, Epoch(now).ToString(), ClaimValueTypes.Integer64)
            };

            var token = new JwtSecurityToken(
                issuer: _opt.Issuer,
                audience: string.IsNullOrWhiteSpace(_opt.Audience) ? null : _opt.Audience,
                claims: claims,
                notBefore: now,
                expires: now.AddMinutes(_opt.ExpiresMinutes <= 0 ? 60 : _opt.ExpiresMinutes),
                signingCredentials: _creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static long Epoch(DateTime dtUtc) =>
            (long)Math.Floor((dtUtc - DateTime.UnixEpoch).TotalSeconds);
    }
}

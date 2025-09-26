using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EV_ChargingStationBooking_system_EAD.Api.Common;
using EV_ChargingStationBooking_system_EAD.Api.Domain; // for Role constants
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EV_ChargingStationBooking_system_EAD.Api.Services
{
    public interface IJwtTokenService
    {
        // userId: stable ID for the subject (use NIC for owners, GUID/DB id for staff)
        // username: email for staff, email (or NIC) for owners (used for display)
        // role: one of Role.Backoffice / Role.Operator / Role.EvOwner (constants)
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
            // Map role to EXACT constants your policies use
            var roleForClaim = role switch
            {
                "BACKOFFICE" or "backoffice" or "Backoffice" => Role.Backoffice, // "BACKOFFICE"
                "OPERATOR"   or "operator"   or "Operator"   => Role.Operator,   // "OPERATOR"
                "EV_OWNER"   or "ev_owner"   or "EvOwner"    => Role.EvOwner,    // "EV_OWNER"
                _ => role
            };

            var now = DateTime.UtcNow;

            var claims = new List<Claim>
            {
                // Subject = stable id (for owners pass NIC here!)
                new Claim(JwtRegisteredClaimNames.Sub, userId),

                // Names for convenience
                new Claim(JwtRegisteredClaimNames.UniqueName, username),
                new Claim(ClaimTypes.Name, username),

                // Role that matches [Authorize(Roles = "...")]
                new Claim(ClaimTypes.Role, roleForClaim),

                // Helpful metadata
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
                new Claim(JwtRegisteredClaimNames.Iat, Epoch(now).ToString(), ClaimValueTypes.Integer64)
            };

            // If this is an EV owner, also include explicit "nic" claim
            if (roleForClaim == Role.EvOwner)
            {
                claims.Add(new Claim("nic", userId));
                // also set NameIdentifier to the same for convenience
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
            }

            var token = new JwtSecurityToken(
                issuer: _opt.Issuer,
                audience: _opt.Audience,             // <-- make sure ValidateAudience matches this
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

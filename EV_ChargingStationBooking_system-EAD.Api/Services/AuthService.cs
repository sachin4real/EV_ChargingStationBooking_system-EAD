using BCrypt.Net;
using EV_ChargingStationBooking_system_EAD.Api.Domain;
using EV_ChargingStationBooking_system_EAD.Api.Domain.Entities;
using EV_ChargingStationBooking_system_EAD.Api.Dtos;
using EV_ChargingStationBooking_system_EAD.Api.Infrastructure.Repositories;

namespace EV_ChargingStationBooking_system_EAD.Api.Services
{
    public interface IAuthService
    {
        Task<AuthUser> RegisterStaffAsync(RegisterStaffDto dto);
        Task<AuthUser> RegisterOwnerAsync(RegisterOwnerDto dto);
        Task<AuthUser> ValidateCredentialsAsync(string username, string password);
        Task EnsureSeedBackofficeAsync();
    }

    public sealed class AuthService : IAuthService
    {
        private readonly IAuthUserRepository _repo;

        public AuthService(IAuthUserRepository repo) => _repo = repo;

        public async Task<AuthUser> RegisterStaffAsync(RegisterStaffDto dto)
        {
            // Normalize incoming role to our Title-case constants
            var reqRole = (dto.Role ?? "").Trim();
            string normalized = reqRole.Equals(Role.Backoffice, StringComparison.OrdinalIgnoreCase) ? Role.Backoffice
                               : reqRole.Equals(Role.Operator,   StringComparison.OrdinalIgnoreCase) ? Role.Operator
                               : "";

            if (string.IsNullOrEmpty(normalized))
                throw new InvalidOperationException("Invalid role. Use Backoffice or Operator.");

            var existing = await _repo.GetByUsernameAsync(dto.Email);
            if (existing != null) throw new InvalidOperationException("User exists.");

            var hash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            var u = new AuthUser
            {
                Id = Guid.NewGuid().ToString("N"),
                Username = dto.Email,
                PasswordHash = hash,
                Role = normalized // store Title-case
            };
            await _repo.CreateAsync(u);
            return u;
        }

        public async Task<AuthUser> RegisterOwnerAsync(RegisterOwnerDto dto)
        {
            var existing = await _repo.GetByUsernameAsync(dto.Nic);
            if (existing != null) throw new InvalidOperationException("Owner user exists.");

            var u = new AuthUser
            {
                Id = Guid.NewGuid().ToString("N"),
                Username = dto.Nic, // NIC = login
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = Role.EvOwner, // Title-case
                OwnerNic = dto.Nic
            };
            await _repo.CreateAsync(u);
            return u;
        }

        public async Task<AuthUser> ValidateCredentialsAsync(string username, string password)
        {
            var user = await _repo.GetByUsernameAsync(username) ?? throw new UnauthorizedAccessException("Invalid credentials.");
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                throw new UnauthorizedAccessException("Invalid credentials.");
            return user;
        }

        public async Task EnsureSeedBackofficeAsync()
        {
            if (await _repo.CountBackofficeAsync() == 0)
            {
                var u = new AuthUser
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Username = "admin@ev.local",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                    Role = Role.Backoffice // Title-case
                };
                await _repo.CreateAsync(u);
            }
        }
    }
}

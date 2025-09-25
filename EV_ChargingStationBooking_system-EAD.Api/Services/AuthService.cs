
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
            if (dto.Role != Role.Backoffice && dto.Role != Role.Operator)
                throw new InvalidOperationException("Invalid role for staff.");

            var existing = await _repo.GetByUsernameAsync(dto.Email);
            if (existing != null) throw new InvalidOperationException("User exists.");

            var hash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            var u = new AuthUser
            {
                Id = Guid.NewGuid().ToString("N"),
                Username = dto.Email,
                PasswordHash = hash,
                Role = dto.Role
            };
            await _repo.CreateAsync(u);
            return u;
        }

        public async Task<AuthUser> RegisterOwnerAsync(RegisterOwnerDto dto)
        {
            var existing = await _repo.GetByUsernameAsync(dto.Nic);
            if (existing != null) throw new InvalidOperationException("Owner user exists.");

            var hash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            var u = new AuthUser
            {
                Id = Guid.NewGuid().ToString("N"),
                Username = dto.Nic,                 // login with NIC
                PasswordHash = hash,
                Role = Role.EvOwner,
                OwnerNic = dto.Nic
            };
            await _repo.CreateAsync(u);
            return u;
        }

        public async Task<AuthUser> ValidateCredentialsAsync(string username, string password)
        {
            var user = await _repo.GetByUsernameAsync(username) ?? throw new UnauthorizedAccessException("Invalid credentials.");
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                throw new UnauthorizedAccessException("Invalid Credentials.");
            return user;
        }

        public async Task EnsureSeedBackofficeAsync()
        {
            if (await _repo.CountBackofficeAsync() == 0)
            {
                var hash = BCrypt.Net.BCrypt.HashPassword("Admin@123");
                var u = new AuthUser
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Username = "admin@ev.local",
                    PasswordHash = hash,
                    Role = Role.Backoffice
                };
                await _repo.CreateAsync(u);
            }
        }
    }
}
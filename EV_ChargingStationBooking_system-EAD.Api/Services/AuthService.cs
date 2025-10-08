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
        private readonly IEvOwnerRepository _owners;

        public AuthService(IAuthUserRepository repo, IEvOwnerRepository owners)
        {
            _repo = repo;
            _owners = owners;
        } 

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
            var nic   = dto.Nic.Trim();
            var email = dto.Email.Trim().ToLowerInvariant();
            var name  = dto.FullName?.Trim() ?? string.Empty;
            var phone = dto.Phone?.Trim() ?? string.Empty;

            
            if (await _repo.GetByUsernameAsync(nic) is not null)
                throw new InvalidOperationException("NIC is already registered.");
            if (await _owners.ExistsNicAsync(nic))
                throw new InvalidOperationException("NIC is already registered.");
            if (await _owners.ExistsEmailAsync(email))
                throw new InvalidOperationException("Email is already in use.");

            // Create AUTH user (login = NIC)
            var user = new AuthUser
            {
                Id = Guid.NewGuid().ToString("N"),
                Username = nic,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = Role.EvOwner,  
                OwnerNic = nic,
                FullName = name,
                Phone = phone
            };
            await _repo.CreateAsync(user);

            
            await _owners.CreateAsync(new EvOwner
            {
                Nic = nic,
                FullName = name,
                Email = email,
                Phone = phone,
                IsActive = true
            });

            return user;
        }

        public async Task<AuthUser> ValidateCredentialsAsync(string username, string password)
        {
            var user = await _repo.GetByUsernameAsync(username) ?? throw new UnauthorizedAccessException("Invalid credentials.");
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                throw new UnauthorizedAccessException("Invalid credentials.");

            if (user.Role == Role.EvOwner)
            {
                var nic = user.OwnerNic ?? user.Username;
                var owner = await _owners.GetByNicAsync(nic);
                if (owner is null || !owner.IsActive)
                    throw new UnauthorizedAccessException("Owner account is Deactivated. Please Contact Backoffice");
            }
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

using EV_ChargingStationBooking_system_EAD.Api.Domain;
using EV_ChargingStationBooking_system_EAD.Api.Domain.Entities;
using EV_ChargingStationBooking_system_EAD.Api.Dtos;
using EV_ChargingStationBooking_system_EAD.Api.Infrastructure.Repositories;

namespace EV_ChargingStationBooking_system_EAD.Api.Services
{
    public interface IEvOwnerService
    {
        // Admin
        Task<EvOwnerViewDto> AdminCreateAsync(EvOwnerCreateDto dto, string actorUserId);
        Task<EvOwnerViewDto> AdminGetAsync(string nic);
        Task<(IReadOnlyList<EvOwnerViewDto> items, long total)> AdminSearchAsync(OwnerListQuery q);
        Task<EvOwnerViewDto> AdminUpdateAsync(string nic, EvOwnerUpdateDto dto, string actorUserId);
        Task AdminSetActiveAsync(string nic, bool isActive, string actorUserId, string? reason);
        Task<bool> AdminHardDeleteAsync(string nic, string actorUserId); // returns true if deleted

        // Mobile self
        Task<EvOwnerViewDto> SelfGetAsync(string nicFromToken);
        Task<(EvOwnerViewDto profile, bool emailChanged)> SelfUpdateAsync(string nicFromToken, EvOwnerUpdateDto dto);
        Task SelfDeactivateAsync(string nicFromToken);

    }

    public sealed class EvOwnerService : IEvOwnerService
    {
        private readonly IEvOwnerRepository _owners;
        private readonly IAuthUserRepository _users;
        private readonly IBookingRepository _bookings;

        public EvOwnerService(IEvOwnerRepository owners, IAuthUserRepository users, IBookingRepository bookings)
        {
            _owners = owners; _users = users; _bookings = bookings;
        }

        // ---------------- Admin ----------------
        public async Task<EvOwnerViewDto> AdminCreateAsync(EvOwnerCreateDto dto, string actorUserId)
        {
            dto.Nic = dto.Nic.Trim();
            dto.Email = dto.Email.Trim().ToLowerInvariant();
            dto.FullName = dto.FullName?.Trim() ?? string.Empty;
            dto.Phone = dto.Phone?.Trim() ?? string.Empty;

            if (await _owners.ExistsNicAsync(dto.Nic))
                throw new InvalidOperationException("NIC is already registered.");
            if (await _owners.ExistsEmailAsync(dto.Email))
                throw new InvalidOperationException("Email is already in use.");

            var owner = new EvOwner
            {
                Nic = dto.Nic,
                FullName = dto.FullName,
                Email = dto.Email,
                Phone = dto.Phone,
                IsActive = true
            };
            await _owners.CreateAsync(owner);

            // Ensure there is also an auth user with Role=EvOwner using NIC as username
            var auth = await _users.GetByUsernameAsync(dto.Nic);
            if (auth is null)
            {
                var hash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
                await _users.CreateAsync(new AuthUser
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Username = dto.Nic,            // login = NIC
                    PasswordHash = hash,
                    Role = Role.EvOwner,           // Title-case if you adopted that
                    OwnerNic = dto.Nic,
                    FullName = dto.FullName,
                    Phone = dto.Phone
                });
            }

            return Map(owner);
        }

        public async Task<EvOwnerViewDto> AdminGetAsync(string nic)
            => Map(await _owners.GetByNicAsync(nic) ?? throw new KeyNotFoundException("Owner not found."));

        public async Task<(IReadOnlyList<EvOwnerViewDto> items, long total)> AdminSearchAsync(OwnerListQuery q)
        {
            var page = Math.Max(1, q.Page);
            var size = Math.Clamp(q.PageSize, 1, 100);
            var (items, total) = await _owners.SearchAsync(q.Q, q.IsActive, (page - 1) * size, size);
            return (items.Select(Map).ToList(), total);
        }

        public async Task<EvOwnerViewDto> AdminUpdateAsync(string nic, EvOwnerUpdateDto dto, string actorUserId)
        {
            dto.Email = dto.Email.Trim().ToLowerInvariant();
            dto.FullName = dto.FullName?.Trim() ?? string.Empty;
            dto.Phone = dto.Phone?.Trim() ?? string.Empty;

            var owner = await _owners.GetByNicAsync(nic) ?? throw new KeyNotFoundException("Owner not found.");

            if (!string.Equals(owner.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
            {
                if (await _owners.ExistsEmailAsync(dto.Email, excludeNic: nic))
                    throw new InvalidOperationException("Email is already in use.");
            }

            owner.FullName = dto.FullName;
            owner.Email = dto.Email;
            owner.Phone = dto.Phone;
            await _owners.UpdateAsync(owner);

            // Keep auth profile in sync (login remains NIC)
            var auth = await _users.GetByUsernameAsync(nic);
            if (auth is not null)
            {
                auth.FullName = owner.FullName;
                auth.Username = nic; // NIC stays the username
                auth.Phone = owner.Phone;
                await _users.UpdateAsync(auth);
            }

            return Map(owner);
        }

        public async Task AdminSetActiveAsync(string nic, bool isActive, string actorUserId, string? reason)
        {
            var owner = await _owners.GetByNicAsync(nic) ?? throw new KeyNotFoundException("Owner not found.");
            owner.IsActive = isActive;
            await _owners.UpdateAsync(owner);
            // TODO: audit log (actorUserId, reason)
        }

        public async Task<bool> AdminHardDeleteAsync(string nic, string actorUserId)
        {
            // only allow delete if no bookings exist (ever)
            if (await _bookings.ExistsForOwnerAsync(nic))
                throw new InvalidOperationException("Owner cannot be deleted because bookings exist; deactivate instead.");

            await _owners.DeleteAsync(nic);

            // Optional: also remove auth user (login is NIC)
            var auth = await _users.GetByUsernameAsync(nic);
            if (auth is not null)
                await _users.DeleteAsync(auth.Id);

            // TODO: audit log (actorUserId)
            return true;
        }

        // ---------------- Self (mobile) ----------------
        public async Task<EvOwnerViewDto> SelfGetAsync(string nicFromToken)
            => Map(await _owners.GetByNicAsync(nicFromToken) ?? throw new KeyNotFoundException("Owner not found."));

        public async Task<(EvOwnerViewDto profile, bool emailChanged)> SelfUpdateAsync(string nicFromToken, EvOwnerUpdateDto dto)
        {
            dto.Email = dto.Email.Trim().ToLowerInvariant();
            dto.FullName = dto.FullName?.Trim() ?? string.Empty;
            dto.Phone = dto.Phone?.Trim() ?? string.Empty;

            var owner = await _owners.GetByNicAsync(nicFromToken) ?? throw new KeyNotFoundException("Owner not found.");
            if (!owner.IsActive)
                throw new InvalidOperationException("Owner account is deactivated; please contact Backoffice.");

            var emailChanged = !string.Equals(owner.Email, dto.Email, StringComparison.OrdinalIgnoreCase);
            if (emailChanged && await _owners.ExistsEmailAsync(dto.Email, excludeNic: nicFromToken))
                throw new InvalidOperationException("Email is already in use.");

            owner.FullName = dto.FullName;
            owner.Email = dto.Email;
            owner.Phone = dto.Phone;
            await _owners.UpdateAsync(owner);

            // Keep auth profile in sync for convenience (login remains NIC)
            var auth = await _users.GetByUsernameAsync(nicFromToken);
            if (auth is not null)
            {
                auth.FullName = owner.FullName;
                auth.Phone = owner.Phone;
                await _users.UpdateAsync(auth);
            }

            return (Map(owner), emailChanged);
        }

        public async Task SelfDeactivateAsync(string nicFromToken)
        {
            var owner = await _owners.GetByNicAsync(nicFromToken) ?? throw new KeyNotFoundException("Owner not found.");
            owner.IsActive = false;
            await _owners.UpdateAsync(owner);
        }

        // ---------------- Mapping ----------------
        private static EvOwnerViewDto Map(EvOwner o) => new()
        {
            Nic = o.Nic,
            FullName = o.FullName,
            Email = o.Email,
            Phone = o.Phone,
            IsActive = o.IsActive,
            CreatedAtUtc = o.CreatedAtUtc,
            UpdatedAtUtc = o.UpdatedAtUtc
        };

    }
}

using EV_ChargingStationBooking_system_EAD.Api.Domain;
using EV_ChargingStationBooking_system_EAD.Api.Dtos;
using EV_ChargingStationBooking_system_EAD.Api.Domain.Entities;
using EV_ChargingStationBooking_system_EAD.Api.Infrastructure.Repositories;

namespace EV_ChargingStationBooking_system_EAD.Api.Services
{
    public interface IAdminStaffService
    {
        Task<(IReadOnlyList<StaffUserViewDto> items, long total)> SearchAsync(StaffListQuery q);
        Task<StaffUserViewDto> GetAsync(string id);

        // No create here (AuthService handles hashing); just allow profile/role/stations update.
        Task<StaffUserViewDto> UpdateAsync(string id, StaffUpdateDto dto);
    }

    public sealed class AdminStaffService : IAdminStaffService
    {
        private readonly IAuthUserRepository _repo;
        public AdminStaffService(IAuthUserRepository repo) => _repo = repo;

        public async Task<(IReadOnlyList<StaffUserViewDto> items, long total)> SearchAsync(StaffListQuery q)
        {
            var page = Math.Max(1, q.Page);
            var size = Math.Clamp(q.PageSize, 1, 100);

            var (items, total) = await _repo.SearchStaffAsync(q.Q, q.Role, (page - 1) * size, size);
            return (items.Select(Map).ToList(), total);
        }

        public async Task<StaffUserViewDto> GetAsync(string id)
        {
            var u = await _repo.GetByIdAsync(id) ?? throw new KeyNotFoundException("Staff user not found.");
            if (u.Role != Role.Backoffice && u.Role != Role.Operator)
                throw new KeyNotFoundException("Staff user not found.");
            return Map(u);
        }

        public async Task<StaffUserViewDto> UpdateAsync(string id, StaffUpdateDto dto)
        {
            var u = await _repo.GetByIdAsync(id) ?? throw new KeyNotFoundException("Staff user not found.");

            if (!string.IsNullOrWhiteSpace(dto.FullName))
                u.FullName = dto.FullName.Trim();

            if (!string.IsNullOrWhiteSpace(dto.Phone))
                u.Phone = dto.Phone.Trim();

            if (!string.IsNullOrWhiteSpace(dto.Role))
                u.Role = dto.Role; // "Backoffice" | "Operator"

            if (u.Role == Role.Operator)
            {
                if (dto.StationIds != null)
                {
                    if (dto.StationIds.Count == 0)
                        throw new ArgumentException("Operator must be assigned to at least one station.");
                    u.StationIds = dto.StationIds.Distinct().ToList();
                }
            }
            else
            {
                // Backoffice has no station scope
                u.StationIds = new();
            }

            await _repo.UpdateAsync(u);
            return Map(u);
        }

        private static StaffUserViewDto Map(AuthUser u) => new()
        {
            Id           = u.Id,
            Email        = u.Username,
            Role         = u.Role,
            FullName     = u.FullName,
            Phone        = u.Phone,
            CreatedAtUtc = u.CreatedAtUtc,
            StationIds   = u.StationIds?.ToList() ?? new List<string>()
        };
    }
}

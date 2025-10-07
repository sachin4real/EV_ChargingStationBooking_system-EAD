using EV_ChargingStationBooking_system_EAD.Api.Dtos;
using EV_ChargingStationBooking_system_EAD.Api.Domain.Entities;
using EV_ChargingStationBooking_system_EAD.Api.Infrastructure.Repositories;

namespace EV_ChargingStationBooking_system_EAD.Api.Services
{
    public interface IAdminStaffService
    {
        Task<(IReadOnlyList<StaffUserViewDto> items, long total)> SearchAsync(StaffListQuery q);
        Task<StaffUserViewDto> GetAsync(string id);
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
            if (u.Role != Domain.Role.Backoffice && u.Role != Domain.Role.Operator)
                throw new KeyNotFoundException("Staff user not found.");
            return Map(u);
        }

        private static StaffUserViewDto Map(AuthUser u) => new()
        {
            Id = u.Id,
            Email = u.Username,
            Role = u.Role,
            FullName = u.FullName,
            Phone = u.Phone,
            CreatedAtUtc = u.CreatedAtUtc
        };
    }
}

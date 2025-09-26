using System.Collections.Generic;
using System.Threading.Tasks;
using EV_ChargingStationBooking_system_EAD.Api.Domain.Entities;

namespace EV_ChargingStationBooking_system_EAD.Api.Infrastructure.Repositories
{
    public interface IAuthUserRepository
    {
        Task<AuthUser?> GetByUsernameAsync(string username);
        Task<AuthUser?> GetByIdAsync(string id);
        Task CreateAsync(AuthUser u);
        Task<long> CountBackofficeAsync();

        // helpers used by controllers
        Task<Dictionary<string, string>> GetUsernamesByIdsAsync(IEnumerable<string> ids);
        Task UpdatePasswordHashAsync(string userId, string newHash);
        Task UpdateIsActiveAsync(string userId, bool isActive);
    }
}

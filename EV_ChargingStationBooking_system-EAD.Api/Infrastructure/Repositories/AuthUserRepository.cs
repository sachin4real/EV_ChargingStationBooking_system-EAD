using EV_ChargingStationBooking_system_EAD.Api.Domain.Entities;
using EV_ChargingStationBooking_system_EAD.Api.Infrastructure.Mongo;
using MongoDB.Driver;

namespace EV_ChargingStationBooking_system_EAD.Api.Infrastructure.Repositories
{
    public interface IAuthUserRepository
    {
        Task<AuthUser?> GetByUsernameAsync(string username);
        Task<AuthUser?> GetByIdAsync(string id);
        Task CreateAsync(AuthUser u);
        Task<long> CountBackofficeAsync();
    }

    public sealed class AuthUserRepository : IAuthUserRepository
    {
        private readonly IMongoCollection<AuthUser> _col;
        public AuthUserRepository(MongoContext ctx)
        {
            _col = ctx.GetCollection<AuthUser>("users_auth");
        }
        public Task<AuthUser?> GetByUsernameAsync(string username) => _col.Find(x =>
        x.Username == username).FirstOrDefaultAsync();

        public Task<AuthUser?> GetByIdAsync(string id) => _col.Find(x => x.Id == id).FirstOrDefaultAsync();

        public Task CreateAsync(AuthUser u) => _col.InsertOneAsync(u);

        public async Task<long> CountBackofficeAsync() => await _col.CountDocumentsAsync(x =>
        x.Role == Domain.Role.Backoffice);
    }
}
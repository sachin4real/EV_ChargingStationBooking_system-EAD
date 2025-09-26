using EV_ChargingStationBooking_system_EAD.Api.Domain.Entities;
using MongoDB.Driver;

namespace EV_ChargingStationBooking_system_EAD.Api.Infrastructure.Mongo;

public sealed class IndexInitializer
{
    private readonly MongoContext _ctx;
    public IndexInitializer(MongoContext ctx) => _ctx = ctx;

    public async Task EnsureAsync()
    {
        // users_auth: unique username (email/NIC)
        var users = _ctx.GetCollection<AuthUser>("users_auth");
        await users.Indexes.CreateOneAsync(new CreateIndexModel<AuthUser>(
            Builders<AuthUser>.IndexKeys.Ascending(x => x.Username),
            new CreateIndexOptions { Unique = true, Name = "ux_users_username" }));

        // operators: search helpers
        var ops = _ctx.GetCollection<Operator>("operators");
        await ops.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<Operator>(Builders<Operator>.IndexKeys.Ascending(o => o.Name)),
            new CreateIndexModel<Operator>(Builders<Operator>.IndexKeys.Ascending(o => o.Phone))
        });

        // owners: NIC is _id => already unique; add name/phone search helpers if you like
        var owners = _ctx.GetCollection<EvOwner>("owners");
        await owners.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<EvOwner>(Builders<EvOwner>.IndexKeys.Ascending(o => o.FullName)),
            new CreateIndexModel<EvOwner>(Builders<EvOwner>.IndexKeys.Ascending(o => o.Phone))
        });
    }
}

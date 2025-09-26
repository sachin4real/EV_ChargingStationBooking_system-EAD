using EV_ChargingStationBooking_system_EAD.Api.Domain.Entities;
using EV_ChargingStationBooking_system_EAD.Api.Infrastructure.Mongo;
using MongoDB.Driver;

namespace EV_ChargingStationBooking_system_EAD.Api.Infrastructure.Repositories;

public interface IOwnerRepository
{
    IMongoCollection<EvOwner> Col { get; }
}

public sealed class OwnerRepository : IOwnerRepository
{
    public IMongoCollection<EvOwner> Col { get; }
    public OwnerRepository(MongoContext ctx) => Col = ctx.GetCollection<EvOwner>("owners");
}

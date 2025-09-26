using EV_ChargingStationBooking_system_EAD.Api.Domain.Entities;
using EV_ChargingStationBooking_system_EAD.Api.Infrastructure.Mongo;
using MongoDB.Driver;

namespace EV_ChargingStationBooking_system_EAD.Api.Infrastructure.Repositories;

public interface IOperatorRepository
{
    IMongoCollection<Operator> Col { get; }
}

public sealed class OperatorRepository : IOperatorRepository
{
    public IMongoCollection<Operator> Col { get; }
    public OperatorRepository(MongoContext ctx) => Col = ctx.GetCollection<Operator>("operators");
}

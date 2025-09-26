using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace EV_ChargingStationBooking_system_EAD.Api.Infrastructure.Mongo
{
    public sealed class MongoContext
    {
        public IMongoDatabase Database { get; }
        public MongoContext(IOptions<MongoOptions> options)
        {
            var client = new MongoClient(options.Value.ConnectionString);
            Database = client.GetDatabase(options.Value.Database);
        }

        public IMongoCollection<T> GetCollection<T>(string name) => Database.GetCollection<T>(name);
    }
}
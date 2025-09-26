namespace EV_ChargingStationBooking_system_EAD.Api.Infrastructure.Mongo
{
    public sealed class MongoOptions
    {
        public string ConnectionString { get; set; } = "";
        public string Database { get; set; } = "";
    }
}
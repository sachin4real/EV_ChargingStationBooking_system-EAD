using EV_ChargingStationBooking_system_EAD.Api.Domain;
using EV_ChargingStationBooking_system_EAD.Api.Domain.Entities;
using EV_ChargingStationBooking_system_EAD.Api.Dtos;
using EV_ChargingStationBooking_system_EAD.Api.Infrastructure.Repositories;
using EV_ChargingStationBooking_system_EAD.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EV_ChargingStationBooking_system_EAD.Api.Controllers
{
    [ApiController]
    [Route("api/operators")]
    public sealed class OperatorsScanController : ControllerBase
    {
        private readonly IQrService _qr;
        private readonly IBookingRepository _bookings;
        private readonly IEvOwnerRepository _owners;
        private readonly IChargingStationRepository _stations;

        public OperatorsScanController(IQrService qr, IBookingRepository bookings,
            IEvOwnerRepository owners, IChargingStationRepository stations)
        { _qr = qr; _bookings = bookings; _owners = owners; _stations = stations; }

        // POST /api/operators/scan   (Operator only)
        [HttpPost("scan")]
        [Authorize(Roles = Role.Operator)]
        public async Task<ActionResult> Scan([FromBody] OperatorScanRequest body)
        {
            var (bookingId, nic, expUtc) = _qr.ValidateOwnerBookingQr(body.Qr);

            var b = await _bookings.GetByIdAsync(bookingId) ?? throw new KeyNotFoundException("Booking not found.");
            if (b.Nic != nic) throw new InvalidOperationException("QR does not match owner.");
            if (b.Status != BookingStatus.Approved) throw new InvalidOperationException("Booking is not approved.");
            if (DateTime.UtcNow > b.EndTimeUtc) throw new InvalidOperationException("Booking already ended.");

            var owner = await _owners.GetByNicAsync(nic) ?? throw new KeyNotFoundException("Owner not found.");
            var station = await _stations.GetAsync(b.StationId) ?? throw new KeyNotFoundException("Station not found.");

            return Ok(new OperatorScanResponse
            {
                Booking = new BookingViewDto {
                    Id = b.Id, Nic = b.Nic, StationId = b.StationId,
                    StartTimeUtc = b.StartTimeUtc, EndTimeUtc = b.EndTimeUtc,
                    Status = b.Status, CreatedAtUtc = b.CreatedAtUtc, UpdatedAtUtc = b.UpdatedAtUtc
                },
                OwnerNic = owner.Nic,
                OwnerName = owner.FullName,
                StationId = station.Id,
                StationName = station.Name,
                ServerTimeUtc = DateTime.UtcNow
            });
        }
    }
}

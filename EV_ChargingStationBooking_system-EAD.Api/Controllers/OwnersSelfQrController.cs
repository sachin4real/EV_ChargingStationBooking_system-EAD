using System.Security.Claims;
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
    [Route("api/owners/me/bookings")]
    [Authorize(Roles = Role.EvOwner)]
    public sealed class OwnersSelfQrController : ControllerBase
    {
        private readonly IBookingRepository _bookings;
        private readonly IQrService _qr;

        public OwnersSelfQrController(IBookingRepository bookings, IQrService qr)
        { _bookings = bookings; _qr = qr; }

        private string Nic() =>
            User.FindFirstValue("nic") ??
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("sub") ??
            throw new UnauthorizedAccessException("NIC missing from token.");

        // GET /api/owners/me/bookings/{id}/qr
        [HttpGet("{id}/qr")]
        public async Task<ActionResult> GetQr(string id)
        {
            var b = await _bookings.GetByIdAsync(id) ?? throw new KeyNotFoundException("Booking not found.");
            if (b.Nic != Nic()) throw new UnauthorizedAccessException("Not your booking.");

            if (b.Status != BookingStatus.Approved)
                throw new InvalidOperationException("QR available only for approved bookings.");

            // optional: expire QR if booking already ended
            if (DateTime.UtcNow > b.EndTimeUtc)
                throw new InvalidOperationException("Booking already ended.");

            var (qr, exp) = _qr.CreateOwnerBookingQr(b.Id, b.Nic);
            return Ok(new OwnerBookingQrDto { Qr = qr, ExpiresAtUtc = exp });
        }
    }
}

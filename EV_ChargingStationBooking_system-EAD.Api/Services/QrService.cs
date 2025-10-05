using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EV_ChargingStationBooking_system_EAD.Api.Common;

namespace EV_ChargingStationBooking_system_EAD.Api.Services
{
    public interface IQrService
    {
        // create compact string like "<base64url(payload)>.<base64url(sig)>"
        (string qr, DateTime expiresAtUtc) CreateOwnerBookingQr(string bookingId, string nic);
        // validate and return payload
        (string bookingId, string nic, DateTime expUtc) ValidateOwnerBookingQr(string qr);
    }

    public sealed class QrService : IQrService
    {
        private readonly byte[] _key;
        private readonly int _ttl;

        private record Payload(string bid, string nic, long exp); // exp = epoch seconds

        public QrService(Microsoft.Extensions.Options.IOptions<QrOptions> opt)
        {
            var o = opt.Value;
            if (string.IsNullOrWhiteSpace(o.Secret) || o.Secret.Length < 32)
                throw new InvalidOperationException("Qr:Secret must be >=32 chars.");
            _key = Encoding.UTF8.GetBytes(o.Secret);
            _ttl = o.ExpiryMinutes > 0 ? o.ExpiryMinutes : 5;
        }

        public (string qr, DateTime expiresAtUtc) CreateOwnerBookingQr(string bookingId, string nic)
        {
            var exp = DateTime.UtcNow.AddMinutes(_ttl);
            var payload = new Payload(bookingId, nic, ToEpoch(exp));
            var json = JsonSerializer.Serialize(payload);
            var p = Base64UrlEncode(Encoding.UTF8.GetBytes(json));
            var s = Base64UrlEncode(HmacSha256(p));
            return ($"{p}.{s}", exp);
        }

        public (string bookingId, string nic, DateTime expUtc) ValidateOwnerBookingQr(string qr)
        {
            var parts = qr.Split('.');
            if (parts.Length != 2) throw new InvalidOperationException("Invalid QR format.");

            var p = parts[0];
            var s = parts[1];
            var sig = Base64UrlEncode(HmacSha256(p));
            if (!SlowEquals(sig, s)) throw new InvalidOperationException("Invalid QR signature.");

            var json = Encoding.UTF8.GetString(Base64UrlDecode(p));
            var payload = JsonSerializer.Deserialize<Payload>(json) ?? throw new InvalidOperationException("Invalid QR payload.");

            var expUtc = FromEpoch(payload.exp);
            if (DateTime.UtcNow > expUtc) throw new InvalidOperationException("QR expired.");

            return (payload.bid, payload.nic, expUtc);
        }

        private byte[] HmacSha256(string msg)
        {
            using var h = new HMACSHA256(_key);
            return h.ComputeHash(Encoding.UTF8.GetBytes(msg));
        }

        private static string Base64UrlEncode(byte[] data) =>
            Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        private static byte[] Base64UrlDecode(string s)
        {
            s = s.Replace('-', '+').Replace('_', '/');
            switch (s.Length % 4) { case 2: s += "=="; break; case 3: s += "="; break; }
            return Convert.FromBase64String(s);
        }

        private static bool SlowEquals(string a, string b)
        {
            var ab = Encoding.UTF8.GetBytes(a);
            var bb = Encoding.UTF8.GetBytes(b);
            if (ab.Length != bb.Length) return false;
            int diff = 0;
            for (int i = 0; i < ab.Length; i++) diff |= ab[i] ^ bb[i];
            return diff == 0;
        }

        private static long ToEpoch(DateTime utc) => (long)Math.Floor((utc - DateTime.UnixEpoch).TotalSeconds);
        private static DateTime FromEpoch(long s) => DateTime.UnixEpoch.AddSeconds(s);
    }
}

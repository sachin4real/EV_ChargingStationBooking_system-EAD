using EV_ChargingStationBooking_system_EAD.Api.Domain;
using EV_ChargingStationBooking_system_EAD.Api.Dtos;
using EV_ChargingStationBooking_system_EAD.Api.Infrastructure.Repositories;

namespace EV_ChargingStationBooking_system_EAD.Api.Services
{
    public interface IMyProfileService
    {
        Task<MyProfileDto> GetAsync(string userId);

        // Single method: updates profile, and if CurrentPassword/NewPassword are provided, changes password too.
        Task<(MyProfileDto profile, bool emailChanged)> UpdateAsync(string userId, MyProfileUpdateDto dto);
    }

    public sealed class MyProfileService : IMyProfileService
    {
        private readonly IAuthUserRepository _users;
        public MyProfileService(IAuthUserRepository users) => _users = users;

        public async Task<MyProfileDto> GetAsync(string userId)
        {
            var u = await _users.GetByIdAsync(userId) ?? throw new KeyNotFoundException("User not found.");
            return new MyProfileDto
            {
                Username = u.Username,
                FullName = u.FullName,
                Phone    = u.Phone,
                Role     = u.Role
            };
        }

        public async Task<(MyProfileDto profile, bool emailChanged)> UpdateAsync(string userId, MyProfileUpdateDto dto)
        {
            var u = await _users.GetByIdAsync(userId) ?? throw new KeyNotFoundException("User Not Found.");
            if (u.Role != Role.Backoffice && u.Role != Role.Operator && u.Role != Role.EvOwner)
                throw new UnauthorizedAccessException("Only Staff can edit this Profile.");

            // ---- OPTIONAL password change (only if any password field is supplied)
            var wantsPwChange = !string.IsNullOrWhiteSpace(dto.CurrentPassword)
                             || !string.IsNullOrWhiteSpace(dto.NewPassword);

            if (wantsPwChange)
            {
                if (string.IsNullOrWhiteSpace(dto.CurrentPassword) ||
                    string.IsNullOrWhiteSpace(dto.NewPassword))
                    throw new InvalidOperationException("To change password, provide both currentPassword and newPassword.");

                if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, u.PasswordHash))
                    throw new UnauthorizedAccessException("Current password is incorrect.");

                if (dto.NewPassword.Length < 8)
                    throw new InvalidOperationException("New password must be at least 8 characters.");

                u.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            }

            // ---- Profile bits
            var emailTrimmed = dto.Email?.Trim() ?? "";
            var emailChanged = !string.Equals(u.Username, emailTrimmed, StringComparison.OrdinalIgnoreCase);
            if (emailChanged)
            {
                var inUse = await _users.EmailInUseAsync(emailTrimmed, u.Id);
                if (inUse) throw new InvalidOperationException("Email already in use.");
                u.Username = emailTrimmed;
            }

            u.FullName = dto.FullName?.Trim();
            u.Phone    = dto.Phone?.Trim();

            await _users.UpdateAsync(u);

            return (new MyProfileDto
            {
                Username = u.Username,
                FullName = u.FullName,
                Phone    = u.Phone,
                Role     = u.Role
            }, emailChanged);
        }
    }
}

using EnvironmentalSurvey.DTOs;

namespace EnvironmentalSurvey.Services
{
    public interface IUserService
    {
        Task<IEnumerable<UserDto>> GetAllUsersAsync(string? role, string? registrationStatus, int page, int pageSize);
        Task<UserDto?> GetUserByIdAsync(int id);
        Task<UserDto> RegisterUserAsync(RegisterDto registerDto);
        Task<LoginResponseDto?> LoginAsync(LoginDto loginDto);
        Task<UserDto?> UpdateUserAsync(int id, UpdateUserDto updateDto);
        Task<bool> ApproveRegistrationAsync(int id);
        Task<bool> RejectRegistrationAsync(int id);
        Task<bool> DeleteUserAsync(int id);
        Task<bool> DeactivateUserAsync(int id);
        Task<UserStatisticsDto?> GetUserStatisticsAsync(int id);
        Task<IEnumerable<UserDto>> GetPendingRegistrationsAsync();
        Task<bool> ChangePasswordAsync(int id, ChangePasswordDto changePasswordDto);
    }
}
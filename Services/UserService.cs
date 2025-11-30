using EnvironmentalSurvey.Data;
using EnvironmentalSurvey.DTOs;
using EnvironmentalSurvey.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace EnvironmentalSurvey.Services
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public UserService(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync(string? role, string? registrationStatus, int page, int pageSize)
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(role))
            {
                query = query.Where(u => u.Role == role);
            }

            if (!string.IsNullOrEmpty(registrationStatus))
            {
                query = query.Where(u => u.RegistrationStatus == registrationStatus);
            }

            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => MapToDto(u))
                .ToListAsync();

            return users;
        }

        public async Task<UserDto?> GetUserByIdAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            return user == null ? null : MapToDto(user);
        }

        public async Task<UserDto> RegisterUserAsync(RegisterDto registerDto)
        {
            // Check if username or email already exists
            if (await _context.Users.AnyAsync(u => u.Username == registerDto.Username))
            {
                throw new InvalidOperationException("Username already exists");
            }

            if (await _context.Users.AnyAsync(u => u.Email == registerDto.Email))
            {
                throw new InvalidOperationException("Email already exists");
            }

            var user = new User
            {
                Username = registerDto.Username,
                Email = registerDto.Email,
                PasswordHash = HashPassword(registerDto.Password),
                FullName = registerDto.FullName,
                Role = registerDto.Role,
                RollNumber = registerDto.RollNumber,
                EmployeeNumber = registerDto.EmployeeNumber,
                Class = registerDto.Class,
                Specification = registerDto.Specification,
                Section = registerDto.Section,
                AdmissionDate = registerDto.AdmissionDate,
                JoiningDate = registerDto.JoiningDate,
                RegistrationStatus = "pending",
                IsActive = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return MapToDto(user);
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginDto loginDto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == loginDto.Username);

            if (user == null || !VerifyPassword(loginDto.Password, user.PasswordHash))
            {
                return null;
            }

            if (!user.IsActive || user.RegistrationStatus != "approved")
            {
                return null;
            }

            var token = GenerateJwtToken(user);
            var expiresAt = DateTime.Now.AddHours(24);

            return new LoginResponseDto
            {
                Token = token,
                User = MapToDto(user),
                ExpiresAt = expiresAt
            };
        }

        public async Task<UserDto?> UpdateUserAsync(int id, UpdateUserDto updateDto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return null;

            if (!string.IsNullOrEmpty(updateDto.FullName))
                user.FullName = updateDto.FullName;

            if (!string.IsNullOrEmpty(updateDto.Email))
            {
                if (await _context.Users.AnyAsync(u => u.Email == updateDto.Email && u.UserId != id))
                {
                    throw new InvalidOperationException("Email already exists");
                }
                user.Email = updateDto.Email;
            }

            user.RollNumber = updateDto.RollNumber ?? user.RollNumber;
            user.EmployeeNumber = updateDto.EmployeeNumber ?? user.EmployeeNumber;
            user.Class = updateDto.Class ?? user.Class;
            user.Specification = updateDto.Specification ?? user.Specification;
            user.Section = updateDto.Section ?? user.Section;
            user.AdmissionDate = updateDto.AdmissionDate ?? user.AdmissionDate;
            user.JoiningDate = updateDto.JoiningDate ?? user.JoiningDate;
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return MapToDto(user);
        }

        public async Task<bool> ApproveRegistrationAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;

            user.RegistrationStatus = "approved";
            user.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RejectRegistrationAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;

            user.RegistrationStatus = "rejected";
            user.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeactivateUserAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;

            user.IsActive = false;
            user.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ActivateUserAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;

            user.IsActive = true;
            user.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<UserStatisticsDto?> GetUserStatisticsAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return null;

            // Query các bảng liên quan trực tiếp thay vì dùng navigation properties
            var surveysParticipated = await _context.SurveyResponses
                .Where(sr => sr.UserId == id)
                .CountAsync();

            var averageScore = await _context.SurveyResponses
                .Where(sr => sr.UserId == id && sr.Score.HasValue)
                .AverageAsync(sr => (decimal?)sr.Score);

            var lastParticipation = await _context.SurveyResponses
                .Where(sr => sr.UserId == id)
                .MaxAsync(sr => (DateTime?)sr.SubmittedAt);

            var competitionsWon = await _context.CompetitionWinners
                .Where(cw => cw.UserId == id)
                .CountAsync();

            var participationsSubmitted = await _context.EffectiveParticipations
                .Where(ep => ep.UserId == id)
                .CountAsync();

            return new UserStatisticsDto
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Role = user.Role,
                SurveysParticipated = surveysParticipated,
                AverageScore = averageScore,
                LastParticipation = lastParticipation,
                CompetitionsWon = competitionsWon,
                ParticipationsSubmitted = participationsSubmitted
            };
        }

        public async Task<IEnumerable<UserDto>> GetPendingRegistrationsAsync()
        {
            var users = await _context.Users
                .Where(u => u.RegistrationStatus == "pending")
                .OrderBy(u => u.CreatedAt)
                .Select(u => MapToDto(u))
                .ToListAsync();

            return users;
        }

        public async Task<bool> ChangePasswordAsync(int id, ChangePasswordDto changePasswordDto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;

            if (!VerifyPassword(changePasswordDto.CurrentPassword, user.PasswordHash))
            {
                return false;
            }

            user.PasswordHash = HashPassword(changePasswordDto.NewPassword);
            user.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }

        // Helper methods
        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        private static bool VerifyPassword(string password, string hash)
        {
            var hashOfInput = HashPassword(password);
            return hashOfInput == hash;
        }

        private string GenerateJwtToken(User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!"));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(24),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static UserDto MapToDto(User user)
        {
            return new UserDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role,
                RollNumber = user.RollNumber,
                EmployeeNumber = user.EmployeeNumber,
                Class = user.Class,
                Specification = user.Specification,
                Section = user.Section,
                AdmissionDate = user.AdmissionDate,
                JoiningDate = user.JoiningDate,
                RegistrationStatus = user.RegistrationStatus,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            };
        }
    }
}
using EnvironmentalSurvey.Data;
using EnvironmentalSurvey.DTOs;
using EnvironmentalSurvey.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace EnvironmentalSurvey.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UsersController> _logger;

        public UsersController(AppDbContext context, IConfiguration configuration, ILogger<UsersController> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        // GET: api/users
        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetAllUsers(
            [FromQuery] string? role = null,
            [FromQuery] string? registrationStatus = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
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

                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/users/{id}
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<UserDto>> GetUserById(int id)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (userRole != "admin" && currentUserId != id)
                {
                    return Forbid();
                }

                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                return Ok(MapToDto(user));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by id");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/users/register
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<UserDto>> Register([FromBody] RegisterDto registerDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (await _context.Users.AnyAsync(u => u.Username == registerDto.Username))
                {
                    return BadRequest(new { message = "Username already exists" });
                }

                if (await _context.Users.AnyAsync(u => u.Email == registerDto.Email))
                {
                    return BadRequest(new { message = "Email already exists" });
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

                return CreatedAtAction(nameof(GetUserById), new { id = user.UserId }, MapToDto(user));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering user");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/users/login
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == loginDto.Username);

                if (user == null || !VerifyPassword(loginDto.Password, user.PasswordHash))
                {
                    return Unauthorized(new { message = "Invalid credentials" });
                }

                if (!user.IsActive || user.RegistrationStatus != "approved")
                {
                    return Unauthorized(new { message = "Account not active or not approved" });
                }

                var token = GenerateJwtToken(user);
                var expiresAt = DateTime.Now.AddHours(24);

                return Ok(new LoginResponseDto
                {
                    Token = token,
                    User = MapToDto(user),
                    ExpiresAt = expiresAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return StatusCode(500, "Internal server error");
            }
        }

        // PUT: api/users/{id}
        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult<UserDto>> UpdateUser(int id, [FromBody] UpdateUserDto updateDto)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (userRole != "admin" && currentUserId != id)
                {
                    return Forbid();
                }

                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                if (!string.IsNullOrEmpty(updateDto.FullName))
                    user.FullName = updateDto.FullName;

                if (!string.IsNullOrEmpty(updateDto.Email))
                {
                    if (await _context.Users.AnyAsync(u => u.Email == updateDto.Email && u.UserId != id))
                    {
                        return BadRequest(new { message = "Email already exists" });
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
                return Ok(MapToDto(user));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user");
                return StatusCode(500, "Internal server error");
            }
        }

        // PUT: api/users/{id}/approve
        [HttpPut("{id}/approve")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult> ApproveRegistration(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                user.RegistrationStatus = "approved";
                user.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                return Ok(new { message = "User approved successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving user");
                return StatusCode(500, "Internal server error");
            }
        }

        // PUT: api/users/{id}/reject
        [HttpPut("{id}/reject")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult> RejectRegistration(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                user.RegistrationStatus = "rejected";
                user.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                return Ok(new { message = "User rejected successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting user");
                return StatusCode(500, "Internal server error");
            }
        }

        // DELETE: api/users/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult> DeleteUser(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user");
                return StatusCode(500, "Internal server error");
            }
        }

        // PUT: api/users/{id}/deactivate
        [HttpPut("{id}/deactivate")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult> DeactivateUser(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                user.IsActive = false;
                user.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                return Ok(new { message = "User deactivated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating user");
                return StatusCode(500, "Internal server error");
            }
        }
        [HttpPut("{id}/activate")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult> ActivateUser(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                user.IsActive = true;
                user.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                return Ok(new { message = "User activated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating user");
                return StatusCode(500, "Internal server error");
            }
        }
        // GET: api/users/{id}/statistics
        [HttpGet("{id}/statistics")]
        [Authorize]
        public async Task<ActionResult<UserStatisticsDto>> GetUserStatistics(int id)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (userRole != "admin" && currentUserId != id)
                {
                    return Forbid();
                }

                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                // Query trực tiếp thay vì dùng navigation properties
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

                var statistics = new UserStatisticsDto
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

                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user statistics");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/users/pending
        [HttpGet("pending")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetPendingRegistrations()
        {
            try
            {
                var users = await _context.Users
                    .Where(u => u.RegistrationStatus == "pending")
                    .OrderBy(u => u.CreatedAt)
                    .Select(u => MapToDto(u))
                    .ToListAsync();

                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending registrations");
                return StatusCode(500, "Internal server error");
            }
        }

        // PUT: api/users/{id}/change-password
        [HttpPut("{id}/change-password")]
        [Authorize]
        public async Task<ActionResult> ChangePassword(int id, [FromBody] ChangePasswordDto changePasswordDto)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                if (currentUserId != id)
                {
                    return Forbid();
                }

                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                if (!VerifyPassword(changePasswordDto.CurrentPassword, user.PasswordHash))
                {
                    return BadRequest(new { message = "Invalid current password" });
                }

                user.PasswordHash = HashPassword(changePasswordDto.NewPassword);
                user.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Password changed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password");
                return StatusCode(500, "Internal server error");
            }
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
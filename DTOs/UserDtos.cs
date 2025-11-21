using System.ComponentModel.DataAnnotations;

namespace EnvironmentalSurvey.DTOs
{
    public class UserDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? RollNumber { get; set; }
        public string? EmployeeNumber { get; set; }
        public string? Class { get; set; }
        public string? Specification { get; set; }
        public string? Section { get; set; }
        public DateTime? AdmissionDate { get; set; }
        public DateTime? JoiningDate { get; set; }
        public string RegistrationStatus { get; set; } = "pending";
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class RegisterDto
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^(admin|faculty|staff|student)$")]
        public string Role { get; set; } = string.Empty;

        public string? RollNumber { get; set; }
        public string? EmployeeNumber { get; set; }
        public string? Class { get; set; }
        public string? Specification { get; set; }
        public string? Section { get; set; }
        public DateTime? AdmissionDate { get; set; }
        public DateTime? JoiningDate { get; set; }
    }

    public class LoginDto
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public UserDto User { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
    }

    public class UpdateUserDto
    {
        [StringLength(100)]
        public string? FullName { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }

        public string? RollNumber { get; set; }
        public string? EmployeeNumber { get; set; }
        public string? Class { get; set; }
        public string? Specification { get; set; }
        public string? Section { get; set; }
        public DateTime? AdmissionDate { get; set; }
        public DateTime? JoiningDate { get; set; }
    }

    public class ChangePasswordDto
    {
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string NewPassword { get; set; } = string.Empty;
    }

    public class UserStatisticsDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int SurveysParticipated { get; set; }
        public decimal? AverageScore { get; set; }
        public DateTime? LastParticipation { get; set; }
        public int CompetitionsWon { get; set; }
        public int ParticipationsSubmitted { get; set; }
    }
}
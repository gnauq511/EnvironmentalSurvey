using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EnvironmentalSurvey.Models
{
    [Table("users")]
    public class User
    {
        [Key]
        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [StringLength(50)]
        [Column("username")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        [Column("password_hash")]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Column("full_name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        [Column("role")]
        public string Role { get; set; } = string.Empty;

        [StringLength(50)]
        [Column("roll_number")]
        public string? RollNumber { get; set; }

        [StringLength(50)]
        [Column("employee_number")]
        public string? EmployeeNumber { get; set; }

        [StringLength(50)]
        [Column("class")]
        public string? Class { get; set; }

        [StringLength(100)]
        [Column("specification")]
        public string? Specification { get; set; }

        [StringLength(10)]
        [Column("section")]
        public string? Section { get; set; }

        [Column("admission_date")]
        public DateTime? AdmissionDate { get; set; }

        [Column("joining_date")]
        public DateTime? JoiningDate { get; set; }

        [StringLength(20)]
        [Column("registration_status")]
        public string RegistrationStatus { get; set; } = "pending";

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
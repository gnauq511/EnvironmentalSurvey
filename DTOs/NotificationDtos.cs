using System.ComponentModel.DataAnnotations;

namespace EnvironmentalSurvey.DTOs
{
    public class NotificationDto
    {
        public int NotificationId { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Type { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateNotificationDto
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        [StringLength(20)]
        [RegularExpression("^(survey|competition|result|general)$")]
        public string? Type { get; set; }
    }

    public class BroadcastNotificationDto
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        [StringLength(20)]
        [RegularExpression("^(survey|competition|result|general)$")]
        public string? Type { get; set; }

        [StringLength(20)]
        [RegularExpression("^(admin|faculty|staff|student)$")]
        public string? TargetRole { get; set; }
    }
}

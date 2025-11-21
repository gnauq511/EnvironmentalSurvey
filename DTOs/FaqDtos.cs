using System.ComponentModel.DataAnnotations;

namespace EnvironmentalSurvey.DTOs
{
    public class FaqDto
    {
        public int FaqId { get; set; }
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public string? Category { get; set; }
        public int OrderNumber { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateFaqDto
    {
        [Required]
        [StringLength(500)]
        public string Question { get; set; } = string.Empty;

        [Required]
        public string Answer { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Category { get; set; }

        [Required]
        public int OrderNumber { get; set; }
    }

    public class UpdateFaqDto
    {
        [StringLength(500)]
        public string? Question { get; set; }

        public string? Answer { get; set; }

        [StringLength(50)]
        public string? Category { get; set; }

        public int? OrderNumber { get; set; }

        public bool? IsActive { get; set; }
    }
}
using System.ComponentModel.DataAnnotations;

namespace EnvironmentalSurvey.DTOs
{
    public class SurveyDto
    {
        public int SurveyId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string TargetAudience { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public int CreatedBy { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class SurveyDetailDto : SurveyDto
    {
        public List<QuestionDto> Questions { get; set; } = new();
    }

    public class CreateSurveyDto
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        [RegularExpression("^(student|faculty|staff|all)$")]
        public string TargetAudience { get; set; } = string.Empty;

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }
    }

    public class UpdateSurveyDto
    {
        [StringLength(200)]
        public string? Title { get; set; }

        public string? Description { get; set; }

        [RegularExpression("^(student|faculty|staff|all)$")]
        public string? TargetAudience { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool? IsActive { get; set; }
    }

    public class SurveyStatisticsDto
    {
        public int SurveyId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int TotalResponses { get; set; }
        public int TotalQuestions { get; set; }
        public decimal AverageScore { get; set; }
        public decimal CompletionRate { get; set; }
    }

}
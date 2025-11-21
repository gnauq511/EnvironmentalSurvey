using System.ComponentModel.DataAnnotations;

namespace EnvironmentalSurvey.DTOs
{
    public class CompetitionDto
    {
        public int CompetitionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? RelatedSurveyId { get; set; }
        public string? RelatedSurveyTitle { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? PrizeDescription { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class CreateCompetitionDto
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int? RelatedSurveyId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public string? PrizeDescription { get; set; }
    }

    public class UpdateCompetitionDto
    {
        [StringLength(200)]
        public string? Title { get; set; }

        public string? Description { get; set; }

        public int? RelatedSurveyId { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public string? PrizeDescription { get; set; }

        [RegularExpression("^(upcoming|ongoing|completed)$")]
        public string? Status { get; set; }
    }

    public class CompetitionWinnerDto
    {
        public int WinnerId { get; set; }
        public int CompetitionId { get; set; }
        public string CompetitionTitle { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int Rank { get; set; }
        public decimal Score { get; set; }
        public string? PrizeDetails { get; set; }
        public DateTime AnnouncedAt { get; set; }
    }

    public class CreateWinnerDto
    {
        [Required]
        public int CompetitionId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [Range(1, 3)]
        public int Rank { get; set; }

        [Required]
        public decimal Score { get; set; }

        public string? PrizeDetails { get; set; }
    }
}
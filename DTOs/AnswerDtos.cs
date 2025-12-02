using EnvironmentalSurvey.DTOs;
using System.ComponentModel.DataAnnotations;

namespace EnvironmentalSurvey.DTOs
{
    public class AnswerDto
    {
        public int AnswerId { get; set; }
        public int ResponseId { get; set; }
        public int QuestionId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string QuestionType { get; set; } = string.Empty;
        public int? OptionId { get; set; }
        public QuestionOptionDto? SelectedOption { get; set; }
        public string? TextAnswer { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateAnswerDto
    {
        [Required]
        public int ResponseId { get; set; }

        [Required]
        public int QuestionId { get; set; }

        public int? OptionId { get; set; }

        public string? TextAnswer { get; set; }
    }

    public class UpdateAnswerDto
    {
        public int? OptionId { get; set; }

        public string? TextAnswer { get; set; }
    }

    public class AnswerStatisticsDto
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string QuestionType { get; set; } = string.Empty;
        public int TotalAnswers { get; set; }
        public List<OptionStatisticsDto> OptionStatistics { get; set; } = new();
        public List<string> TextAnswers { get; set; } = new();
    }

    public class OptionStatisticsDto
    {
        public int OptionId { get; set; }
        public string OptionText { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Percentage { get; set; }
        public bool IsCorrect { get; set; }
    }

    public class QuestionAnswerSummaryDto
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string QuestionType { get; set; } = string.Empty;
        public int TotalResponses { get; set; }
        public int CorrectAnswers { get; set; }
        public int IncorrectAnswers { get; set; }
        public decimal CorrectPercentage { get; set; }
    }
}
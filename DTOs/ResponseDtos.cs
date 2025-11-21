using EnvironmentalSurvey.DTOs;
using System.ComponentModel.DataAnnotations;

namespace EnvironmentalSurvey.DTOs
{
    public class SurveyResponseDto
    {
        public int ResponseId { get; set; }
        public int SurveyId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
        public decimal? Score { get; set; }
    }

    public class SurveyResponseDetailDto : SurveyResponseDto
    {
        public List<AnswerDto> Answers { get; set; } = new();
    }

    public class SubmitResponseDto
    {
        [Required]
        public int SurveyId { get; set; }

        [Required]
        public List<SubmitAnswerDto> Answers { get; set; } = new();
    }

    public class SubmitAnswerDto
    {
        [Required]
        public int QuestionId { get; set; }

        public int? OptionId { get; set; }

        public string? TextAnswer { get; set; }
    }

    public class AnswerDto
    {
        public int AnswerId { get; set; }
        public int ResponseId { get; set; }
        public int QuestionId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public int? OptionId { get; set; }
        public QuestionOptionDto? SelectedOption { get; set; }
        public string? TextAnswer { get; set; }
    }
}
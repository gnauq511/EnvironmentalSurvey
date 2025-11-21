using System.ComponentModel.DataAnnotations;

namespace EnvironmentalSurvey.DTOs
{
    public class QuestionDto
    {
        public int QuestionId { get; set; }
        public int SurveyId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string QuestionType { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public int OrderNumber { get; set; }
        public List<QuestionOptionDto> Options { get; set; } = new();
    }

    public class CreateQuestionDto
    {
        [Required]
        public int SurveyId { get; set; }

        [Required]
        public string QuestionText { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^(single_choice|multiple_choice|text)$")]
        public string QuestionType { get; set; } = string.Empty;

        public bool IsRequired { get; set; } = true;

        [Required]
        public int OrderNumber { get; set; }

        public List<CreateQuestionOptionDto> Options { get; set; } = new();
    }

    public class UpdateQuestionDto
    {
        public string? QuestionText { get; set; }
        public string? QuestionType { get; set; }
        public bool? IsRequired { get; set; }
        public int? OrderNumber { get; set; }
    }

    public class QuestionOptionDto
    {
        public int OptionId { get; set; }
        public int QuestionId { get; set; }
        public string OptionText { get; set; } = string.Empty;
        public int OrderNumber { get; set; }
        public bool IsCorrect { get; set; }
    }

    public class CreateQuestionOptionDto
    {
        [Required]
        [StringLength(500)]
        public string OptionText { get; set; } = string.Empty;

        [Required]
        public int OrderNumber { get; set; }

        public bool IsCorrect { get; set; } = false;
    }
}
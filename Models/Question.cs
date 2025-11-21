using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EnvironmentalSurvey.Models;

[Table("questions")]
public class Question
{
    [Key]
    [Column("question_id")]
    public int QuestionId { get; set; }

    [Column("survey_id")]
    public int SurveyId { get; set; }

    [Required]
    [Column("question_text")]
    public string QuestionText { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    [Column("question_type")]
    public string QuestionType { get; set; } = string.Empty;

    [Column("is_required")]
    public bool IsRequired { get; set; } = true;

    [Column("order_number")]
    public int OrderNumber { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [ForeignKey("SurveyId")]
    public virtual Survey Survey { get; set; } = null!;
    public virtual ICollection<QuestionOption> Options { get; set; } = new List<QuestionOption>();
    public virtual ICollection<Answer> Answers { get; set; } = new List<Answer>();
}

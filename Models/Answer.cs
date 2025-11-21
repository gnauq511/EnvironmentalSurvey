using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EnvironmentalSurvey.Models;

[Table("answers")]
public class Answer
{
    [Key]
    [Column("answer_id")]
    public int AnswerId { get; set; }

    [Column("response_id")]
    public int ResponseId { get; set; }

    [Column("question_id")]
    public int QuestionId { get; set; }

    [Column("option_id")]
    public int? OptionId { get; set; }

    [Column("text_answer")]
    public string? TextAnswer { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [ForeignKey("ResponseId")]
    public virtual SurveyResponse Response { get; set; } = null!;

    [ForeignKey("QuestionId")]
    public virtual Question Question { get; set; } = null!;

    [ForeignKey("OptionId")]
    public virtual QuestionOption? Option { get; set; }
}

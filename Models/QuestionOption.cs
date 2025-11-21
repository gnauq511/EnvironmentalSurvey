using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EnvironmentalSurvey.Models;

[Table("question_options")]
public class QuestionOption
{
    [Key]
    [Column("option_id")]
    public int OptionId { get; set; }

    [Column("question_id")]
    public int QuestionId { get; set; }

    [Required]
    [StringLength(500)]
    [Column("option_text")]
    public string OptionText { get; set; } = string.Empty;

    [Column("order_number")]
    public int OrderNumber { get; set; }

    [Column("is_correct")]
    public bool IsCorrect { get; set; } = false;

    [ForeignKey("QuestionId")]
    public virtual Question Question { get; set; } = null!;
    public virtual ICollection<Answer> Answers { get; set; } = new List<Answer>();
}
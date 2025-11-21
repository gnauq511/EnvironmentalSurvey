using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EnvironmentalSurvey.Models;

[Table("survey_responses")]
public class SurveyResponse
{
    [Key]
    [Column("response_id")]
    public int ResponseId { get; set; }

    [Column("survey_id")]
    public int SurveyId { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("submitted_at")]
    public DateTime SubmittedAt { get; set; } = DateTime.Now;

    [Column("score")]
    public decimal? Score { get; set; }

    [ForeignKey("SurveyId")]
    public virtual Survey Survey { get; set; } = null!;

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
    public virtual ICollection<Answer> Answers { get; set; } = new List<Answer>();
}
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EnvironmentalSurvey.Models;

[Table("competitions")]
public class Competition
{
    [Key]
    [Column("competition_id")]
    public int CompetitionId { get; set; }

    [Required]
    [StringLength(200)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("related_survey_id")]
    public int? RelatedSurveyId { get; set; }

    [Column("start_date")]
    public DateTime StartDate { get; set; }

    [Column("end_date")]
    public DateTime EndDate { get; set; }

    [Column("prize_description")]
    public string? PrizeDescription { get; set; }

    [StringLength(20)]
    [Column("status")]
    public string Status { get; set; } = "upcoming";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    [ForeignKey("RelatedSurveyId")]
    public virtual Survey? RelatedSurvey { get; set; }
    public virtual ICollection<CompetitionWinner> Winners { get; set; } = new List<CompetitionWinner>();
}
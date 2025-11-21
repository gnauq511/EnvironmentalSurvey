using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EnvironmentalSurvey.Models;

[Table("surveys")]
public class Survey
{
    [Key]
    [Column("survey_id")]
    public int SurveyId { get; set; }

    [Required]
    [StringLength(200)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Required]
    [StringLength(20)]
    [Column("target_audience")]
    public string TargetAudience { get; set; } = string.Empty;

    [Column("start_date")]
    public DateTime StartDate { get; set; }

    [Column("end_date")]
    public DateTime EndDate { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_by")]
    public int CreatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

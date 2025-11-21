using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EnvironmentalSurvey.Models;

[Table("effective_participation")]
public class EffectiveParticipation
{
    [Key]
    [Column("participation_id")]
    public int ParticipationId { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Required]
    [StringLength(200)]
    [Column("seminar_title")]
    public string SeminarTitle { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    [Column("location")]
    public string Location { get; set; } = string.Empty;

    [Column("date_conducted")]
    public DateTime DateConducted { get; set; }

    [Column("number_of_participants")]
    public int? NumberOfParticipants { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("approved_by")]
    public int? ApprovedBy { get; set; }

    [StringLength(20)]
    [Column("approval_status")]
    public string ApprovalStatus { get; set; } = "pending";

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("ApprovedBy")]
    public virtual User? ApprovedByUser { get; set; }
}
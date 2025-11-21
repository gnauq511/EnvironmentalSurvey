using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EnvironmentalSurvey.Models;

[Table("support_info")]
public class SupportInfo
{
    [Key]
    [Column("support_id")]
    public int SupportId { get; set; }

    [StringLength(20)]
    [Column("contact_type")]
    public string? ContactType { get; set; }

    [StringLength(200)]
    [Column("contact_value")]
    public string? ContactValue { get; set; }

    [StringLength(500)]
    [Column("description")]
    public string? Description { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
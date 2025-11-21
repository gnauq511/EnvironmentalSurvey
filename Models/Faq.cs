using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EnvironmentalSurvey.Models;

[Table("faqs")]
public class Faq
{
    [Key]
    [Column("faq_id")]
    public int FaqId { get; set; }

    [Required]
    [StringLength(500)]
    [Column("question")]
    public string Question { get; set; } = string.Empty;

    [Required]
    [Column("answer")]
    public string Answer { get; set; } = string.Empty;

    [StringLength(50)]
    [Column("category")]
    public string? Category { get; set; }

    [Column("order_number")]
    public int OrderNumber { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_by")]
    public int CreatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    [ForeignKey("CreatedBy")]
    public virtual User CreatedByUser { get; set; } = null!;
}
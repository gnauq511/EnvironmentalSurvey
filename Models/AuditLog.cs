using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EnvironmentalSurvey.Models;

[Table("audit_logs")]
public class AuditLog
{
    [Key]
    [Column("log_id")]
    public int LogId { get; set; }

    [Column("user_id")]
    public int? UserId { get; set; }

    [Required]
    [StringLength(100)]
    [Column("action")]
    public string Action { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    [Column("table_name")]
    public string TableName { get; set; } = string.Empty;

    [Column("record_id")]
    public int? RecordId { get; set; }

    [Column("old_value")]
    public string? OldValue { get; set; }

    [Column("new_value")]
    public string? NewValue { get; set; }

    [StringLength(45)]
    [Column("ip_address")]
    public string? IpAddress { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
}

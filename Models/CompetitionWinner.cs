using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EnvironmentalSurvey.Models;

[Table("competition_winners")]
public class CompetitionWinner
{
    [Key]
    [Column("winner_id")]
    public int WinnerId { get; set; }

    [Column("competition_id")]
    public int CompetitionId { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("rank")]
    public int Rank { get; set; }

    [Column("score")]
    public decimal Score { get; set; }

    [StringLength(200)]
    [Column("prize_details")]
    public string? PrizeDetails { get; set; }

    [Column("announced_at")]
    public DateTime AnnouncedAt { get; set; } = DateTime.Now;

    [ForeignKey("CompetitionId")]
    public virtual Competition Competition { get; set; } = null!;

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
}

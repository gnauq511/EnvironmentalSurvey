using System;
using System.Collections.Generic;

namespace EnvironmentalSurvey.Models;

public partial class CompetitionLeaderboard
{
    public int CompetitionId { get; set; }

    public string CompetitionTitle { get; set; } = null!;

    public string? Status { get; set; }

    public int? Rank { get; set; }

    public string? WinnerName { get; set; }

    public string? WinnerRole { get; set; }

    public decimal? Score { get; set; }

    public string? PrizeDetails { get; set; }

    public DateTime? AnnouncedAt { get; set; }
}

using System;
using System.Collections.Generic;

namespace EnvironmentalSurvey.Models;

public partial class UserSurveyStat
{
    public int UserId { get; set; }

    public string FullName { get; set; } = null!;

    public string Role { get; set; } = null!;

    public int? SurveysParticipated { get; set; }

    public decimal? AverageScore { get; set; }

    public DateTime? LastParticipation { get; set; }
}

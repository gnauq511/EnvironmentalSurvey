using System;
using System.Collections.Generic;

namespace EnvironmentalSurvey.Models;

public partial class SurveyStatistic
{
    public int SurveyId { get; set; }

    public string Title { get; set; } = null!;

    public string TargetAudience { get; set; } = null!;

    public int? TotalResponses { get; set; }

    public int? TotalQuestions { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public bool? IsActive { get; set; }
}

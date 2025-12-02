namespace EnvironmentalSurvey.DTOs
{
    // Overview - Tổng quan hệ thống
    public class DashboardOverviewDto
    {
        public int TotalUsers { get; set; }
        public int PendingSurveys { get; set; }
        public int OngoingSurveys { get; set; }
        public int OngoingCompetitions { get; set; }
    }

    // Recent Activities - Hoạt động gần đây
    public class RecentActivityDto
    {
        public string Type { get; set; } = string.Empty; // user_registration, survey_created, etc.
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int? RelatedId { get; set; }
        public int? UserId { get; set; }
        public string? UserName { get; set; }
        public DateTime Timestamp { get; set; }
        public string TimeAgo
        {
            get
            {
                var span = DateTime.Now - Timestamp;
                if (span.TotalMinutes < 1) return "Just now";
                if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} minutes ago";
                if (span.TotalHours < 24) return $"{(int)span.TotalHours} hours ago";
                if (span.TotalDays < 7) return $"{(int)span.TotalDays} days ago";
                if (span.TotalDays < 30) return $"{(int)(span.TotalDays / 7)} weeks ago";
                return $"{(int)(span.TotalDays / 30)} months ago";
            }
        }
    }

    // Statistics - Thống kê chi tiết
    public class DashboardStatisticsDto
    {
        public UserStatisticsOverviewDto UserStatistics { get; set; } = new();
        public SurveyStatisticsOverviewDto SurveyStatistics { get; set; } = new();
        public CompetitionStatisticsOverviewDto CompetitionStatistics { get; set; } = new();
        public ParticipationStatisticsOverviewDto ParticipationStatistics { get; set; } = new();
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
    }

    public class UserStatisticsOverviewDto
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int PendingUsers { get; set; }
        public int NewUsers { get; set; }
    }

    public class SurveyStatisticsOverviewDto
    {
        public int TotalSurveys { get; set; }
        public int ActiveSurveys { get; set; }
        public int CompletedSurveys { get; set; }
        public int TotalResponses { get; set; }
        public int ResponsesInPeriod { get; set; }
    }

    public class CompetitionStatisticsOverviewDto
    {
        public int TotalCompetitions { get; set; }
        public int OngoingCompetitions { get; set; }
        public int CompletedCompetitions { get; set; }
    }

    public class ParticipationStatisticsOverviewDto
    {
        public int TotalParticipations { get; set; }
        public int PendingParticipations { get; set; }
        public int ApprovedParticipations { get; set; }
    }

    // Growth Data - Dữ liệu tăng trưởng theo thời gian
    public class GrowthDataDto
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
        public string DateLabel => Date.ToString("dd/MM");
    }

    // Top Surveys - Khảo sát phổ biến
    public class TopSurveyDto
    {
        public int SurveyId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int ResponseCount { get; set; }
        public string TargetAudience { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    // User Distribution - Phân bố người dùng theo role
    public class UserDistributionDto
    {
        public string Role { get; set; } = string.Empty;
        public int Count { get; set; }
        public string RoleLabel
        {
            get
            {
                return Role switch
                {
                    "admin" => "Admin",
                    "faculty" => "Faculty",
                    "staff" => "Staff",
                    "student" => "Student",
                    _ => Role
                };
            }
        }
    }

    // Pending Approvals - Các yêu cầu chờ duyệt
    public class PendingApprovalsDto
    {
        public int PendingUsers { get; set; }
        public int PendingParticipations { get; set; }
        public int TotalPending { get; set; }
    }

    // System Health - Sức khỏe hệ thống
    public class SystemHealthDto
    {
        public decimal UserActivityRate { get; set; }
        public decimal SurveyActivityRate { get; set; }
        public int ResponsesLast24Hours { get; set; }
        public int ResponsesLast7Days { get; set; }
        public int TotalAuditLogs { get; set; }
        public int RecentActivityCount { get; set; }
        public string Status { get; set; } = string.Empty; // healthy, warning, critical
        public DateTime LastUpdated { get; set; }
    }

    // Summary cho trang chính
    public class DashboardSummaryDto
    {
        public DashboardOverviewDto Overview { get; set; } = new();
        public List<RecentActivityDto> RecentActivities { get; set; } = new();
        public PendingApprovalsDto PendingApprovals { get; set; } = new();
        public List<TopSurveyDto> TopSurveys { get; set; } = new();
        public List<UserDistributionDto> UserDistribution { get; set; } = new();
    }
}
using EnvironmentalSurvey.Data;
using EnvironmentalSurvey.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace EnvironmentalSurvey.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "admin")]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(AppDbContext context, ILogger<DashboardController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/dashboard/overview
        [HttpGet("overview")]
        public async Task<ActionResult<DashboardOverviewDto>> GetOverview()
        {
            try
            {
                var totalUsers = await _context.Users
                    .Where(u => u.RegistrationStatus == "approved")
                    .CountAsync();

                var pendingSurveys = await _context.Surveys
                    .Where(s => s.IsActive == true && s.EndDate >= DateTime.Now)
                    .CountAsync();

                var ongoingSurveys = await _context.Surveys
                    .Where(s => s.IsActive == true &&
                               s.StartDate <= DateTime.Now &&
                               s.EndDate >= DateTime.Now)
                    .CountAsync();

                var ongoingCompetitions = await _context.Competitions
                    .Where(c => c.Status == "ongoing")
                    .CountAsync();

                var overview = new DashboardOverviewDto
                {
                    TotalUsers = totalUsers,
                    PendingSurveys = pendingSurveys,
                    OngoingSurveys = ongoingSurveys,
                    OngoingCompetitions = ongoingCompetitions
                };

                return Ok(overview);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard overview");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/dashboard/recent-activities
        [HttpGet("recent-activities")]
        public async Task<ActionResult<IEnumerable<RecentActivityDto>>> GetRecentActivities([FromQuery] int limit = 10)
        {
            try
            {
                var activities = new List<RecentActivityDto>();

                // Người dùng mới đăng ký
                var newUsers = await _context.Users
                    .Where(u => u.RegistrationStatus == "pending")
                    .OrderByDescending(u => u.CreatedAt)
                    .Take(limit)
                    .Select(u => new RecentActivityDto
                    {
                        Type = "user_registration",
                        Title = "New user registration",
                        Description = u.FullName,
                        UserId = u.UserId,
                        UserName = u.FullName,
                        Timestamp = u.CreatedAt
                    })
                    .ToListAsync();

                // Khảo sát mới được tạo
                var newSurveys = await _context.Surveys
                    .OrderByDescending(s => s.CreatedAt)
                    .Take(limit)
                    .ToListAsync();

                foreach (var survey in newSurveys)
                {
                    var creator = await _context.Users.FindAsync(survey.CreatedBy);
                    activities.Add(new RecentActivityDto
                    {
                        Type = "survey_created",
                        Title = "Survey created",
                        Description = survey.Title,
                        RelatedId = survey.SurveyId,
                        UserId = survey.CreatedBy,
                        UserName = creator?.FullName ?? "Unknown",
                        Timestamp = survey.CreatedAt
                    });
                }

                // Cuộc thi được cập nhật
                var updatedCompetitions = await _context.Competitions
                    .OrderByDescending(c => c.UpdatedAt)
                    .Take(limit)
                    .Select(c => new RecentActivityDto
                    {
                        Type = "competition_updated",
                        Title = "Competition updated",
                        Description = c.Title,
                        RelatedId = c.CompetitionId,
                        Timestamp = c.UpdatedAt
                    })
                    .ToListAsync();

                // Người dùng được phê duyệt
                var approvedUsers = await _context.AuditLogs
                    .Where(a => a.Action == "UPDATE" &&
                               a.TableName == "users" &&
                               a.NewValue != null &&
                               a.NewValue.Contains("approved"))
                    .OrderByDescending(a => a.CreatedAt)
                    .Take(limit)
                    .ToListAsync();

                foreach (var log in approvedUsers)
                {
                    if (log.RecordId.HasValue)
                    {
                        var user = await _context.Users.FindAsync(log.RecordId.Value);
                        if (user != null)
                        {
                            activities.Add(new RecentActivityDto
                            {
                                Type = "user_approved",
                                Title = "Approved users",
                                Description = user.FullName,
                                UserId = user.UserId,
                                UserName = user.FullName,
                                Timestamp = log.CreatedAt
                            });
                        }
                    }
                }

                // Sắp xếp theo thời gian và lấy top records
                var sortedActivities = activities
                    .OrderByDescending(a => a.Timestamp)
                    .Take(limit)
                    .ToList();

                return Ok(sortedActivities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent activities");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/dashboard/statistics
        [HttpGet("statistics")]
        public async Task<ActionResult<DashboardStatisticsDto>> GetStatistics(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var from = fromDate ?? DateTime.Now.AddMonths(-1);
                var to = toDate ?? DateTime.Now;

                // User statistics
                var totalUsers = await _context.Users.CountAsync();
                var activeUsers = await _context.Users.Where(u => u.IsActive == true).CountAsync();
                var pendingUsers = await _context.Users.Where(u => u.RegistrationStatus == "pending").CountAsync();
                var newUsersInPeriod = await _context.Users
                    .Where(u => u.CreatedAt >= from && u.CreatedAt <= to)
                    .CountAsync();

                // Survey statistics
                var totalSurveys = await _context.Surveys.CountAsync();
                var activeSurveys = await _context.Surveys
                    .Where(s => s.IsActive == true && s.EndDate >= DateTime.Now)
                    .CountAsync();
                var completedSurveys = await _context.Surveys
                    .Where(s => s.EndDate < DateTime.Now)
                    .CountAsync();

                // Response statistics
                var totalResponses = await _context.SurveyResponses.CountAsync();
                var responsesInPeriod = await _context.SurveyResponses
                    .Where(r => r.SubmittedAt >= from && r.SubmittedAt <= to)
                    .CountAsync();

                // Competition statistics
                var totalCompetitions = await _context.Competitions.CountAsync();
                var ongoingCompetitions = await _context.Competitions
                    .Where(c => c.Status == "ongoing")
                    .CountAsync();
                var completedCompetitions = await _context.Competitions
                    .Where(c => c.Status == "completed")
                    .CountAsync();

                // Participation statistics
                var totalParticipations = await _context.EffectiveParticipations.CountAsync();
                var pendingParticipations = await _context.EffectiveParticipations
                    .Where(p => p.ApprovalStatus == "pending")
                    .CountAsync();
                var approvedParticipations = await _context.EffectiveParticipations
                    .Where(p => p.ApprovalStatus == "approved")
                    .CountAsync();

                var statistics = new DashboardStatisticsDto
                {
                    UserStatistics = new UserStatisticsOverviewDto
                    {
                        TotalUsers = totalUsers,
                        ActiveUsers = activeUsers,
                        PendingUsers = pendingUsers,
                        NewUsers = newUsersInPeriod
                    },
                    SurveyStatistics = new SurveyStatisticsOverviewDto
                    {
                        TotalSurveys = totalSurveys,
                        ActiveSurveys = activeSurveys,
                        CompletedSurveys = completedSurveys,
                        TotalResponses = totalResponses,
                        ResponsesInPeriod = responsesInPeriod
                    },
                    CompetitionStatistics = new CompetitionStatisticsOverviewDto
                    {
                        TotalCompetitions = totalCompetitions,
                        OngoingCompetitions = ongoingCompetitions,
                        CompletedCompetitions = completedCompetitions
                    },
                    ParticipationStatistics = new ParticipationStatisticsOverviewDto
                    {
                        TotalParticipations = totalParticipations,
                        PendingParticipations = pendingParticipations,
                        ApprovedParticipations = approvedParticipations
                    },
                    PeriodStart = from,
                    PeriodEnd = to
                };

                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting statistics");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/dashboard/user-growth
        [HttpGet("user-growth")]
        public async Task<ActionResult<IEnumerable<GrowthDataDto>>> GetUserGrowth([FromQuery] int days = 30)
        {
            try
            {
                var startDate = DateTime.Now.AddDays(-days).Date;
                var growthData = new List<GrowthDataDto>();

                for (int i = 0; i <= days; i++)
                {
                    var date = startDate.AddDays(i);
                    var count = await _context.Users
                        .Where(u => u.CreatedAt.Date == date)
                        .CountAsync();

                    growthData.Add(new GrowthDataDto
                    {
                        Date = date,
                        Count = count
                    });
                }

                return Ok(growthData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user growth");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/dashboard/survey-responses-trend
        [HttpGet("survey-responses-trend")]
        public async Task<ActionResult<IEnumerable<GrowthDataDto>>> GetSurveyResponsesTrend([FromQuery] int days = 30)
        {
            try
            {
                var startDate = DateTime.Now.AddDays(-days).Date;
                var trendData = new List<GrowthDataDto>();

                for (int i = 0; i <= days; i++)
                {
                    var date = startDate.AddDays(i);
                    var count = await _context.SurveyResponses
                        .Where(r => r.SubmittedAt.Date == date)
                        .CountAsync();

                    trendData.Add(new GrowthDataDto
                    {
                        Date = date,
                        Count = count
                    });
                }

                return Ok(trendData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting survey responses trend");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/dashboard/top-surveys
        [HttpGet("top-surveys")]
        public async Task<ActionResult<IEnumerable<TopSurveyDto>>> GetTopSurveys([FromQuery] int limit = 5)
        {
            try
            {
                var surveys = await _context.Surveys
                    .OrderByDescending(s => s.CreatedAt)
                    .Take(100)
                    .ToListAsync();

                var topSurveys = new List<TopSurveyDto>();

                foreach (var survey in surveys)
                {
                    var responseCount = await _context.SurveyResponses
                        .Where(r => r.SurveyId == survey.SurveyId)
                        .CountAsync();

                    topSurveys.Add(new TopSurveyDto
                    {
                        SurveyId = survey.SurveyId,
                        Title = survey.Title,
                        ResponseCount = responseCount,
                        TargetAudience = survey.TargetAudience,
                        IsActive = survey.IsActive,
                        StartDate = survey.StartDate,
                        EndDate = survey.EndDate
                    });
                }

                var result = topSurveys
                    .OrderByDescending(s => s.ResponseCount)
                    .Take(limit)
                    .ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top surveys");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/dashboard/user-distribution
        [HttpGet("user-distribution")]
        public async Task<ActionResult<IEnumerable<UserDistributionDto>>> GetUserDistribution()
        {
            try
            {
                var distribution = await _context.Users
                    .Where(u => u.RegistrationStatus == "approved")
                    .GroupBy(u => u.Role)
                    .Select(g => new UserDistributionDto
                    {
                        Role = g.Key,
                        Count = g.Count()
                    })
                    .ToListAsync();

                return Ok(distribution);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user distribution");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/dashboard/pending-approvals
        [HttpGet("pending-approvals")]
        public async Task<ActionResult<PendingApprovalsDto>> GetPendingApprovals()
        {
            try
            {
                var pendingUsers = await _context.Users
                    .Where(u => u.RegistrationStatus == "pending")
                    .CountAsync();

                var pendingParticipations = await _context.EffectiveParticipations
                    .Where(p => p.ApprovalStatus == "pending")
                    .CountAsync();

                var pendingApprovals = new PendingApprovalsDto
                {
                    PendingUsers = pendingUsers,
                    PendingParticipations = pendingParticipations,
                    TotalPending = pendingUsers + pendingParticipations
                };

                return Ok(pendingApprovals);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending approvals");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/dashboard/system-health
        [HttpGet("system-health")]
        public async Task<ActionResult<SystemHealthDto>> GetSystemHealth()
        {
            try
            {
                var totalUsers = await _context.Users.CountAsync();
                var activeUsers = await _context.Users.Where(u => u.IsActive == true).CountAsync();
                var totalSurveys = await _context.Surveys.CountAsync();
                var activeSurveys = await _context.Surveys.Where(s => s.IsActive == true).CountAsync();

                var last24HoursResponses = await _context.SurveyResponses
                    .Where(r => r.SubmittedAt >= DateTime.Now.AddHours(-24))
                    .CountAsync();

                var last7DaysResponses = await _context.SurveyResponses
                    .Where(r => r.SubmittedAt >= DateTime.Now.AddDays(-7))
                    .CountAsync();

                var auditLogCount = await _context.AuditLogs.CountAsync();
                var recentAuditLogs = await _context.AuditLogs
                    .Where(a => a.CreatedAt >= DateTime.Now.AddHours(-1))
                    .CountAsync();

                var health = new SystemHealthDto
                {
                    UserActivityRate = totalUsers > 0 ? (decimal)activeUsers / totalUsers * 100 : 0,
                    SurveyActivityRate = totalSurveys > 0 ? (decimal)activeSurveys / totalSurveys * 100 : 0,
                    ResponsesLast24Hours = last24HoursResponses,
                    ResponsesLast7Days = last7DaysResponses,
                    TotalAuditLogs = auditLogCount,
                    RecentActivityCount = recentAuditLogs,
                    Status = "healthy",
                    LastUpdated = DateTime.Now
                };

                return Ok(health);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system health");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
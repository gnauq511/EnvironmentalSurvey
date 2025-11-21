using EnvironmentalSurvey.Data;
using EnvironmentalSurvey.DTOs;
using EnvironmentalSurvey.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EnvironmentalSurvey.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SurveysController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SurveysController> _logger;

        public SurveysController(AppDbContext context, ILogger<SurveysController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/surveys
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<SurveyDto>>> GetAllSurveys(
            [FromQuery] string? targetAudience = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var query = _context.Surveys.AsQueryable();

                if (!string.IsNullOrEmpty(targetAudience))
                {
                    query = query.Where(s => s.TargetAudience == targetAudience);
                }

                if (isActive.HasValue)
                {
                    query = query.Where(s => s.IsActive == isActive.Value);
                }

                var surveys = await query
                    .OrderByDescending(s => s.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var surveyDtos = new List<SurveyDto>();
                foreach (var survey in surveys)
                {
                    var creator = await _context.Users.FindAsync(survey.CreatedBy);
                    surveyDtos.Add(new SurveyDto
                    {
                        SurveyId = survey.SurveyId,
                        Title = survey.Title,
                        Description = survey.Description,
                        TargetAudience = survey.TargetAudience,
                        StartDate = survey.StartDate,
                        EndDate = survey.EndDate,
                        IsActive = (bool)survey.IsActive,
                        CreatedBy = survey.CreatedBy,
                        CreatedByName = creator?.FullName ?? "Unknown",
                        CreatedAt = (DateTime)survey.CreatedAt
                    });
                }

                return Ok(surveyDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting surveys");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/surveys/{id}
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<SurveyDetailDto>> GetSurveyById(int id)
        {
            try
            {
                var survey = await _context.Surveys.FindAsync(id);
                if (survey == null)
                {
                    return NotFound(new { message = "Survey not found" });
                }

                var creator = await _context.Users.FindAsync(survey.CreatedBy);

                var questions = await _context.Questions
                    .Where(q => q.SurveyId == id)
                    .OrderBy(q => q.OrderNumber)
                    .ToListAsync();

                var questionDtos = new List<QuestionDto>();
                foreach (var question in questions)
                {
                    var options = await _context.QuestionOptions
                        .Where(o => o.QuestionId == question.QuestionId)
                        .OrderBy(o => o.OrderNumber)
                        .Select(o => new QuestionOptionDto
                        {
                            OptionId = o.OptionId,
                            QuestionId = o.QuestionId,
                            OptionText = o.OptionText,
                            OrderNumber = o.OrderNumber,
                            IsCorrect = (bool)o.IsCorrect
                        })
                        .ToListAsync();

                    questionDtos.Add(new QuestionDto
                    {
                        QuestionId = question.QuestionId,
                        SurveyId = question.SurveyId,
                        QuestionText = question.QuestionText,
                        QuestionType = question.QuestionType,
                        IsRequired = (bool)question.IsRequired,
                        OrderNumber = question.OrderNumber,
                        Options = options
                    });
                }

                var surveyDto = new SurveyDetailDto
                {
                    SurveyId = survey.SurveyId,
                    Title = survey.Title,
                    Description = survey.Description,
                    TargetAudience = survey.TargetAudience,
                    StartDate = survey.StartDate,
                    EndDate = survey.EndDate,
                    IsActive = (bool)survey.IsActive,
                    CreatedBy = survey.CreatedBy,
                    CreatedByName = creator?.FullName ?? "Unknown",
                    CreatedAt = (DateTime)survey.CreatedAt,
                    Questions = questionDtos
                };

                return Ok(surveyDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting survey by id");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/surveys
        [HttpPost]
        [Authorize(Roles = "admin,faculty")]
        public async Task<ActionResult<SurveyDto>> CreateSurvey([FromBody] CreateSurveyDto createDto)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var survey = new Survey
                {
                    Title = createDto.Title,
                    Description = createDto.Description,
                    TargetAudience = createDto.TargetAudience,
                    StartDate = createDto.StartDate,
                    EndDate = createDto.EndDate,
                    IsActive = true,
                    CreatedBy = currentUserId,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.Surveys.Add(survey);
                await _context.SaveChangesAsync();

                var creator = await _context.Users.FindAsync(currentUserId);

                var surveyDto = new SurveyDto
                {
                    SurveyId = survey.SurveyId,
                    Title = survey.Title,
                    Description = survey.Description,
                    TargetAudience = survey.TargetAudience,
                    StartDate = survey.StartDate,
                    EndDate = survey.EndDate,
                    IsActive = (bool)survey.IsActive,
                    CreatedBy = survey.CreatedBy,
                    CreatedByName = creator?.FullName ?? "Unknown",
                    CreatedAt = (DateTime)survey.CreatedAt
                };

                return CreatedAtAction(nameof(GetSurveyById), new { id = survey.SurveyId }, surveyDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating survey");
                return StatusCode(500, "Internal server error");
            }
        }

        // PUT: api/surveys/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "admin,faculty")]
        public async Task<ActionResult<SurveyDto>> UpdateSurvey(int id, [FromBody] UpdateSurveyDto updateDto)
        {
            try
            {
                var survey = await _context.Surveys.FindAsync(id);
                if (survey == null)
                {
                    return NotFound(new { message = "Survey not found" });
                }

                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (userRole != "admin" && survey.CreatedBy != currentUserId)
                {
                    return Forbid();
                }

                if (!string.IsNullOrEmpty(updateDto.Title))
                    survey.Title = updateDto.Title;

                if (updateDto.Description != null)
                    survey.Description = updateDto.Description;

                if (!string.IsNullOrEmpty(updateDto.TargetAudience))
                    survey.TargetAudience = updateDto.TargetAudience;

                if (updateDto.StartDate.HasValue)
                    survey.StartDate = updateDto.StartDate.Value;

                if (updateDto.EndDate.HasValue)
                    survey.EndDate = updateDto.EndDate.Value;

                if (updateDto.IsActive.HasValue)
                    survey.IsActive = updateDto.IsActive.Value;

                survey.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                var creator = await _context.Users.FindAsync(survey.CreatedBy);

                var surveyDto = new SurveyDto
                {
                    SurveyId = survey.SurveyId,
                    Title = survey.Title,
                    Description = survey.Description,
                    TargetAudience = survey.TargetAudience,
                    StartDate = survey.StartDate,
                    EndDate = survey.EndDate,
                    IsActive = (bool)survey.IsActive,
                    CreatedBy = survey.CreatedBy,
                    CreatedByName = creator?.FullName ?? "Unknown",
                    CreatedAt = (DateTime)survey.CreatedAt
                };

                return Ok(surveyDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating survey");
                return StatusCode(500, "Internal server error");
            }
        }

        // DELETE: api/surveys/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult> DeleteSurvey(int id)
        {
            try
            {
                var survey = await _context.Surveys.FindAsync(id);
                if (survey == null)
                {
                    return NotFound(new { message = "Survey not found" });
                }

                _context.Surveys.Remove(survey);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting survey");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/surveys/{id}/statistics
        [HttpGet("{id}/statistics")]
        [Authorize(Roles = "admin,faculty")]
        public async Task<ActionResult<SurveyStatisticsDto>> GetSurveyStatistics(int id)
        {
            try
            {
                var survey = await _context.Surveys.FindAsync(id);
                if (survey == null)
                {
                    return NotFound(new { message = "Survey not found" });
                }

                var responsesCount = await _context.SurveyResponses
                    .CountAsync(r => r.SurveyId == id);

                var questionsCount = await _context.Questions
                    .CountAsync(q => q.SurveyId == id);

                var averageScore = await _context.SurveyResponses
                    .Where(r => r.SurveyId == id && r.Score.HasValue)
                    .AverageAsync(r => (decimal?)r.Score) ?? 0;

                var completionRate = responsesCount > 0
                    ? (decimal)await _context.SurveyResponses.CountAsync(r => r.SurveyId == id && r.Score.HasValue) / responsesCount * 100
                    : 0;

                var statistics = new SurveyStatisticsDto
                {
                    SurveyId = survey.SurveyId,
                    Title = survey.Title,
                    TotalResponses = responsesCount,
                    TotalQuestions = questionsCount,
                    AverageScore = averageScore,
                    CompletionRate = completionRate
                };

                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting survey statistics");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/surveys/my-surveys
        [HttpGet("my-surveys")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<SurveyDto>>> GetMySurveys()
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var surveys = await _context.Surveys
                    .Where(s => s.CreatedBy == currentUserId)
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync();

                var creator = await _context.Users.FindAsync(currentUserId);

                var surveyDtos = surveys.Select(s => new SurveyDto
                {
                    SurveyId = s.SurveyId,
                    Title = s.Title,
                    Description = s.Description,
                    TargetAudience = s.TargetAudience,
                    StartDate = s.StartDate,
                    EndDate = s.EndDate,
                    IsActive = (bool)s.IsActive,
                    CreatedBy = s.CreatedBy,
                    CreatedByName = creator?.FullName ?? "Unknown",
                    CreatedAt = (DateTime)s.CreatedAt
                }).ToList();

                return Ok(surveyDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting my surveys");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/surveys/available
        [HttpGet("available")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<SurveyDto>>> GetAvailableSurveys()
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "";

                var now = DateTime.Now;
                var surveys = await _context.Surveys
                    .Where(s => s.IsActive == true && s.StartDate <= now && s.EndDate >= now)
                    .Where(s => s.TargetAudience == "all" || s.TargetAudience == userRole)
                    .OrderBy(s => s.EndDate)
                    .ToListAsync();

                var surveyDtos = new List<SurveyDto>();
                foreach (var survey in surveys)
                {
                    var creator = await _context.Users.FindAsync(survey.CreatedBy);
                    surveyDtos.Add(new SurveyDto
                    {
                        SurveyId = survey.SurveyId,
                        Title = survey.Title,
                        Description = survey.Description,
                        TargetAudience = survey.TargetAudience,
                        StartDate = survey.StartDate,
                        EndDate = survey.EndDate,
                        IsActive = (bool)survey.IsActive,
                        CreatedBy = survey.CreatedBy,
                        CreatedByName = creator?.FullName ?? "Unknown",
                        CreatedAt = (DateTime)survey.CreatedAt
                    });
                }

                return Ok(surveyDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available surveys");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
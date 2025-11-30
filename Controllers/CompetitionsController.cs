using EnvironmentalSurvey.Data;
using EnvironmentalSurvey.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EnvironmentalSurvey.DTOs;
namespace EnvironmentalSurvey.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CompetitionsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CompetitionsController> _logger;

        public CompetitionsController(AppDbContext context, ILogger<CompetitionsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/competitions
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<CompetitionDto>>> GetAllCompetitions(
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var query = _context.Competitions.AsQueryable();

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(c => c.Status == status);
                }

                var competitions = await query
                    .OrderByDescending(c => c.StartDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var competitionDtos = new List<CompetitionDto>();
                foreach (var competition in competitions)
                {
                    string? surveyTitle = null;
                    if (competition.RelatedSurveyId.HasValue)
                    {
                        var survey = await _context.Surveys.FindAsync(competition.RelatedSurveyId.Value);
                        surveyTitle = survey?.Title;
                    }

                    competitionDtos.Add(new CompetitionDto
                    {
                        CompetitionId = competition.CompetitionId,
                        Title = competition.Title,
                        Description = competition.Description,
                        RelatedSurveyId = competition.RelatedSurveyId,
                        RelatedSurveyTitle = surveyTitle,
                        StartDate = competition.StartDate,
                        EndDate = competition.EndDate,
                        PrizeDescription = competition.PrizeDescription,
                        Status = competition.Status,
                        CreatedAt = competition.CreatedAt
                    });
                }

                return Ok(competitionDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting competitions");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/competitions/{id}
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<CompetitionDto>> GetCompetitionById(int id)
        {
            try
            {
                var competition = await _context.Competitions.FindAsync(id);
                if (competition == null)
                {
                    return NotFound(new { message = "Competition not found" });
                }

                string? surveyTitle = null;
                if (competition.RelatedSurveyId.HasValue)
                {
                    var survey = await _context.Surveys.FindAsync(competition.RelatedSurveyId.Value);
                    surveyTitle = survey?.Title;
                }

                var competitionDto = new CompetitionDto
                {
                    CompetitionId = competition.CompetitionId,
                    Title = competition.Title,
                    Description = competition.Description,
                    RelatedSurveyId = competition.RelatedSurveyId,
                    RelatedSurveyTitle = surveyTitle,
                    StartDate = competition.StartDate,
                    EndDate = competition.EndDate,
                    PrizeDescription = competition.PrizeDescription,
                    Status = competition.Status,
                    CreatedAt = competition.CreatedAt
                };

                return Ok(competitionDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting competition by id");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/competitions
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<CompetitionDto>> CreateCompetition([FromBody] CreateCompetitionDto createDto)
        {
            try
            {
                if (createDto.RelatedSurveyId.HasValue)
                {
                    var survey = await _context.Surveys.FindAsync(createDto.RelatedSurveyId.Value);
                    if (survey == null)
                    {
                        return BadRequest(new { message = "Related survey not found" });
                    }
                }

                // Tính toán status dựa trên ngày
                var now = DateTime.Now;
                string status;

                if (createDto.StartDate > now)
                {
                    status = "upcoming"; // Chưa bắt đầu
                }
                else if (createDto.EndDate < now)
                {
                    status = "completed"; // Đã kết thúc
                }
                else
                {
                    status = "ongoing"; // Đang diễn ra
                }

                var competition = new Competition
                {
                    Title = createDto.Title,
                    Description = createDto.Description,
                    RelatedSurveyId = createDto.RelatedSurveyId,
                    StartDate = createDto.StartDate,
                    EndDate = createDto.EndDate,
                    PrizeDescription = createDto.PrizeDescription,
                    Status = status, // Sử dụng status đã tính toán
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.Competitions.Add(competition);
                await _context.SaveChangesAsync();

                string? surveyTitle = null;
                if (competition.RelatedSurveyId.HasValue)
                {
                    var survey = await _context.Surveys.FindAsync(competition.RelatedSurveyId.Value);
                    surveyTitle = survey?.Title;
                }

                var competitionDto = new CompetitionDto
                {
                    CompetitionId = competition.CompetitionId,
                    Title = competition.Title,
                    Description = competition.Description,
                    RelatedSurveyId = competition.RelatedSurveyId,
                    RelatedSurveyTitle = surveyTitle,
                    StartDate = competition.StartDate,
                    EndDate = competition.EndDate,
                    PrizeDescription = competition.PrizeDescription,
                    Status = competition.Status,
                    CreatedAt = competition.CreatedAt
                };

                return CreatedAtAction(nameof(GetCompetitionById), new { id = competition.CompetitionId }, competitionDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating competition");
                return StatusCode(500, "Internal server error");
            }
        }

        // PUT: api/competitions/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<CompetitionDto>> UpdateCompetition(int id, [FromBody] UpdateCompetitionDto updateDto)
        {
            try
            {
                var competition = await _context.Competitions.FindAsync(id);
                if (competition == null)
                {
                    return NotFound(new { message = "Competition not found" });
                }

                if (!string.IsNullOrEmpty(updateDto.Title))
                    competition.Title = updateDto.Title;

                if (updateDto.Description != null)
                    competition.Description = updateDto.Description;

                if (updateDto.RelatedSurveyId.HasValue)
                {
                    var survey = await _context.Surveys.FindAsync(updateDto.RelatedSurveyId.Value);
                    if (survey == null)
                    {
                        return BadRequest(new { message = "Related survey not found" });
                    }
                    competition.RelatedSurveyId = updateDto.RelatedSurveyId;
                }

                if (updateDto.StartDate.HasValue)
                    competition.StartDate = updateDto.StartDate.Value;

                if (updateDto.EndDate.HasValue)
                    competition.EndDate = updateDto.EndDate.Value;

                if (updateDto.PrizeDescription != null)
                    competition.PrizeDescription = updateDto.PrizeDescription;

                if (!string.IsNullOrEmpty(updateDto.Status))
                    competition.Status = updateDto.Status;


                var now = DateTime.Now;
                string status;

                if (updateDto.StartDate > now)
                {
                    status = "upcoming";
                }
                else if (updateDto.EndDate < now)
                {
                    status = "completed";
                }
                else
                {
                    status = "ongoing";
                }

                competition.Status = status;
                competition.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                string? surveyTitle = null;
                if (competition.RelatedSurveyId.HasValue)
                {
                    var survey = await _context.Surveys.FindAsync(competition.RelatedSurveyId.Value);
                    surveyTitle = survey?.Title;
                }

                var competitionDto = new CompetitionDto
                {
                    CompetitionId = competition.CompetitionId,
                    Title = competition.Title,
                    Description = competition.Description,
                    RelatedSurveyId = competition.RelatedSurveyId,
                    RelatedSurveyTitle = surveyTitle,
                    StartDate = competition.StartDate,
                    EndDate = competition.EndDate,
                    PrizeDescription = competition.PrizeDescription,
                    Status = competition.Status,
                    CreatedAt = competition.CreatedAt
                };

                return Ok(competitionDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating competition");
                return StatusCode(500, "Internal server error");
            }
        }

        // DELETE: api/competitions/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteCompetition(int id)
        {
            var competition = await _context.Competitions.FindAsync(id);
            if (competition == null) return NotFound();

            // Xóa tất cả competition_winners liên quan trước
            var winners = await _context.CompetitionWinners
                .Where(w => w.CompetitionId == id)
                .ToListAsync();
            _context.CompetitionWinners.RemoveRange(winners);

            // Sau đó mới xóa competition
            _context.Competitions.Remove(competition);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/competitions/active
        [HttpGet("active")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<CompetitionDto>>> GetActiveCompetitions()
        {
            try
            {
                var now = DateTime.Now;
                var competitions = await _context.Competitions
                    .Where(c => c.Status == "ongoing" && c.StartDate <= now && c.EndDate >= now)
                    .OrderBy(c => c.EndDate)
                    .ToListAsync();

                var competitionDtos = new List<CompetitionDto>();
                foreach (var competition in competitions)
                {
                    string? surveyTitle = null;
                    if (competition.RelatedSurveyId.HasValue)
                    {
                        var survey = await _context.Surveys.FindAsync(competition.RelatedSurveyId.Value);
                        surveyTitle = survey?.Title;
                    }

                    competitionDtos.Add(new CompetitionDto
                    {
                        CompetitionId = competition.CompetitionId,
                        Title = competition.Title,
                        Description = competition.Description,
                        RelatedSurveyId = competition.RelatedSurveyId,
                        RelatedSurveyTitle = surveyTitle,
                        StartDate = competition.StartDate,
                        EndDate = competition.EndDate,
                        PrizeDescription = competition.PrizeDescription,
                        Status = competition.Status,
                        CreatedAt = competition.CreatedAt
                    });
                }

                return Ok(competitionDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active competitions");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
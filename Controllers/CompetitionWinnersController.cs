using EnvironmentalSurvey.Data;
using EnvironmentalSurvey.DTOs;
using EnvironmentalSurvey.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnvironmentalSurvey.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CompetitionWinnersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CompetitionWinnersController> _logger;

        public CompetitionWinnersController(AppDbContext context, ILogger<CompetitionWinnersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/competitionwinners/competition/{competitionId}
        [HttpGet("competition/{competitionId}")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<CompetitionWinnerDto>>> GetWinnersByCompetition(int competitionId)
        {
            try
            {
                var winners = await _context.CompetitionWinners
                    .Where(w => w.CompetitionId == competitionId)
                    .OrderBy(w => w.Rank)
                    .ToListAsync();

                var winnerDtos = new List<CompetitionWinnerDto>();
                foreach (var winner in winners)
                {
                    var user = await _context.Users.FindAsync(winner.UserId);
                    var competition = await _context.Competitions.FindAsync(winner.CompetitionId);

                    winnerDtos.Add(new CompetitionWinnerDto
                    {
                        WinnerId = winner.WinnerId,
                        CompetitionId = winner.CompetitionId,
                        CompetitionTitle = competition?.Title ?? "Unknown",
                        UserId = winner.UserId,
                        UserName = user?.FullName ?? "Unknown",
                        Rank = winner.Rank,
                        Score = winner.Score,
                        PrizeDetails = winner.PrizeDetails,
                        AnnouncedAt = winner.AnnouncedAt
                    });
                }

                return Ok(winnerDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting winners by competition");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/competitionwinners/{id}
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<CompetitionWinnerDto>> GetWinnerById(int id)
        {
            try
            {
                var winner = await _context.CompetitionWinners.FindAsync(id);
                if (winner == null)
                {
                    return NotFound(new { message = "Winner not found" });
                }

                var user = await _context.Users.FindAsync(winner.UserId);
                var competition = await _context.Competitions.FindAsync(winner.CompetitionId);

                var winnerDto = new CompetitionWinnerDto
                {
                    WinnerId = winner.WinnerId,
                    CompetitionId = winner.CompetitionId,
                    CompetitionTitle = competition?.Title ?? "Unknown",
                    UserId = winner.UserId,
                    UserName = user?.FullName ?? "Unknown",
                    Rank = winner.Rank,
                    Score = winner.Score,
                    PrizeDetails = winner.PrizeDetails,
                    AnnouncedAt = winner.AnnouncedAt
                };

                return Ok(winnerDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting winner by id");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/competitionwinners
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<CompetitionWinnerDto>> CreateWinner([FromBody] CreateWinnerDto createDto)
        {
            try
            {
                var competition = await _context.Competitions.FindAsync(createDto.CompetitionId);
                if (competition == null)
                {
                    return NotFound(new { message = "Competition not found" });
                }

                var user = await _context.Users.FindAsync(createDto.UserId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                var existingWinner = await _context.CompetitionWinners
                    .FirstOrDefaultAsync(w => w.CompetitionId == createDto.CompetitionId && w.Rank == createDto.Rank);

                if (existingWinner != null)
                {
                    return BadRequest(new { message = "This rank is already assigned in this competition" });
                }

                var winner = new CompetitionWinner
                {
                    CompetitionId = createDto.CompetitionId,
                    UserId = createDto.UserId,
                    Rank = createDto.Rank,
                    Score = createDto.Score,
                    PrizeDetails = createDto.PrizeDetails,
                    AnnouncedAt = DateTime.Now
                };

                _context.CompetitionWinners.Add(winner);
                await _context.SaveChangesAsync();

                var winnerDto = new CompetitionWinnerDto
                {
                    WinnerId = winner.WinnerId,
                    CompetitionId = winner.CompetitionId,
                    CompetitionTitle = competition.Title,
                    UserId = winner.UserId,
                    UserName = user.FullName,
                    Rank = winner.Rank,
                    Score = winner.Score,
                    PrizeDetails = winner.PrizeDetails,
                    AnnouncedAt = winner.AnnouncedAt
                };

                return CreatedAtAction(nameof(GetWinnerById), new { id = winner.WinnerId }, winnerDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating winner");
                return StatusCode(500, "Internal server error");
            }
        }

        // DELETE: api/competitionwinners/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult> DeleteWinner(int id)
        {
            try
            {
                var winner = await _context.CompetitionWinners.FindAsync(id);
                if (winner == null)
                {
                    return NotFound(new { message = "Winner not found" });
                }

                _context.CompetitionWinners.Remove(winner);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting winner");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/competitionwinners/leaderboard
        [HttpGet("leaderboard")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<CompetitionWinnerDto>>> GetLeaderboard()
        {
            try
            {
                var winners = await _context.CompetitionWinners
                    .OrderByDescending(w => w.AnnouncedAt)
                    .ThenBy(w => w.Rank)
                    .Take(50)
                    .ToListAsync();

                var winnerDtos = new List<CompetitionWinnerDto>();
                foreach (var winner in winners)
                {
                    var user = await _context.Users.FindAsync(winner.UserId);
                    var competition = await _context.Competitions.FindAsync(winner.CompetitionId);

                    winnerDtos.Add(new CompetitionWinnerDto
                    {
                        WinnerId = winner.WinnerId,
                        CompetitionId = winner.CompetitionId,
                        CompetitionTitle = competition?.Title ?? "Unknown",
                        UserId = winner.UserId,
                        UserName = user?.FullName ?? "Unknown",
                        Rank = winner.Rank,
                        Score = winner.Score,
                        PrizeDetails = winner.PrizeDetails,
                        AnnouncedAt = winner.AnnouncedAt
                    });
                }

                return Ok(winnerDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting leaderboard");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
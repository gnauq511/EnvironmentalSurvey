using EnvironmentalSurvey.Data;
using EnvironmentalSurvey.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using EnvironmentalSurvey.DTOs;
namespace EnvironmentalSurvey.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EffectiveParticipationController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<EffectiveParticipationController> _logger;

        public EffectiveParticipationController(AppDbContext context, ILogger<EffectiveParticipationController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/effectiveparticipation
        [HttpGet]
        [Authorize(Roles = "admin,faculty")]
        public async Task<ActionResult<IEnumerable<ParticipationDto>>> GetAllParticipations(
            [FromQuery] string? approvalStatus = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var query = _context.EffectiveParticipations.AsQueryable();

                if (!string.IsNullOrEmpty(approvalStatus))
                {
                    query = query.Where(p => p.ApprovalStatus == approvalStatus);
                }

                var participations = await query
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var participationDtos = new List<ParticipationDto>();
                foreach (var participation in participations)
                {
                    var user = await _context.Users.FindAsync(participation.UserId);
                    User? approver = null;
                    if (participation.ApprovedBy.HasValue)
                    {
                        approver = await _context.Users.FindAsync(participation.ApprovedBy.Value);
                    }

                    participationDtos.Add(new ParticipationDto
                    {
                        ParticipationId = participation.ParticipationId,
                        UserId = participation.UserId,
                        UserName = user?.FullName ?? "Unknown",
                        SeminarTitle = participation.SeminarTitle,
                        Location = participation.Location,
                        DateConducted = participation.DateConducted,
                        NumberOfParticipants = participation.NumberOfParticipants,
                        Description = participation.Description,
                        ApprovalStatus = participation.ApprovalStatus,
                        ApprovedBy = participation.ApprovedBy,
                        ApprovedByName = approver?.FullName,
                        CreatedAt = participation.CreatedAt
                    });
                }

                return Ok(participationDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting participations");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/effectiveparticipation/{id}
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<ParticipationDto>> GetParticipationById(int id)
        {
            try
            {
                var participation = await _context.EffectiveParticipations.FindAsync(id);
                if (participation == null)
                {
                    return NotFound(new { message = "Participation not found" });
                }

                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (userRole != "admin" && userRole != "faculty" && participation.UserId != currentUserId)
                {
                    return Forbid();
                }

                var user = await _context.Users.FindAsync(participation.UserId);
                User? approver = null;
                if (participation.ApprovedBy.HasValue)
                {
                    approver = await _context.Users.FindAsync(participation.ApprovedBy.Value);
                }

                var participationDto = new ParticipationDto
                {
                    ParticipationId = participation.ParticipationId,
                    UserId = participation.UserId,
                    UserName = user?.FullName ?? "Unknown",
                    SeminarTitle = participation.SeminarTitle,
                    Location = participation.Location,
                    DateConducted = participation.DateConducted,
                    NumberOfParticipants = participation.NumberOfParticipants,
                    Description = participation.Description,
                    ApprovalStatus = participation.ApprovalStatus,
                    ApprovedBy = participation.ApprovedBy,
                    ApprovedByName = approver?.FullName,
                    CreatedAt = participation.CreatedAt
                };

                return Ok(participationDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting participation by id");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/effectiveparticipation
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<ParticipationDto>> CreateParticipation([FromBody] CreateParticipationDto createDto)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var participation = new EffectiveParticipation
                {
                    UserId = currentUserId,
                    SeminarTitle = createDto.SeminarTitle,
                    Location = createDto.Location,
                    DateConducted = createDto.DateConducted,
                    NumberOfParticipants = createDto.NumberOfParticipants,
                    Description = createDto.Description,
                    ApprovalStatus = "pending",
                    CreatedAt = DateTime.Now
                };

                _context.EffectiveParticipations.Add(participation);
                await _context.SaveChangesAsync();

                var user = await _context.Users.FindAsync(currentUserId);

                var participationDto = new ParticipationDto
                {
                    ParticipationId = participation.ParticipationId,
                    UserId = participation.UserId,
                    UserName = user?.FullName ?? "Unknown",
                    SeminarTitle = participation.SeminarTitle,
                    Location = participation.Location,
                    DateConducted = participation.DateConducted,
                    NumberOfParticipants = participation.NumberOfParticipants,
                    Description = participation.Description,
                    ApprovalStatus = participation.ApprovalStatus,
                    CreatedAt = participation.CreatedAt
                };

                return CreatedAtAction(nameof(GetParticipationById), new { id = participation.ParticipationId }, participationDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating participation");
                return StatusCode(500, "Internal server error");
            }
        }

        // PUT: api/effectiveparticipation/{id}
        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult<ParticipationDto>> UpdateParticipation(int id, [FromBody] UpdateParticipationDto updateDto)
        {
            try
            {
                var participation = await _context.EffectiveParticipations.FindAsync(id);
                if (participation == null)
                {
                    return NotFound(new { message = "Participation not found" });
                }

                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (userRole != "admin" && participation.UserId != currentUserId)
                {
                    return Forbid();
                }

                if (!string.IsNullOrEmpty(updateDto.SeminarTitle))
                    participation.SeminarTitle = updateDto.SeminarTitle;

                if (!string.IsNullOrEmpty(updateDto.Location))
                    participation.Location = updateDto.Location;

                if (updateDto.DateConducted.HasValue)
                    participation.DateConducted = updateDto.DateConducted.Value;

                if (updateDto.NumberOfParticipants.HasValue)
                    participation.NumberOfParticipants = updateDto.NumberOfParticipants;

                if (updateDto.Description != null)
                    participation.Description = updateDto.Description;

                await _context.SaveChangesAsync();

                var user = await _context.Users.FindAsync(participation.UserId);
                User? approver = null;
                if (participation.ApprovedBy.HasValue)
                {
                    approver = await _context.Users.FindAsync(participation.ApprovedBy.Value);
                }

                var participationDto = new ParticipationDto
                {
                    ParticipationId = participation.ParticipationId,
                    UserId = participation.UserId,
                    UserName = user?.FullName ?? "Unknown",
                    SeminarTitle = participation.SeminarTitle,
                    Location = participation.Location,
                    DateConducted = participation.DateConducted,
                    NumberOfParticipants = participation.NumberOfParticipants,
                    Description = participation.Description,
                    ApprovalStatus = participation.ApprovalStatus,
                    ApprovedBy = participation.ApprovedBy,
                    ApprovedByName = approver?.FullName,
                    CreatedAt = participation.CreatedAt
                };

                return Ok(participationDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating participation");
                return StatusCode(500, "Internal server error");
            }
        }

        // PUT: api/effectiveparticipation/{id}/approve
        [HttpPut("{id}/approve")]
        [Authorize(Roles = "admin,faculty")]
        public async Task<ActionResult> ApproveParticipation(int id)
        {
            try
            {
                var participation = await _context.EffectiveParticipations.FindAsync(id);
                if (participation == null)
                {
                    return NotFound(new { message = "Participation not found" });
                }

                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                participation.ApprovalStatus = "approved";
                participation.ApprovedBy = currentUserId;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Participation approved successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving participation");
                return StatusCode(500, "Internal server error");
            }
        }

        // PUT: api/effectiveparticipation/{id}/reject
        [HttpPut("{id}/reject")]
        [Authorize(Roles = "admin,faculty")]
        public async Task<ActionResult> RejectParticipation(int id)
        {
            try
            {
                var participation = await _context.EffectiveParticipations.FindAsync(id);
                if (participation == null)
                {
                    return NotFound(new { message = "Participation not found" });
                }

                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                participation.ApprovalStatus = "rejected";
                participation.ApprovedBy = currentUserId;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Participation rejected successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting participation");
                return StatusCode(500, "Internal server error");
            }
        }

        // DELETE: api/effectiveparticipation/{id}
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult> DeleteParticipation(int id)
        {
            try
            {
                var participation = await _context.EffectiveParticipations.FindAsync(id);
                if (participation == null)
                {
                    return NotFound(new { message = "Participation not found" });
                }

                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (userRole != "admin" && participation.UserId != currentUserId)
                {
                    return Forbid();
                }

                _context.EffectiveParticipations.Remove(participation);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting participation");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/effectiveparticipation/my-participations
        [HttpGet("my-participations")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<ParticipationDto>>> GetMyParticipations()
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var participations = await _context.EffectiveParticipations
                    .Where(p => p.UserId == currentUserId)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();

                var user = await _context.Users.FindAsync(currentUserId);

                var participationDtos = new List<ParticipationDto>();
                foreach (var participation in participations)
                {
                    User? approver = null;
                    if (participation.ApprovedBy.HasValue)
                    {
                        approver = await _context.Users.FindAsync(participation.ApprovedBy.Value);
                    }

                    participationDtos.Add(new ParticipationDto
                    {
                        ParticipationId = participation.ParticipationId,
                        UserId = participation.UserId,
                        UserName = user?.FullName ?? "Unknown",
                        SeminarTitle = participation.SeminarTitle,
                        Location = participation.Location,
                        DateConducted = participation.DateConducted,
                        NumberOfParticipants = participation.NumberOfParticipants,
                        Description = participation.Description,
                        ApprovalStatus = participation.ApprovalStatus,
                        ApprovedBy = participation.ApprovedBy,
                        ApprovedByName = approver?.FullName,
                        CreatedAt = participation.CreatedAt
                    });
                }

                return Ok(participationDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting my participations");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/effectiveparticipation/pending
        [HttpGet("pending")]
        [Authorize(Roles = "admin,faculty")]
        public async Task<ActionResult<IEnumerable<ParticipationDto>>> GetPendingParticipations()
        {
            try
            {
                var participations = await _context.EffectiveParticipations
                    .Where(p => p.ApprovalStatus == "pending")
                    .OrderBy(p => p.CreatedAt)
                    .ToListAsync();

                var participationDtos = new List<ParticipationDto>();
                foreach (var participation in participations)
                {
                    var user = await _context.Users.FindAsync(participation.UserId);

                    participationDtos.Add(new ParticipationDto
                    {
                        ParticipationId = participation.ParticipationId,
                        UserId = participation.UserId,
                        UserName = user?.FullName ?? "Unknown",
                        SeminarTitle = participation.SeminarTitle,
                        Location = participation.Location,
                        DateConducted = participation.DateConducted,
                        NumberOfParticipants = participation.NumberOfParticipants,
                        Description = participation.Description,
                        ApprovalStatus = participation.ApprovalStatus,
                        CreatedAt = participation.CreatedAt
                    });
                }

                return Ok(participationDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending participations");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
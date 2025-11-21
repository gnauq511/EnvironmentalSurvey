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
    public class SupportInfoController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SupportInfoController> _logger;

        public SupportInfoController(AppDbContext context, ILogger<SupportInfoController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/supportinfo
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SupportInfoDto>>> GetAllSupport(
            [FromQuery] string? contactType = null)
        {
            try
            {
                var query = _context.SupportInfos.AsQueryable();

                if (!string.IsNullOrEmpty(contactType))
                {
                    query = query.Where(s => s.ContactType == contactType);
                }

                var supports = await query
                    .Where(s => s.IsActive == true)
                    .ToListAsync();

                var supportDtos = supports.Select(s => new SupportInfoDto
                {
                    SupportId = s.SupportId,
                    ContactType = s.ContactType,
                    ContactValue = s.ContactValue,
                    Description = s.Description,
                    IsActive = s.IsActive
                }).ToList();

                return Ok(supportDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting support info");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/supportinfo/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<SupportInfoDto>> GetSupportById(int id)
        {
            try
            {
                var support = await _context.SupportInfos.FindAsync(id);
                if (support == null)
                {
                    return NotFound(new { message = "Support info not found" });
                }

                var supportDto = new SupportInfoDto
                {
                    SupportId = support.SupportId,
                    ContactType = support.ContactType,
                    ContactValue = support.ContactValue,
                    Description = support.Description,
                    IsActive = support.IsActive
                };

                return Ok(supportDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting support info by id");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/supportinfo
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<SupportInfoDto>> CreateSupport([FromBody] CreateSupportInfoDto createDto)
        {
            try
            {
                var support = new SupportInfo
                {
                    ContactType = createDto.ContactType,
                    ContactValue = createDto.ContactValue,
                    Description = createDto.Description,
                    IsActive = true,
                    UpdatedAt = DateTime.Now
                };

                _context.SupportInfos.Add(support);
                await _context.SaveChangesAsync();

                var supportDto = new SupportInfoDto
                {
                    SupportId = support.SupportId,
                    ContactType = support.ContactType,
                    ContactValue = support.ContactValue,
                    Description = support.Description,
                    IsActive = support.IsActive
                };

                return CreatedAtAction(nameof(GetSupportById), new { id = support.SupportId }, supportDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating support info");
                return StatusCode(500, "Internal server error");
            }
        }

        // PUT: api/supportinfo/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<SupportInfoDto>> UpdateSupport(int id, [FromBody] UpdateSupportInfoDto updateDto)
        {
            try
            {
                var support = await _context.SupportInfos.FindAsync(id);
                if (support == null)
                {
                    return NotFound(new { message = "Support info not found" });
                }

                if (updateDto.ContactType != null)
                    support.ContactType = updateDto.ContactType;

                if (updateDto.ContactValue != null)
                    support.ContactValue = updateDto.ContactValue;

                if (updateDto.Description != null)
                    support.Description = updateDto.Description;

                if (updateDto.IsActive.HasValue)
                    support.IsActive = updateDto.IsActive.Value;

                support.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                var supportDto = new SupportInfoDto
                {
                    SupportId = support.SupportId,
                    ContactType = support.ContactType,
                    ContactValue = support.ContactValue,
                    Description = support.Description,
                    IsActive = support.IsActive
                };

                return Ok(supportDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating support info");
                return StatusCode(500, "Internal server error");
            }
        }

        // DELETE: api/supportinfo/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult> DeleteSupport(int id)
        {
            try
            {
                var support = await _context.SupportInfos.FindAsync(id);
                if (support == null)
                {
                    return NotFound(new { message = "Support info not found" });
                }

                _context.SupportInfos.Remove(support);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting support info");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/supportinfo/types
        [HttpGet("types")]
        public async Task<ActionResult<IEnumerable<string>>> GetContactTypes()
        {
            try
            {
                var types = await _context.SupportInfos
                    .Where(s => s.ContactType != null && s.IsActive == true)
                    .Select(s => s.ContactType!)
                    .Distinct()
                    .OrderBy(t => t)
                    .ToListAsync();

                return Ok(types);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting contact types");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
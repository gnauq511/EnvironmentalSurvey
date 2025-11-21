using EnvironmentalSurvey.Data;
using EnvironmentalSurvey.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EnvironmentalSurvey.DTOs;
using System.Security.Claims;

namespace EnvironmentalSurvey.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FaqsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<FaqsController> _logger;

        public FaqsController(AppDbContext context, ILogger<FaqsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/faqs
        [HttpGet]
        public async Task<ActionResult<IEnumerable<FaqDto>>> GetAllFaqs(
            [FromQuery] string? category = null,
            [FromQuery] bool? isActive = true)
        {
            try
            {
                var query = _context.Faqs.AsQueryable();

                if (!string.IsNullOrEmpty(category))
                {
                    query = query.Where(f => f.Category == category);
                }

                if (isActive.HasValue)
                {
                    query = query.Where(f => f.IsActive == isActive.Value);
                }

                var faqs = await query
                    .OrderBy(f => f.OrderNumber)
                    .ToListAsync();

                var faqDtos = faqs.Select(f => new FaqDto
                {
                    FaqId = f.FaqId,
                    Question = f.Question,
                    Answer = f.Answer,
                    Category = f.Category,
                    OrderNumber = f.OrderNumber,
                    IsActive = f.IsActive,
                    CreatedAt = f.CreatedAt
                }).ToList();

                return Ok(faqDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting FAQs");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/faqs/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<FaqDto>> GetFaqById(int id)
        {
            try
            {
                var faq = await _context.Faqs.FindAsync(id);
                if (faq == null)
                {
                    return NotFound(new { message = "FAQ not found" });
                }

                var faqDto = new FaqDto
                {
                    FaqId = faq.FaqId,
                    Question = faq.Question,
                    Answer = faq.Answer,
                    Category = faq.Category,
                    OrderNumber = faq.OrderNumber,
                    IsActive = faq.IsActive,
                    CreatedAt = faq.CreatedAt
                };

                return Ok(faqDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting FAQ by id");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/faqs
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<FaqDto>> CreateFaq([FromBody] CreateFaqDto createDto)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var faq = new Faq
                {
                    Question = createDto.Question,
                    Answer = createDto.Answer,
                    Category = createDto.Category,
                    OrderNumber = createDto.OrderNumber,
                    IsActive = true,
                    CreatedBy = currentUserId,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.Faqs.Add(faq);
                await _context.SaveChangesAsync();

                var faqDto = new FaqDto
                {
                    FaqId = faq.FaqId,
                    Question = faq.Question,
                    Answer = faq.Answer,
                    Category = faq.Category,
                    OrderNumber = faq.OrderNumber,
                    IsActive = faq.IsActive,
                    CreatedAt = faq.CreatedAt
                };

                return CreatedAtAction(nameof(GetFaqById), new { id = faq.FaqId }, faqDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating FAQ");
                return StatusCode(500, "Internal server error");
            }
        }

        // PUT: api/faqs/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<FaqDto>> UpdateFaq(int id, [FromBody] UpdateFaqDto updateDto)
        {
            try
            {
                var faq = await _context.Faqs.FindAsync(id);
                if (faq == null)
                {
                    return NotFound(new { message = "FAQ not found" });
                }

                if (!string.IsNullOrEmpty(updateDto.Question))
                    faq.Question = updateDto.Question;

                if (!string.IsNullOrEmpty(updateDto.Answer))
                    faq.Answer = updateDto.Answer;

                if (updateDto.Category != null)
                    faq.Category = updateDto.Category;

                if (updateDto.OrderNumber.HasValue)
                    faq.OrderNumber = updateDto.OrderNumber.Value;

                if (updateDto.IsActive.HasValue)
                    faq.IsActive = updateDto.IsActive.Value;

                faq.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                var faqDto = new FaqDto
                {
                    FaqId = faq.FaqId,
                    Question = faq.Question,
                    Answer = faq.Answer,
                    Category = faq.Category,
                    OrderNumber = faq.OrderNumber,
                    IsActive = faq.IsActive,
                    CreatedAt = faq.CreatedAt
                };

                return Ok(faqDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating FAQ");
                return StatusCode(500, "Internal server error");
            }
        }

        // DELETE: api/faqs/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult> DeleteFaq(int id)
        {
            try
            {
                var faq = await _context.Faqs.FindAsync(id);
                if (faq == null)
                {
                    return NotFound(new { message = "FAQ not found" });
                }

                _context.Faqs.Remove(faq);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting FAQ");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/faqs/categories
        [HttpGet("categories")]
        public async Task<ActionResult<IEnumerable<string>>> GetCategories()
        {
            try
            {
                var categories = await _context.Faqs
                    .Where(f => f.Category != null && f.IsActive == true)
                    .Select(f => f.Category!)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categories");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/faqs/by-category/{category}
        [HttpGet("by-category/{category}")]
        public async Task<ActionResult<IEnumerable<FaqDto>>> GetFaqsByCategory(string category)
        {
            try
            {
                var faqs = await _context.Faqs
                    .Where(f => f.Category == category && f.IsActive == true)
                    .OrderBy(f => f.OrderNumber)
                    .ToListAsync();

                var faqDtos = faqs.Select(f => new FaqDto
                {
                    FaqId = f.FaqId,
                    Question = f.Question,
                    Answer = f.Answer,
                    Category = f.Category,
                    OrderNumber = f.OrderNumber,
                    IsActive = f.IsActive,
                    CreatedAt = f.CreatedAt
                }).ToList();

                return Ok(faqDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting FAQs by category");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
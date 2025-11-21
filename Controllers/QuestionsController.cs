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
    public class QuestionsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<QuestionsController> _logger;

        public QuestionsController(AppDbContext context, ILogger<QuestionsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/questions/survey/{surveyId}
        [HttpGet("survey/{surveyId}")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<QuestionDto>>> GetQuestionsBySurvey(int surveyId)
        {
            try
            {
                var survey = await _context.Surveys.FindAsync(surveyId);
                if (survey == null)
                {
                    return NotFound(new { message = "Survey not found" });
                }

                var questions = await _context.Questions
                    .Where(q => q.SurveyId == surveyId)
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

                return Ok(questionDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting questions by survey");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/questions/{id}
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<QuestionDto>> GetQuestionById(int id)
        {
            try
            {
                var question = await _context.Questions.FindAsync(id);
                if (question == null)
                {
                    return NotFound(new { message = "Question not found" });
                }

                var options = await _context.QuestionOptions
                    .Where(o => o.QuestionId == id)
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

                var questionDto = new QuestionDto
                {
                    QuestionId = question.QuestionId,
                    SurveyId = question.SurveyId,
                    QuestionText = question.QuestionText,
                    QuestionType = question.QuestionType,
                    IsRequired = (bool)question.IsRequired,
                    OrderNumber = question.OrderNumber,
                    Options = options
                };

                return Ok(questionDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting question by id");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/questions
        [HttpPost]
        [Authorize(Roles = "admin,faculty")]
        public async Task<ActionResult<QuestionDto>> CreateQuestion([FromBody] CreateQuestionDto createDto)
        {
            try
            {
                var survey = await _context.Surveys.FindAsync(createDto.SurveyId);
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

                var question = new Question
                {
                    SurveyId = createDto.SurveyId,
                    QuestionText = createDto.QuestionText,
                    QuestionType = createDto.QuestionType,
                    IsRequired = createDto.IsRequired,
                    OrderNumber = createDto.OrderNumber,
                    CreatedAt = DateTime.Now
                };

                _context.Questions.Add(question);
                await _context.SaveChangesAsync();

                // Add options if provided
                var optionDtos = new List<QuestionOptionDto>();
                if (createDto.Options != null && createDto.Options.Any())
                {
                    foreach (var optionDto in createDto.Options)
                    {
                        var option = new QuestionOption
                        {
                            QuestionId = question.QuestionId,
                            OptionText = optionDto.OptionText,
                            OrderNumber = optionDto.OrderNumber,
                            IsCorrect = optionDto.IsCorrect
                        };
                        _context.QuestionOptions.Add(option);
                    }
                    await _context.SaveChangesAsync();

                    optionDtos = await _context.QuestionOptions
                        .Where(o => o.QuestionId == question.QuestionId)
                        .Select(o => new QuestionOptionDto
                        {
                            OptionId = o.OptionId,
                            QuestionId = o.QuestionId,
                            OptionText = o.OptionText,
                            OrderNumber = o.OrderNumber,
                            IsCorrect = (bool)o.IsCorrect
                        })
                        .ToListAsync();
                }

                var questionDto = new QuestionDto
                {
                    QuestionId = question.QuestionId,
                    SurveyId = question.SurveyId,
                    QuestionText = question.QuestionText,
                    QuestionType = question.QuestionType,
                    IsRequired = (bool)question.IsRequired,
                    OrderNumber = question.OrderNumber,
                    Options = optionDtos
                };

                return CreatedAtAction(nameof(GetQuestionById), new { id = question.QuestionId }, questionDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating question");
                return StatusCode(500, "Internal server error");
            }
        }

        // PUT: api/questions/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "admin,faculty")]
        public async Task<ActionResult<QuestionDto>> UpdateQuestion(int id, [FromBody] UpdateQuestionDto updateDto)
        {
            try
            {
                var question = await _context.Questions.FindAsync(id);
                if (question == null)
                {
                    return NotFound(new { message = "Question not found" });
                }

                var survey = await _context.Surveys.FindAsync(question.SurveyId);
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (userRole != "admin" && survey?.CreatedBy != currentUserId)
                {
                    return Forbid();
                }

                if (!string.IsNullOrEmpty(updateDto.QuestionText))
                    question.QuestionText = updateDto.QuestionText;

                if (!string.IsNullOrEmpty(updateDto.QuestionType))
                    question.QuestionType = updateDto.QuestionType;

                if (updateDto.IsRequired.HasValue)
                    question.IsRequired = updateDto.IsRequired.Value;

                if (updateDto.OrderNumber.HasValue)
                    question.OrderNumber = updateDto.OrderNumber.Value;

                await _context.SaveChangesAsync();

                var options = await _context.QuestionOptions
                    .Where(o => o.QuestionId == id)
                    .Select(o => new QuestionOptionDto
                    {
                        OptionId = o.OptionId,
                        QuestionId = o.QuestionId,
                        OptionText = o.OptionText,
                        OrderNumber = o.OrderNumber,
                        IsCorrect = (bool)o.IsCorrect
                    })
                    .ToListAsync();

                var questionDto = new QuestionDto
                {
                    QuestionId = question.QuestionId,
                    SurveyId = question.SurveyId,
                    QuestionText = question.QuestionText,
                    QuestionType = question.QuestionType,
                    IsRequired = (bool)question.IsRequired,
                    OrderNumber = question.OrderNumber,
                    Options = options
                };

                return Ok(questionDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating question");
                return StatusCode(500, "Internal server error");
            }
        }

        // DELETE: api/questions/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin,faculty")]
        public async Task<ActionResult> DeleteQuestion(int id)
        {
            try
            {
                var question = await _context.Questions.FindAsync(id);
                if (question == null)
                {
                    return NotFound(new { message = "Question not found" });
                }

                var survey = await _context.Surveys.FindAsync(question.SurveyId);
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (userRole != "admin" && survey?.CreatedBy != currentUserId)
                {
                    return Forbid();
                }

                _context.Questions.Remove(question);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting question");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/questions/{id}/options
        [HttpPost("{id}/options")]
        [Authorize(Roles = "admin,faculty")]
        public async Task<ActionResult<QuestionOptionDto>> AddOption(int id, [FromBody] CreateQuestionOptionDto createDto)
        {
            try
            {
                var question = await _context.Questions.FindAsync(id);
                if (question == null)
                {
                    return NotFound(new { message = "Question not found" });
                }

                var survey = await _context.Surveys.FindAsync(question.SurveyId);
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (userRole != "admin" && survey?.CreatedBy != currentUserId)
                {
                    return Forbid();
                }

                var option = new QuestionOption
                {
                    QuestionId = id,
                    OptionText = createDto.OptionText,
                    OrderNumber = createDto.OrderNumber,
                    IsCorrect = createDto.IsCorrect
                };

                _context.QuestionOptions.Add(option);
                await _context.SaveChangesAsync();

                var optionDto = new QuestionOptionDto
                {
                    OptionId = option.OptionId,
                    QuestionId = option.QuestionId,
                    OptionText = option.OptionText,
                    OrderNumber = option.OrderNumber,
                    IsCorrect = (bool)option.IsCorrect
                };

                return Ok(optionDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding option");
                return StatusCode(500, "Internal server error");
            }
        }

        // DELETE: api/questions/options/{optionId}
        [HttpDelete("options/{optionId}")]
        [Authorize(Roles = "admin,faculty")]
        public async Task<ActionResult> DeleteOption(int optionId)
        {
            try
            {
                var option = await _context.QuestionOptions.FindAsync(optionId);
                if (option == null)
                {
                    return NotFound(new { message = "Option not found" });
                }

                var question = await _context.Questions.FindAsync(option.QuestionId);
                var survey = await _context.Surveys.FindAsync(question?.SurveyId);
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (userRole != "admin" && survey?.CreatedBy != currentUserId)
                {
                    return Forbid();
                }

                _context.QuestionOptions.Remove(option);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting option");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
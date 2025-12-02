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
    public class AnswersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AnswersController> _logger;

        public AnswersController(AppDbContext context, ILogger<AnswersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/answers/response/{responseId}
        [HttpGet("response/{responseId}")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<AnswerDto>>> GetAnswersByResponse(int responseId)
        {
            try
            {
                var response = await _context.SurveyResponses.FindAsync(responseId);
                if (response == null)
                {
                    return NotFound(new { message = "Response not found" });
                }

                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                var survey = await _context.Surveys.FindAsync(response.SurveyId);

                // Check permission
                if (userRole != "admin" && userRole != "faculty" &&
                    response.UserId != currentUserId && survey?.CreatedBy != currentUserId)
                {
                    return Forbid();
                }

                var answers = await _context.Answers
                    .Where(a => a.ResponseId == responseId)
                    .ToListAsync();

                var answerDtos = new List<AnswerDto>();
                foreach (var answer in answers)
                {
                    var question = await _context.Questions.FindAsync(answer.QuestionId);
                    QuestionOptionDto? optionDto = null;

                    if (answer.OptionId.HasValue)
                    {
                        var option = await _context.QuestionOptions.FindAsync(answer.OptionId.Value);
                        if (option != null)
                        {
                            optionDto = new QuestionOptionDto
                            {
                                OptionId = option.OptionId,
                                QuestionId = option.QuestionId,
                                OptionText = option.OptionText,
                                OrderNumber = option.OrderNumber,
                                IsCorrect = option.IsCorrect
                            };
                        }
                    }

                    answerDtos.Add(new AnswerDto
                    {
                        AnswerId = answer.AnswerId,
                        ResponseId = answer.ResponseId,
                        QuestionId = answer.QuestionId,
                        QuestionText = question?.QuestionText ?? "",
                        QuestionType = question?.QuestionType ?? "",
                        OptionId = answer.OptionId,
                        SelectedOption = optionDto,
                        TextAnswer = answer.TextAnswer,
                        CreatedAt = answer.CreatedAt
                    });
                }

                return Ok(answerDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting answers by response");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/answers/{id}
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<AnswerDto>> GetAnswerById(int id)
        {
            try
            {
                var answer = await _context.Answers.FindAsync(id);
                if (answer == null)
                {
                    return NotFound(new { message = "Answer not found" });
                }

                var response = await _context.SurveyResponses.FindAsync(answer.ResponseId);
                if (response == null)
                {
                    return NotFound(new { message = "Response not found" });
                }

                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                var survey = await _context.Surveys.FindAsync(response.SurveyId);

                // Check permission
                if (userRole != "admin" && userRole != "faculty" &&
                    response.UserId != currentUserId && survey?.CreatedBy != currentUserId)
                {
                    return Forbid();
                }

                var question = await _context.Questions.FindAsync(answer.QuestionId);
                QuestionOptionDto? optionDto = null;

                if (answer.OptionId.HasValue)
                {
                    var option = await _context.QuestionOptions.FindAsync(answer.OptionId.Value);
                    if (option != null)
                    {
                        optionDto = new QuestionOptionDto
                        {
                            OptionId = option.OptionId,
                            QuestionId = option.QuestionId,
                            OptionText = option.OptionText,
                            OrderNumber = option.OrderNumber,
                            IsCorrect = option.IsCorrect
                        };
                    }
                }

                var answerDto = new AnswerDto
                {
                    AnswerId = answer.AnswerId,
                    ResponseId = answer.ResponseId,
                    QuestionId = answer.QuestionId,
                    QuestionText = question?.QuestionText ?? "",
                    QuestionType = question?.QuestionType ?? "",
                    OptionId = answer.OptionId,
                    SelectedOption = optionDto,
                    TextAnswer = answer.TextAnswer,
                    CreatedAt = answer.CreatedAt
                };

                return Ok(answerDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting answer by id");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/answers
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<AnswerDto>> CreateAnswer([FromBody] CreateAnswerDto createDto)
        {
            try
            {
                var response = await _context.SurveyResponses.FindAsync(createDto.ResponseId);
                if (response == null)
                {
                    return NotFound(new { message = "Response not found" });
                }

                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // Only the response owner can add answers
                if (response.UserId != currentUserId)
                {
                    return Forbid();
                }

                var question = await _context.Questions.FindAsync(createDto.QuestionId);
                if (question == null)
                {
                    return NotFound(new { message = "Question not found" });
                }

                // Check if answer already exists for this question in this response
                var existingAnswer = await _context.Answers
                    .FirstOrDefaultAsync(a => a.ResponseId == createDto.ResponseId &&
                                            a.QuestionId == createDto.QuestionId);

                if (existingAnswer != null)
                {
                    return BadRequest(new { message = "Answer already exists for this question" });
                }

                // Validate answer based on question type
                if (question.QuestionType == "text")
                {
                    if (string.IsNullOrEmpty(createDto.TextAnswer))
                    {
                        return BadRequest(new { message = "Text answer is required for text questions" });
                    }
                }
                else
                {
                    if (!createDto.OptionId.HasValue)
                    {
                        return BadRequest(new { message = "Option ID is required for choice questions" });
                    }

                    var option = await _context.QuestionOptions.FindAsync(createDto.OptionId.Value);
                    if (option == null || option.QuestionId != createDto.QuestionId)
                    {
                        return BadRequest(new { message = "Invalid option for this question" });
                    }
                }

                var answer = new Answer
                {
                    ResponseId = createDto.ResponseId,
                    QuestionId = createDto.QuestionId,
                    OptionId = createDto.OptionId,
                    TextAnswer = createDto.TextAnswer,
                    CreatedAt = DateTime.Now
                };

                _context.Answers.Add(answer);
                await _context.SaveChangesAsync();

                QuestionOptionDto? optionDto = null;
                if (answer.OptionId.HasValue)
                {
                    var option = await _context.QuestionOptions.FindAsync(answer.OptionId.Value);
                    if (option != null)
                    {
                        optionDto = new QuestionOptionDto
                        {
                            OptionId = option.OptionId,
                            QuestionId = option.QuestionId,
                            OptionText = option.OptionText,
                            OrderNumber = option.OrderNumber,
                            IsCorrect = option.IsCorrect
                        };
                    }
                }

                var answerDto = new AnswerDto
                {
                    AnswerId = answer.AnswerId,
                    ResponseId = answer.ResponseId,
                    QuestionId = answer.QuestionId,
                    QuestionText = question.QuestionText,
                    QuestionType = question.QuestionType,
                    OptionId = answer.OptionId,
                    SelectedOption = optionDto,
                    TextAnswer = answer.TextAnswer,
                    CreatedAt = answer.CreatedAt
                };

                return CreatedAtAction(nameof(GetAnswerById), new { id = answer.AnswerId }, answerDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating answer");
                return StatusCode(500, "Internal server error");
            }
        }

        // PUT: api/answers/{id}
        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult<AnswerDto>> UpdateAnswer(int id, [FromBody] UpdateAnswerDto updateDto)
        {
            try
            {
                var answer = await _context.Answers.FindAsync(id);
                if (answer == null)
                {
                    return NotFound(new { message = "Answer not found" });
                }

                var response = await _context.SurveyResponses.FindAsync(answer.ResponseId);
                if (response == null)
                {
                    return NotFound(new { message = "Response not found" });
                }

                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // Only the response owner can update answers
                if (response.UserId != currentUserId)
                {
                    return Forbid();
                }

                var question = await _context.Questions.FindAsync(answer.QuestionId);
                if (question == null)
                {
                    return NotFound(new { message = "Question not found" });
                }

                // Update based on question type
                if (question.QuestionType == "text")
                {
                    if (updateDto.TextAnswer != null)
                    {
                        answer.TextAnswer = updateDto.TextAnswer;
                    }
                }
                else
                {
                    if (updateDto.OptionId.HasValue)
                    {
                        var option = await _context.QuestionOptions.FindAsync(updateDto.OptionId.Value);
                        if (option == null || option.QuestionId != answer.QuestionId)
                        {
                            return BadRequest(new { message = "Invalid option for this question" });
                        }
                        answer.OptionId = updateDto.OptionId.Value;
                    }
                }

                await _context.SaveChangesAsync();

                QuestionOptionDto? optionDto = null;
                if (answer.OptionId.HasValue)
                {
                    var option = await _context.QuestionOptions.FindAsync(answer.OptionId.Value);
                    if (option != null)
                    {
                        optionDto = new QuestionOptionDto
                        {
                            OptionId = option.OptionId,
                            QuestionId = option.QuestionId,
                            OptionText = option.OptionText,
                            OrderNumber = option.OrderNumber,
                            IsCorrect = option.IsCorrect
                        };
                    }
                }

                var answerDto = new AnswerDto
                {
                    AnswerId = answer.AnswerId,
                    ResponseId = answer.ResponseId,
                    QuestionId = answer.QuestionId,
                    QuestionText = question.QuestionText,
                    QuestionType = question.QuestionType,
                    OptionId = answer.OptionId,
                    SelectedOption = optionDto,
                    TextAnswer = answer.TextAnswer,
                    CreatedAt = answer.CreatedAt
                };

                return Ok(answerDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating answer");
                return StatusCode(500, "Internal server error");
            }
        }

        // DELETE: api/answers/{id}
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult> DeleteAnswer(int id)
        {
            try
            {
                var answer = await _context.Answers.FindAsync(id);
                if (answer == null)
                {
                    return NotFound(new { message = "Answer not found" });
                }

                var response = await _context.SurveyResponses.FindAsync(answer.ResponseId);
                if (response == null)
                {
                    return NotFound(new { message = "Response not found" });
                }

                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                // Only the response owner or admin can delete answers
                if (userRole != "admin" && response.UserId != currentUserId)
                {
                    return Forbid();
                }

                _context.Answers.Remove(answer);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting answer");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/answers/question/{questionId}/statistics
        [HttpGet("question/{questionId}/statistics")]
        [Authorize(Roles = "admin,faculty")]
        public async Task<ActionResult<AnswerStatisticsDto>> GetAnswerStatistics(int questionId)
        {
            try
            {
                var question = await _context.Questions.FindAsync(questionId);
                if (question == null)
                {
                    return NotFound(new { message = "Question not found" });
                }

                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                var survey = await _context.Surveys.FindAsync(question.SurveyId);

                // Check permission
                if (userRole != "admin" && survey?.CreatedBy != currentUserId)
                {
                    return Forbid();
                }

                var answers = await _context.Answers
                    .Where(a => a.QuestionId == questionId)
                    .ToListAsync();

                var statistics = new AnswerStatisticsDto
                {
                    QuestionId = question.QuestionId,
                    QuestionText = question.QuestionText,
                    QuestionType = question.QuestionType,
                    TotalAnswers = answers.Count
                };

                if (question.QuestionType == "text")
                {
                    statistics.TextAnswers = answers
                        .Where(a => !string.IsNullOrEmpty(a.TextAnswer))
                        .Select(a => a.TextAnswer!)
                        .ToList();
                }
                else
                {
                    var options = await _context.QuestionOptions
                        .Where(o => o.QuestionId == questionId)
                        .ToListAsync();

                    var optionStats = new List<OptionStatisticsDto>();
                    foreach (var option in options)
                    {
                        var count = answers.Count(a => a.OptionId == option.OptionId);
                        var percentage = answers.Count > 0 ? (decimal)count / answers.Count * 100 : 0;

                        optionStats.Add(new OptionStatisticsDto
                        {
                            OptionId = option.OptionId,
                            OptionText = option.OptionText,
                            Count = count,
                            Percentage = Math.Round(percentage, 2),
                            IsCorrect = option.IsCorrect
                        });
                    }

                    statistics.OptionStatistics = optionStats.OrderByDescending(o => o.Count).ToList();
                }

                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting answer statistics");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/answers/survey/{surveyId}/summary
        [HttpGet("survey/{surveyId}/summary")]
        [Authorize(Roles = "admin,faculty")]
        public async Task<ActionResult<IEnumerable<QuestionAnswerSummaryDto>>> GetSurveySummary(int surveyId)
        {
            try
            {
                var survey = await _context.Surveys.FindAsync(surveyId);
                if (survey == null)
                {
                    return NotFound(new { message = "Survey not found" });
                }

                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                // Check permission
                if (userRole != "admin" && survey.CreatedBy != currentUserId)
                {
                    return Forbid();
                }

                var questions = await _context.Questions
                    .Where(q => q.SurveyId == surveyId)
                    .OrderBy(q => q.OrderNumber)
                    .ToListAsync();

                var summaries = new List<QuestionAnswerSummaryDto>();

                foreach (var question in questions)
                {
                    var answers = await _context.Answers
                        .Where(a => a.QuestionId == question.QuestionId)
                        .ToListAsync();

                    int correctAnswers = 0;
                    int incorrectAnswers = 0;

                    if (question.QuestionType != "text")
                    {
                        foreach (var answer in answers.Where(a => a.OptionId.HasValue))
                        {
                            var option = await _context.QuestionOptions.FindAsync(answer.OptionId!.Value);
                            if (option != null)
                            {
                                if (option.IsCorrect)
                                    correctAnswers++;
                                else
                                    incorrectAnswers++;
                            }
                        }
                    }

                    var totalResponses = answers.Count;
                    var correctPercentage = totalResponses > 0
                        ? (decimal)correctAnswers / totalResponses * 100
                        : 0;

                    summaries.Add(new QuestionAnswerSummaryDto
                    {
                        QuestionId = question.QuestionId,
                        QuestionText = question.QuestionText,
                        QuestionType = question.QuestionType,
                        TotalResponses = totalResponses,
                        CorrectAnswers = correctAnswers,
                        IncorrectAnswers = incorrectAnswers,
                        CorrectPercentage = Math.Round(correctPercentage, 2)
                    });
                }

                return Ok(summaries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting survey summary");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
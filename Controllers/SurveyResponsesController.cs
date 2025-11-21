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
    public class SurveyResponsesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SurveyResponsesController> _logger;

        public SurveyResponsesController(AppDbContext context, ILogger<SurveyResponsesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/surveyresponses/survey/{surveyId}
        [HttpGet("survey/{surveyId}")]
        [Authorize(Roles = "admin,faculty")]
        public async Task<ActionResult<IEnumerable<SurveyResponseDto>>> GetResponsesBySurvey(int surveyId)
        {
            try
            {
                var responses = await _context.SurveyResponses
                    .Where(r => r.SurveyId == surveyId)
                    .OrderByDescending(r => r.SubmittedAt)
                    .ToListAsync();

                var responseDtos = new List<SurveyResponseDto>();
                foreach (var response in responses)
                {
                    var user = await _context.Users.FindAsync(response.UserId);
                    responseDtos.Add(new SurveyResponseDto
                    {
                        ResponseId = response.ResponseId,
                        SurveyId = response.SurveyId,
                        UserId = response.UserId,
                        UserName = user?.FullName ?? "Unknown",
                        SubmittedAt = response.SubmittedAt,
                        Score = response.Score
                    });
                }

                return Ok(responseDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting responses by survey");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/surveyresponses/{id}
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<SurveyResponseDetailDto>> GetResponseById(int id)
        {
            try
            {
                var response = await _context.SurveyResponses.FindAsync(id);
                if (response == null)
                {
                    return NotFound(new { message = "Response not found" });
                }

                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                var survey = await _context.Surveys.FindAsync(response.SurveyId);

                if (userRole != "admin" && userRole != "faculty" &&
                    response.UserId != currentUserId && survey?.CreatedBy != currentUserId)
                {
                    return Forbid();
                }

                var user = await _context.Users.FindAsync(response.UserId);
                var answers = await _context.Answers
                    .Where(a => a.ResponseId == id)
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
                        OptionId = answer.OptionId,
                        SelectedOption = optionDto,
                        TextAnswer = answer.TextAnswer
                    });
                }

                var detailDto = new SurveyResponseDetailDto
                {
                    ResponseId = response.ResponseId,
                    SurveyId = response.SurveyId,
                    UserId = response.UserId,
                    UserName = user?.FullName ?? "Unknown",
                    SubmittedAt = response.SubmittedAt,
                    Score = response.Score,
                    Answers = answerDtos
                };

                return Ok(detailDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting response by id");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/surveyresponses
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<SurveyResponseDto>> SubmitResponse([FromBody] SubmitResponseDto submitDto)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var survey = await _context.Surveys.FindAsync(submitDto.SurveyId);
                if (survey == null)
                {
                    return NotFound(new { message = "Survey not found" });
                }

                if (!survey.IsActive || survey.EndDate < DateTime.Now)
                {
                    return BadRequest(new { message = "Survey is not available" });
                }

                var existingResponse = await _context.SurveyResponses
                    .FirstOrDefaultAsync(r => r.SurveyId == submitDto.SurveyId && r.UserId == currentUserId);

                if (existingResponse != null)
                {
                    return BadRequest(new { message = "You have already submitted a response for this survey" });
                }

                var response = new SurveyResponse
                {
                    SurveyId = submitDto.SurveyId,
                    UserId = currentUserId,
                    SubmittedAt = DateTime.Now
                };

                _context.SurveyResponses.Add(response);
                await _context.SaveChangesAsync();

                // Save answers
                foreach (var answerDto in submitDto.Answers)
                {
                    var answer = new Answer
                    {
                        ResponseId = response.ResponseId,
                        QuestionId = answerDto.QuestionId,
                        OptionId = answerDto.OptionId,
                        TextAnswer = answerDto.TextAnswer,
                        CreatedAt = DateTime.Now
                    };
                    _context.Answers.Add(answer);
                }
                await _context.SaveChangesAsync();

                // Calculate score if applicable
                var score = await CalculateScoreAsync(response.ResponseId);
                if (score.HasValue)
                {
                    response.Score = score.Value;
                    await _context.SaveChangesAsync();
                }

                var user = await _context.Users.FindAsync(currentUserId);
                var responseDto = new SurveyResponseDto
                {
                    ResponseId = response.ResponseId,
                    SurveyId = response.SurveyId,
                    UserId = response.UserId,
                    UserName = user?.FullName ?? "Unknown",
                    SubmittedAt = response.SubmittedAt,
                    Score = response.Score
                };

                return CreatedAtAction(nameof(GetResponseById), new { id = response.ResponseId }, responseDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting response");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/surveyresponses/my-responses
        [HttpGet("my-responses")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<SurveyResponseDto>>> GetMyResponses()
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var responses = await _context.SurveyResponses
                    .Where(r => r.UserId == currentUserId)
                    .OrderByDescending(r => r.SubmittedAt)
                    .ToListAsync();

                var user = await _context.Users.FindAsync(currentUserId);
                var responseDtos = responses.Select(r => new SurveyResponseDto
                {
                    ResponseId = r.ResponseId,
                    SurveyId = r.SurveyId,
                    UserId = r.UserId,
                    UserName = user?.FullName ?? "Unknown",
                    SubmittedAt = r.SubmittedAt,
                    Score = r.Score
                }).ToList();

                return Ok(responseDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting my responses");
                return StatusCode(500, "Internal server error");
            }
        }

        // DELETE: api/surveyresponses/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult> DeleteResponse(int id)
        {
            try
            {
                var response = await _context.SurveyResponses.FindAsync(id);
                if (response == null)
                {
                    return NotFound(new { message = "Response not found" });
                }

                _context.SurveyResponses.Remove(response);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting response");
                return StatusCode(500, "Internal server error");
            }
        }

        // Helper method to calculate score
        private async Task<decimal?> CalculateScoreAsync(int responseId)
        {
            try
            {
                var answers = await _context.Answers
                    .Where(a => a.ResponseId == responseId && a.OptionId.HasValue)
                    .ToListAsync();

                if (!answers.Any())
                {
                    return null;
                }

                int correctAnswers = 0;
                int totalQuestions = 0;

                foreach (var answer in answers)
                {
                    if (answer.OptionId.HasValue)
                    {
                        var option = await _context.QuestionOptions.FindAsync(answer.OptionId.Value);
                        if (option != null)
                        {
                            totalQuestions++;
                            if (option.IsCorrect)
                            {
                                correctAnswers++;
                            }
                        }
                    }
                }

                if (totalQuestions == 0)
                {
                    return null;
                }

                return (decimal)correctAnswers / totalQuestions * 100;
            }
            catch
            {
                return null;
            }
        }
    }
}
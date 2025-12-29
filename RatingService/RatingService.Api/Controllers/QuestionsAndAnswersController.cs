using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RatingService.Core.Interfaces;
using RatingService.Shared.Dtos;
using Serilog;

namespace RatingService.Controllers;


[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class QuestionsAndAnswersController(IQuestionsAndAnswersService questionsAndAnswersService) : Controller
{
    
    [Authorize]
    [HttpPost("questions")]
    public async Task<IActionResult> CreateQuestion([FromBody] CreateQuestionDto questionDto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(kvp => kvp.Value.Errors.Any())
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );

            Log.Warning("ReviewsController.CreateReview: Validation failed {@Errors}", errors);

            return BadRequest(new
            {
                ErrorCode = "ValidationError",
                Message = "Некорректные входные данные.",
                Details = errors
            });
        }
        
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            Log.Error("QuestionAndAnswerController.CreateQuestion: Invalid user claim. Claims: {@Claims}",
                User.Claims.Select(c => new { c.Type, c.Value }));
            return Unauthorized(new
            {
                ErrorCode = "UserIdentificationFailed",
                Message = "Не удалось определить пользователя из токена."
            });
        }

        try
        {
            Log.Information("QuestionAndAnswerController.CreateQuestion: Creating Question {@Dto}", questionDto);

            var result = await questionsAndAnswersService.CreateQuestion(questionDto);
            return CreatedAtAction(nameof(CreateQuestion), result);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "QuestionAndAnswerController.CreateQuestion: Unexpected error {@Dto}", questionDto);

            return StatusCode(500, new
            {
                ErrorCode = "InternalServerError",
                Message = "Ошибка при создании отзыва.",
                ExceptionType = ex.GetType().Name,
                StackTrace = ex.StackTrace
            });
        }
        
    }
    
    [Authorize]
    [HttpPost("answers")]
    public async Task<IActionResult> CreateAnswer([FromBody] CreateAnswerDto answerDto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(kvp => kvp.Value.Errors.Any())
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );

            Log.Warning("QuestionAndAnswerController.CreateAnswer: Validation failed {@Errors}", errors);

            return BadRequest(new
            {
                ErrorCode = "ValidationError",
                Message = "Некорректные входные данные.",
                Details = errors
            });
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            Log.Error("QuestionAndAnswerController.CreateAnswer: Invalid user claim. Claims: {@Claims}",
                User.Claims.Select(c => new { c.Type, c.Value }));
            return Unauthorized(new
            {
                ErrorCode = "UserIdentificationFailed",
                Message = "Не удалось определить пользователя из токена."
            });
        }

        try
        {
            Log.Information("QuestionAndAnswerController.CreateAnswer: Creating Answer {@Dto}", answerDto);

            var result = await questionsAndAnswersService.CreateAnswer(answerDto);
            return CreatedAtAction(nameof(CreateAnswer), result);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "QuestionAndAnswerController.CreateAnswer: Unexpected error {@Dto}", answerDto);

            return StatusCode(500, new
            {
                ErrorCode = "InternalServerError",
                Message = "Ошибка при создании отзыва.",
                ExceptionType = ex.GetType().Name,
                StackTrace = ex.StackTrace
            });
        }
        
 
    }

    [HttpPut("questions")]
    [Authorize]
    public async Task<IActionResult> UpdateQuestion([FromBody] UpdateQuestionDto questionDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { Error = "Invalid data." });
        
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var currentUserId))
            return Unauthorized(new { Error = "Cannot determine user." });

        try
        {
            var success = await questionsAndAnswersService.UpdateQuestion(questionDto);
            return success ? NoContent() : BadRequest();
        }
        catch (KeyNotFoundException ex)
        {
            Log.Warning(ex, "QuestionAndAnswerController.UpdateQuestion: Question {QuestionId} not found",
                questionDto.QuestionId);
            return NotFound(new { Error = ex.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "QuestionAndAnswerController.UpdateQuestion: Error updating question {QuestionId}", questionDto.QuestionId);

            return StatusCode(500, new
            {
                Error = "Server error.",
                ExceptionMessage = ex.Message,
                ExceptionType = ex.GetType().Name,
                StackTrace = ex.StackTrace
            });
        }
        
    }

    [HttpPut("answers")]
    [Authorize]
    public async Task<IActionResult> UpdateAnswer([FromBody] UpdateAnswerDto answerDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { Error = "Invalid data." });
        
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var currentUserId))
            return Unauthorized(new { Error = "Cannot determine user." });
        try
        {
            var success = await questionsAndAnswersService.UpdateAnswer(answerDto);
            return success ? NoContent() : BadRequest();
        }
        catch (KeyNotFoundException ex)
        {
            Log.Warning(ex, "QuestionAndAnswerController.UpdateAnswer: Answer {AnswerId} not found",
                answerDto.AnswerId);
            return NotFound(new { Error = ex.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "QuestionAndAnswerController.UpdateAnswer: Error updating answer {AnswerId`}", answerDto.AnswerId);

            return StatusCode(500, new
            {
                Error = "Server error.",
                ExceptionMessage = ex.Message,
                ExceptionType = ex.GetType().Name,
                StackTrace = ex.StackTrace
            });
        }
       
    }
 
    [HttpDelete("question/{questionId:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteQuestion(Guid questionId)
    {
        try
        {
            var success = await questionsAndAnswersService.DeleteQuestion(questionId);
            return success ? NoContent() : NotFound();
        }
        catch (KeyNotFoundException ex)
        {
            Log.Warning(ex, "QuestionAndAnswerController.DeleteQuestion: Question {QuestionId} not found", questionId);
            return NotFound(new { Error = ex.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "QuestionAndAnswerController.DeleteQuestion: Error deleting question {QuestionId}", questionId);
            return StatusCode(500, new { Error = "Server error." });
        }
    }

    [HttpDelete("answer/{answerId:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteAnswer(Guid answerId)
    {
        try
        {
            var success = await questionsAndAnswersService.DeleteAnswer(answerId);
            return success ? NoContent() : NotFound();
        }
        catch (KeyNotFoundException ex)
        {   
            Log.Warning(ex, "QuestionAndAnswerController.DeleteAnswer: Answer {AnswerId} not found", answerId);
            return NotFound(new { Error = ex.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "QuestionAndAnswerController.DeleteAnswer: Error deleting answer {AnswerId}", answerId);
            return StatusCode(500, new { Error = "Server error." });
        }
     
    }

    [HttpGet("product/{productId:guid}")]
    public async Task<IActionResult> GetProductQuestionsAndAnswers(
        Guid productId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1 || pageSize < 1)
            return BadRequest(new { Error = "Page and PageSize must be greater than 0." });
        try
        {
            var result = await questionsAndAnswersService.GetProductQuestionsAndAnswers(
                productId, page, pageSize);
            
            if (!result.Any())
                return NotFound(new { Error = "Not found!" });

            return Ok(result);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "QuestionAndAnswerController.GetProductQuestionsAndAnswers: Error for product {ProductId}", productId);
            return StatusCode(500, new { Error = "Server error." });
        }

    }
}
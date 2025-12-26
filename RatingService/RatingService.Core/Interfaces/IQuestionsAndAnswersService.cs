using RatingService.Core.Entities;
using RatingService.Shared.Dtos;

namespace RatingService.Core.Interfaces;

public interface IQuestionsAndAnswersService
{
    Task<CreateQuestionDto> CreateQuestion(CreateQuestionDto questionDto);
    Task<CreateAnswerDto> CreateAnswer(CreateAnswerDto answerDto);

    Task<bool> DeleteQuestion(Guid questionId);
    Task<bool> DeleteAnswer(Guid answerId);

    Task<bool> UpdateQuestion(UpdateQuestionDto questionDto);
    Task<bool> UpdateAnswer(UpdateAnswerDto answerDto);

    Task<IEnumerable<ProductQuestion>> GetProductQuestionsAndAnswers(Guid productId, int pageSize, int pageNumber);
}
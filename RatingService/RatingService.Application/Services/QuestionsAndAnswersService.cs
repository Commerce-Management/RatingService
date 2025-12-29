using AutoMapper;
using RatingService.Core.Entities;
using RatingService.Core.Interfaces;
using RatingService.Infrastructure.Interfaces.Base;
using RatingService.Infrastructure.Interfaces.Entities;
using RatingService.Shared.Dtos;

namespace RatingService.Application.Services;

public class QuestionsAndAnswersService(
    IUnitOfWork unitOfWork,
    IProductQuestionRepository questionRepository,
    IProductAnswerRepository answerRepository,
    IMapper mapper) : IQuestionsAndAnswersService
{
    public async Task<CreateQuestionDto> CreateQuestion(CreateQuestionDto questionDto)
    {
        try
        {
            var question = mapper.Map<ProductQuestion>(questionDto);

            await unitOfWork.BeginTransactionAsync();

            var insertedQuestion = await questionRepository.InsertAsync(question);

            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitTransactionAsync();
            
            var createdQuestionForTest = await questionRepository.GetQuestionByIdAsync(insertedQuestion.Id);
            return mapper.Map<CreateQuestionDto>(createdQuestionForTest); // this is wrong
        }
        catch (Exception)
        {
            await unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<CreateAnswerDto> CreateAnswer(CreateAnswerDto answerDto)
    {
        try
        {
            var answer = mapper.Map<ProductAnswer>(answerDto);

            await unitOfWork.BeginTransactionAsync();

            var insertedAnswer = await answerRepository.InsertAsync(answer);

            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitTransactionAsync();

            return mapper.Map<CreateAnswerDto>(insertedAnswer);
        }
        catch (Exception)
        {
            await unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<bool> DeleteQuestion(Guid questionId)
    {
        var question = await questionRepository.GetQuestionByIdAsync(questionId);
        
        if (question == null)
            throw new KeyNotFoundException($"Review {questionId} not found!");
        
        questionRepository.Delete(question);

        var result = await questionRepository.SaveChangesAsync();
        return result > 0;
    }

    public async Task<bool> DeleteAnswer(Guid answerId)
    {
        var answer = await answerRepository.GetAnswerByIdAsync(answerId);
        
        if (answer == null)
            throw new KeyNotFoundException($"Review {answerId} not found!");
        
        answerRepository.Delete(answer);

        var result = await answerRepository.SaveChangesAsync();
        return result > 0;
    }

    public async Task<bool> UpdateQuestion(UpdateQuestionDto questionDto)
    {
        var question = await questionRepository.GetQuestionByIdAsync(questionDto.QuestionId);
        if (question == null)
            throw new KeyNotFoundException($"Question {questionDto.QuestionId} not found!");


        if (questionDto.IsAnonymous.HasValue)
            question.IsAnonymous = questionDto.IsAnonymous.Value;

        if (questionDto.Text != null)
            question.Text = questionDto.Text;

        questionRepository.Update(question);
        var result = await questionRepository.SaveChangesAsync();

        return result > 0;
    }

    public async Task<bool> UpdateAnswer(UpdateAnswerDto answerDto)
    {
        var answer = await answerRepository.GetAnswerByIdAsync(answerDto.AnswerId);
        if (answer == null)
            throw new KeyNotFoundException($"Answer {answerDto.AnswerId} not found!");
         
        answer.Text = answerDto.Text;

        answerRepository.Update(answer);
        var result = await answerRepository.SaveChangesAsync();

        return result > 0;
    }

    public async Task<IEnumerable<ProductQuestion>> GetProductQuestionsAndAnswers(Guid productId, int page, int pageSize)
    {
        var result = await questionRepository.GetQuestionsAndAnswersByProductIdAsync(productId, page, pageSize);
        return result;
    }
}
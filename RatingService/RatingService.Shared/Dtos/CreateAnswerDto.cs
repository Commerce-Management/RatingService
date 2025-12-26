namespace RatingService.Shared.Dtos;

public record CreateAnswerDto(Guid QuestionId, Guid ShopId, string Text);

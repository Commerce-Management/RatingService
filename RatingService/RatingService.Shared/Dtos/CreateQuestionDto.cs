namespace RatingService.Shared.Dtos;

public record CreateQuestionDto(
    Guid ProductId,
    Guid UserId,
    string Text, 
    bool IsAnonymous);
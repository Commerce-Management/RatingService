namespace RatingService.Shared.Dtos;

public record UpdateAnswerDto(
    Guid AnswerId,
    Guid ShopId,
    string Text    
    );
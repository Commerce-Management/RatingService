namespace RatingService.Shared.Dtos;

public record UpdateQuestionDto(
    Guid QuestionId,
    Guid UserId,
    string Text,
    bool? IsAnonymous
    );

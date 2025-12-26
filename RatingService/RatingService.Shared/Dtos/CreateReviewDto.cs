using Microsoft.AspNetCore.Http;
using RatingService.Shared.Dtos.Validation;

namespace RatingService.Shared.Dtos;

public record CreateReviewDto(
    Guid ProductId,
    Guid UserId,
    int Rating, 
    string? Title, 
    string? Text, 
    
    [AllowedExtensions([".jpg", ".png"])]
    [MaxFileSize(10 * 1024 * 1024)]
    IFormFile[]? Images,
    
    bool IsAnonymous);
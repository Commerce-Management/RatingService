using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using RatingService.Shared.Dtos.Validation;

namespace RatingService.Shared.Dtos;

public record UpdateReviewDto(
    [Required]
    Guid ReviewId,
    [Required]
    Guid UserId,
    
    int? Rating,
    string? Title,
    string? Text,
    
    IFormFile[]? NewImages,
    List<string>? RemoveImageUrls,
    
    bool? IsAnonymous);
using Microsoft.AspNetCore.Http;

namespace RatingService.Core.Interfaces;

public interface IReviewImageService
{
    public Task<string> UploadImageAsync(IFormFile imageData);
    public Task<bool> DeleteImageAsync(string imageUrl);
}
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using RatingService.Core.Interfaces;
﻿using Imagekit.Sdk;
using RatingService.Shared.Dtos;

namespace RatingService.Application.Services;

public class ReviewImageService(IConfiguration configuration) : IReviewImageService
{
    private readonly ImagekitClient _imagekitClient = new (configuration["ImageKit:PublicKey"], configuration["ImageKit:PrivateKey"], configuration["ImageKit:UrlEndPoint"]);
    private readonly string _privateKey = configuration["ImageKit:PrivateKey"]!;

    public async Task<string> UploadImageAsync(IFormFile imageData)
    {
        if (imageData == null || imageData.Length == 0)
            throw new ArgumentException("Image data is null or empty.", nameof(imageData));

        using var memoryStream = new MemoryStream();
        await imageData.CopyToAsync(memoryStream);
        var fileBytes = memoryStream.ToArray();

        var ob = new FileCreateRequest
        {
            file = fileBytes,
            fileName = Guid.NewGuid().ToString(),
            folder = "/reviews"
        };
        
        var result = await _imagekitClient.UploadAsync(ob);
        return result.url;
    }
    

    public async Task<bool> DeleteImageAsync(string imageUrl)
    {
        var uri = new Uri(imageUrl);

        // имя файла без папки
        var fileName = Path.GetFileName(uri.AbsolutePath);

        var auth = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{_privateKey}:"));

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", auth);

        var searchResponse = await client.PostAsJsonAsync(
            "https://api.imagekit.io/v1/files/search",
            new
            {
                searchQuery = $"name = \"{fileName}\""
            });

        if (!searchResponse.IsSuccessStatusCode)
            return false;

        var files = await searchResponse.Content
            .ReadFromJsonAsync<List<ImageKitSearchResult>>();

        var file = files?.SingleOrDefault();
        if (file == null)
            return false;

        var deleteResponse = await client.DeleteAsync(
            $"https://api.imagekit.io/v1/files/{file.fileId}");

        return deleteResponse.IsSuccessStatusCode;
    }




    
}
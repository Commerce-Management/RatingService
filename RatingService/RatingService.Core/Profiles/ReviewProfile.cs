using AutoMapper;
using RatingService.Core.Entities;
using RatingService.Shared.Dtos;

namespace RatingService.Core.Profiles;

public class ReviewProfile : Profile
{
    public ReviewProfile()
    {
        CreateMap<CreateReviewDto, ProductReview>();
        CreateMap<UpdateReviewDto, ProductReview>();
    }
}
using AutoMapper;
using RatingService.Core.Entities;
using RatingService.Shared.Dtos;

namespace RatingService.Core.Profiles;

public class QuestionsAndAnswersProfile : Profile
{
    public QuestionsAndAnswersProfile()
    {
        CreateMap<CreateQuestionDto, ProductQuestion>()
            .ForMember(dest => dest.Answers, opt => opt.Ignore())
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.NewGuid()))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));
        CreateMap<CreateAnswerDto, ProductAnswer>();
        CreateMap<UpdateQuestionDto, ProductQuestion>();
        CreateMap<UpdateAnswerDto, ProductAnswer>();
    }
}
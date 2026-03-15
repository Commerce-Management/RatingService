FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 5008

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["RatingService.Api/RatingService.Api.csproj", "RatingService.Api/"]
COPY ["RatingService.Core/RatingService.Core.csproj", "RatingService.Core/"]
COPY ["RatingService.Shared/RatingService.Shared.csproj", "RatingService.Shared/"]
COPY ["RatingService.Application/RatingService.Application.csproj", "RatingService.Application/"]
COPY ["RatingService.Infrastructure/RatingService.Infrastructure.csproj", "RatingService.Infrastructure/"]
RUN dotnet restore "RatingService.Api/RatingService.Api.csproj"
COPY . .
WORKDIR "/src/RatingService.Api"
RUN dotnet build "RatingService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "RatingService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RatingService.Api.dll"]

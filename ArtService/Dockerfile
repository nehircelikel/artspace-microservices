FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["ArtService.API/ArtService.API.csproj", "ArtService.API/"]
COPY ["ArtService.Core/ArtService.Core.csproj", "ArtService.Core/"]
COPY ["ArtService.Infrastructure/ArtService.Infrastructure.csproj", "ArtService.Infrastructure/"]
RUN dotnet restore "ArtService.API/ArtService.API.csproj"
COPY . .
WORKDIR "/src/ArtService.API"
RUN dotnet build "ArtService.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ArtService.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ArtService.API.dll"]
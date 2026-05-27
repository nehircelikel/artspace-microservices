FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["CommentService.API/CommentService.API.csproj", "CommentService.API/"]
COPY ["CommentService.Core/CommentService.Core.csproj", "CommentService.Core/"]
COPY ["CommentService.Infrastructure/CommentService.Infrastructure.csproj", "CommentService.Infrastructure/"]
RUN dotnet restore "CommentService.API/CommentService.API.csproj"
COPY . .
WORKDIR "/src/CommentService.API"
RUN dotnet build "CommentService.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CommentService.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CommentService.API.dll"]
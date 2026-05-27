FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["artspace.API/artspace.API.csproj", "artspace.API/"]
COPY ["artspace.Core/artspace.Core.csproj", "artspace.Core/"]
COPY ["artspace.Infrastructure/artspace.Infrastructure.csproj", "artspace.Infrastructure/"]
RUN dotnet restore "artspace.API/artspace.API.csproj"
COPY . .
WORKDIR "/src/artspace.API"
RUN dotnet build "artspace.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "artspace.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "artspace.API.dll"]
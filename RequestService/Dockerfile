FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["RequestService.API/RequestService.API.csproj", "RequestService.API/"]
COPY ["RequestService.Core/RequestService.Core.csproj", "RequestService.Core/"]
COPY ["RequestService.Infrastructure/RequestService.Infrastructure.csproj", "RequestService.Infrastructure/"]
RUN dotnet restore "RequestService.API/RequestService.API.csproj"
COPY . .
WORKDIR "/src/RequestService.API"
RUN dotnet build "RequestService.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RequestService.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RequestService.API.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["CommissionService.API/CommissionService.API.csproj", "CommissionService.API/"]
COPY ["CommissionService.Core/CommissionService.Core.csproj", "CommissionService.Core/"]
COPY ["CommissionService.Infrastructure/CommissionService.Infrastructure.csproj", "CommissionService.Infrastructure/"]
RUN dotnet restore "CommissionService.API/CommissionService.API.csproj"
COPY . .
WORKDIR "/src/CommissionService.API"
RUN dotnet build "CommissionService.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CommissionService.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CommissionService.API.dll"]

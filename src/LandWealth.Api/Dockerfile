# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy solution and project files for restore caching
COPY LandWealth.sln nuget.config ./
COPY src/LandWealth.Domain/LandWealth.Domain.csproj src/LandWealth.Domain/
COPY src/LandWealth.Application/LandWealth.Application.csproj src/LandWealth.Application/
COPY src/LandWealth.Infrastructure/LandWealth.Infrastructure.csproj src/LandWealth.Infrastructure/
COPY src/LandWealth.Api/LandWealth.Api.csproj src/LandWealth.Api/
COPY tests/LandWealth.UnitTests/LandWealth.UnitTests.csproj tests/LandWealth.UnitTests/
COPY tests/LandWealth.IntegrationTests/LandWealth.IntegrationTests.csproj tests/LandWealth.IntegrationTests/

RUN dotnet restore src/LandWealth.Api/LandWealth.Api.csproj

# Copy source code and build
COPY src/LandWealth.Domain/ src/LandWealth.Domain/
COPY src/LandWealth.Application/ src/LandWealth.Application/
COPY src/LandWealth.Infrastructure/ src/LandWealth.Infrastructure/
COPY src/LandWealth.Api/ src/LandWealth.Api/

WORKDIR /app/src/LandWealth.Api
RUN dotnet publish -c Release -o /out

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /out ./
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "LandWealth.Api.dll"]

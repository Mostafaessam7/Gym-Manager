FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY src/BuildingBlocks/GymManager.SharedKernel/GymManager.SharedKernel.csproj src/BuildingBlocks/GymManager.SharedKernel/
COPY src/Core/GymManager.Domain/GymManager.Domain.csproj src/Core/GymManager.Domain/
COPY src/Core/GymManager.Application/GymManager.Application.csproj src/Core/GymManager.Application/
COPY src/Infrastructure/GymManager.Infrastructure/GymManager.Infrastructure.csproj src/Infrastructure/GymManager.Infrastructure/
COPY src/Presentation/GymManager.Api/GymManager.Api.csproj src/Presentation/GymManager.Api/
RUN dotnet restore src/Presentation/GymManager.Api/GymManager.Api.csproj

COPY src/ src/
RUN dotnet publish src/Presentation/GymManager.Api/GymManager.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app .
RUN chown -R app:app /app
USER app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD curl --fail --silent http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "GymManager.Api.dll"]

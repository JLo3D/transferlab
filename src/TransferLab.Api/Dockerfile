# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /source

COPY ["src/TransferLab.Api/TransferLab.Api.csproj", "src/TransferLab.Api/"]
COPY ["src/TransferLab.Application/TransferLab.Application.csproj", "src/TransferLab.Application/"]
COPY ["src/TransferLab.Domain/TransferLab.Domain.csproj", "src/TransferLab.Domain/"]
COPY ["src/TransferLab.Infrastructure/TransferLab.Infrastructure.csproj", "src/TransferLab.Infrastructure/"]
RUN dotnet restore "src/TransferLab.Api/TransferLab.Api.csproj"

COPY . .
RUN dotnet publish "src/TransferLab.Api/TransferLab.Api.csproj" \
    --configuration "$BUILD_CONFIGURATION" \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

USER root
RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
    DOTNET_EnableDiagnostics=0

EXPOSE 8080
COPY --from=build /app/publish .

USER $APP_UID
HEALTHCHECK --interval=10s --timeout=3s --start-period=10s --retries=5 \
    CMD curl --fail --silent http://127.0.0.1:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "TransferLab.Api.dll"]

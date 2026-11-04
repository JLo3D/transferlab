# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /source

COPY ["src/TransferLab.Worker/TransferLab.Worker.csproj", "src/TransferLab.Worker/"]
COPY ["src/TransferLab.Application/TransferLab.Application.csproj", "src/TransferLab.Application/"]
COPY ["src/TransferLab.Domain/TransferLab.Domain.csproj", "src/TransferLab.Domain/"]
COPY ["src/TransferLab.Infrastructure/TransferLab.Infrastructure.csproj", "src/TransferLab.Infrastructure/"]
RUN dotnet restore "src/TransferLab.Worker/TransferLab.Worker.csproj"

COPY . .
RUN dotnet publish "src/TransferLab.Worker/TransferLab.Worker.csproj" \
    --configuration "$BUILD_CONFIGURATION" \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app

ENV DOTNET_EnableDiagnostics=0

COPY --from=build /app/publish .

USER $APP_UID
ENTRYPOINT ["dotnet", "TransferLab.Worker.dll"]

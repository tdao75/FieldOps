FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY . .

RUN dotnet restore \
    "src/FieldOps.Notification.Worker/FieldOps.Notification.Worker.csproj"

WORKDIR "/src/src/FieldOps.Notification.Worker"

RUN dotnet publish \
    "FieldOps.Notification.Worker.csproj" \
    --configuration $BUILD_CONFIGURATION \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FieldOps.Notification.Worker.dll"]
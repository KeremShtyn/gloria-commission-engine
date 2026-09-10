# Derleme, calisma imajindan ayrilir; uretim imajinda SDK ve kaynak kod bulunmaz.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Once yalnizca proje dosyalari kopyalanir: kaynak degistiginde restore katmani yeniden kullanilir.
COPY global.json ./
COPY src/Gloria.Commission.Domain/*.csproj src/Gloria.Commission.Domain/
COPY src/Gloria.Commission.Application/*.csproj src/Gloria.Commission.Application/
COPY src/Gloria.Commission.Infrastructure/*.csproj src/Gloria.Commission.Infrastructure/
COPY src/Gloria.Commission.Api/*.csproj src/Gloria.Commission.Api/
RUN dotnet restore src/Gloria.Commission.Api/Gloria.Commission.Api.csproj

COPY src/ src/
RUN dotnet publish src/Gloria.Commission.Api/Gloria.Commission.Api.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Root olmayan kullanici: konteyner ele gecirilse bile dosya sistemi yazilabilir degil.
RUN useradd --uid 1001 --create-home appuser \
    && mkdir -p /app/data /app/sample-data \
    && chown -R appuser:appuser /app

COPY --from=build --chown=appuser:appuser /app/publish ./
COPY --chown=appuser:appuser sample-data/ ./sample-data/

USER appuser

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    ConnectionStrings__Default="Data Source=/app/data/gloria-commission.db"

EXPOSE 8080

HEALTHCHECK --interval=15s --timeout=3s --start-period=20s --retries=3 \
    CMD ["dotnet", "Gloria.Commission.Api.dll", "--healthcheck"]

ENTRYPOINT ["dotnet", "Gloria.Commission.Api.dll"]

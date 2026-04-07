FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/DataIngestorService.Host/DataIngestorService.Host.csproj", "src/DataIngestorService.Host/"]
COPY ["src/DataIngestorService.Infrastructure/DataIngestorService.Infrastructure.csproj", "src/DataIngestorService.Infrastructure/"]
COPY ["src/DataIngestorService.Core/DataIngestorService.Core.csproj", "src/DataIngestorService.Core/"]
RUN dotnet restore "src/DataIngestorService.Host/DataIngestorService.Host.csproj"
COPY . .
WORKDIR "/src/src/DataIngestorService.Host"
RUN dotnet build "DataIngestorService.Host.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DataIngestorService.Host.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

RUN useradd -m --no-log-init appuser && \
    chown -R appuser:appuser /app
USER appuser

COPY --from=publish /app/publish .

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "DataIngestorService.Host.dll"]
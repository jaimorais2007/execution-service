# Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restaura primeiro só com os .csproj para aproveitar o cache de camadas
COPY src/ExecutionService.Api/ExecutionService.Api.csproj src/ExecutionService.Api/
RUN dotnet restore src/ExecutionService.Api/ExecutionService.Api.csproj

COPY src/ src/
RUN dotnet publish src/ExecutionService.Api/ExecutionService.Api.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
USER app
ENTRYPOINT ["dotnet", "ExecutionService.Api.dll"]

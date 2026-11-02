# Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restaura as dependencias copiando primeiro apenas os .csproj
COPY src/ExecutionService.Domain/ExecutionService.Domain.csproj src/ExecutionService.Domain/
COPY src/ExecutionService.Application/ExecutionService.Application.csproj src/ExecutionService.Application/
COPY src/ExecutionService.Infrastructure/ExecutionService.Infrastructure.csproj src/ExecutionService.Infrastructure/
COPY src/ExecutionService.Api/ExecutionService.Api.csproj src/ExecutionService.Api/

RUN dotnet restore src/ExecutionService.Api/ExecutionService.Api.csproj

# Copia o codigo-fonte completo e publica
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

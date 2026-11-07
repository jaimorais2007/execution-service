# Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restaura as dependencias copiando primeiro apenas os .csproj
COPY src/RegistryService.Domain/RegistryService.Domain.csproj src/RegistryService.Domain/
COPY src/RegistryService.Application/RegistryService.Application.csproj src/RegistryService.Application/
COPY src/RegistryService.Infrastructure/RegistryService.Infrastructure.csproj src/RegistryService.Infrastructure/
COPY src/RegistryService.Api/RegistryService.Api.csproj src/RegistryService.Api/

RUN dotnet restore src/RegistryService.Api/RegistryService.Api.csproj

# Copia o codigo-fonte completo e publica
COPY src/ src/
RUN dotnet publish src/RegistryService.Api/RegistryService.Api.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
USER app
ENTRYPOINT ["dotnet", "RegistryService.Api.dll"]

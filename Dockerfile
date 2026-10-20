# syntax=docker/dockerfile:1

# ============ ETAPA 1: BUILD ============
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia todo el código fuente
COPY . .

# Compila y publica la API en la carpeta /app/publish
RUN dotnet publish Dsw2025Tpi.Api/Dsw2025Tpi.Api.csproj -c Release -o /app/publish

# ============ ETAPA 2: FINAL (RUNTIME) ============
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Variables de entorno requeridas por la guía
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Exponer el puerto
EXPOSE 8080

# Copia solo los binarios compilados de la etapa build
COPY --from=build /app/publish .

# Comando de inicio
ENTRYPOINT ["dotnet", "Dsw2025Tpi.Api.dll"]
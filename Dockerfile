# Etapa 1: Compilación
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiamos la solución y los proyectos para restaurar dependencias
COPY ["Dsw2025Tpi.sln", "./"]
COPY ["Dsw2025Tpi.Api/Dsw2025Tpi.Api.csproj", "Dsw2025Tpi.Api/"]
COPY ["Dsw2025Tpi.Application/Dsw2025Tpi.Application.csproj", "Dsw2025Tpi.Application/"]
COPY ["Dsw2025Tpi.Data/Dsw2025Tpi.Data.csproj", "Dsw2025Tpi.Data/"]
COPY ["Dsw2025Tpi.Domain/Dsw2025Tpi.Domain.csproj", "Dsw2025Tpi.Domain/"]
COPY ["test/Dsw2025Tpi.Test.csproj", "test/"] 

RUN dotnet restore "Dsw2025Tpi.sln"

# Copiamos el resto del código fuente
COPY . .
WORKDIR "/src/Dsw2025Tpi.Api"
RUN dotnet publish "Dsw2025Tpi.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Etapa 2: Imagen final para ejecución
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Dsw2025Tpi.Dll"]

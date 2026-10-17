FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Dsw2025Tpi.sln", "./"]
COPY ["Dsw2025Tpi.Api/Dsw2025Tpi.Api.csproj", "Dsw2025Tpi.Api/"]
COPY ["Dsw2025Tpi.Application/Dsw2025Tpi.Application.csproj", "Dsw2025Tpi.Application/"]
COPY ["Dsw2025Tpi.Domain/Dsw2025Tpi.Domain.csproj", "Dsw2025Tpi.Domain/"]
COPY ["Dsw2025Tpi.Data/Dsw2025Tpi.Data.csproj", "Dsw2025Tpi.Data/"]
RUN dotnet restore "Dsw2025Tpi.sln"
COPY . .
WORKDIR "/src/Dsw2025Tpi.Api"
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Dsw2025Tpi.Api.dll"]

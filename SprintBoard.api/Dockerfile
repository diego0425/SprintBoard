# ============================================================
# BUILD
# ============================================================

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Copy project files first to improve Docker layer caching.
COPY ["SprintBoard.api/SprintBoard.api.csproj", "SprintBoard.api/"]
COPY ["SprintBoard.Application/SprintBoard.Application.csproj", "SprintBoard.Application/"]
COPY ["SprintBoard.Domain/SprintBoard.Domain.csproj", "SprintBoard.Domain/"]
COPY ["SprintBoard.Infrastructure/SprintBoard.Infrastructure.csproj", "SprintBoard.Infrastructure/"]

# Restore NuGet packages.
RUN dotnet restore "SprintBoard.api/SprintBoard.api.csproj"

# Copy the remaining source code.
COPY . .

WORKDIR "/src/SprintBoard.api"

# Publish the API.
RUN dotnet publish "SprintBoard.api.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# ============================================================
# RUNTIME
# ============================================================

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "SprintBoard.api.dll"]
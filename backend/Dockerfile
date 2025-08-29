# syntax=docker/dockerfile:1

# ----- Build stage -----
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY backend/*.csproj ./
RUN dotnet restore ./backend.csproj

# Copy everything and build
COPY backend/. ./
RUN dotnet publish ./backend.csproj -c Release -o /app/publish /p:UseAppHost=false

# ----- Runtime stage -----
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Non-root user
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser:appuser /app
USER appuser

# Copy published output
COPY --from=build /app/publish .

# Expose HTTP
EXPOSE 8080

# Prefer port 8080
ENV ASPNETCORE_URLS=http://+:8080

# Default to Production; override as needed
ENV ASPNETCORE_ENVIRONMENT=Production

# Logs directory (optional volume)
VOLUME [ "/app/logs" ]

ENTRYPOINT ["dotnet", "backend.dll"]

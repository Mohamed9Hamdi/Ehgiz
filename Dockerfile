# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Ehgiz.API/Ehgiz.API.csproj Ehgiz.API/
COPY Ehgiz.Application/Ehgiz.Application.csproj Ehgiz.Application/
COPY Ehgiz.DAL/Ehgiz.DAL.csproj Ehgiz.DAL/
RUN dotnet restore Ehgiz.API/Ehgiz.API.csproj

COPY . .
RUN dotnet publish Ehgiz.API/Ehgiz.API.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Render injects PORT at runtime; default 8080 for local docker run -p 8080:8080
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=40s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Ehgiz.API.dll"]

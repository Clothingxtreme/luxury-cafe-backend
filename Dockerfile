# ── Stage 1: Build ────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["MaisonGlace.API.csproj", "."]
RUN dotnet restore "MaisonGlace.API.csproj"

COPY . .
RUN dotnet publish "MaisonGlace.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ── Stage 2: Runtime ───────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

RUN apt-get update \
	&& apt-get install -y --no-install-recommends ca-certificates openssl \
	&& update-ca-certificates \
	&& rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Render injects PORT at runtime; default 8080 for local testing
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "MaisonGlace.API.dll"]

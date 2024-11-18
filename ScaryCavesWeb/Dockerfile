FROM mcr.microsoft.com/dotnet/sdk:8.0 AS base
WORKDIR /app
EXPOSE 8000

# Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["./ScaryCavesWeb.csproj", "./ScaryCavesWeb/"]
RUN dotnet restore ScaryCavesWeb/ScaryCavesWeb.csproj

COPY . ./ScaryCavesWeb
RUN dotnet publish "ScaryCavesWeb/ScaryCavesWeb.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ScaryCavesWeb.dll"]

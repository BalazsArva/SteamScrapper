#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:5.0-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build
WORKDIR /src
COPY ["SteamScrapper.SubScanner/SteamScrapper.SubScanner.csproj", "SteamScrapper.SubScanner/"]
RUN dotnet restore "SteamScrapper.SubScanner/SteamScrapper.SubScanner.csproj"
COPY . .
WORKDIR "/src/SteamScrapper.SubScanner"
RUN dotnet build "SteamScrapper.SubScanner.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SteamScrapper.SubScanner.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SteamScrapper.SubScanner.dll"]
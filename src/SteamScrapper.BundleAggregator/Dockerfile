#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["SteamScrapper.BundleAggregator/SteamScrapper.BundleAggregator.csproj", "SteamScrapper.BundleAggregator/"]
RUN dotnet restore "SteamScrapper.BundleAggregator/SteamScrapper.BundleAggregator.csproj"
COPY . .
WORKDIR "/src/SteamScrapper.BundleAggregator"
RUN dotnet build "SteamScrapper.BundleAggregator.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SteamScrapper.BundleAggregator.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SteamScrapper.BundleAggregator.dll"]
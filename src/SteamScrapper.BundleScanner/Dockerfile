#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["SteamScrapper.BundleScanner/SteamScrapper.BundleScanner.csproj", "SteamScrapper.BundleScanner/"]
RUN dotnet restore "SteamScrapper.BundleScanner/SteamScrapper.BundleScanner.csproj"
COPY . .
WORKDIR "/src/SteamScrapper.BundleScanner"
RUN dotnet build "SteamScrapper.BundleScanner.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SteamScrapper.BundleScanner.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SteamScrapper.BundleScanner.dll"]
#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0.2-focal AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0.102-1-focal AS build
WORKDIR /src
COPY . .
WORKDIR /src/src/server/FeedReader.WebServer
RUN dotnet build "FeedReader.WebServer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FeedReader.WebServer.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FeedReader.WebServer.dll"]

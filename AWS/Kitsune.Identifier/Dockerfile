#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:3.1 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build
WORKDIR /src
COPY ["AWS/Kitsune.Identifier/nuget.config", "AWS/Kitsune.Identifier/"]
COPY ["AWS/nuget.config", "AWS/"]
COPY ["AWS/Kitsune.Identifier/Kitsune.Identifier.csproj", "AWS/Kitsune.Identifier/"]
COPY ["Common/FloatingpointLayoutManager/FloatingpointLayoutManager.csproj", "Common/FloatingpointLayoutManager/"]
COPY nuget.config ./
RUN dotnet restore "AWS/Kitsune.Identifier/Kitsune.Identifier.csproj" -s https://nuget.withfloats.com/api/v2 -s https://api.nuget.org/v3/index.json -s https://nuget.pkg.github.com/nowfloats/index.json  --configfile nuget.config
COPY . .
WORKDIR "/src/AWS/Kitsune.Identifier"
RUN dotnet build "Kitsune.Identifier.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Kitsune.Identifier.csproj" -c Release -o /app/publish --source https://nuget.withfloats.com/api/v2 --source https://api.nuget.org/v3/index.json --source https://nuget.pkg.github.com/nowfloats/index.json

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Kitsune.Identifier.dll"]
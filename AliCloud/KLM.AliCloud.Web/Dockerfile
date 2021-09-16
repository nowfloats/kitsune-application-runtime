#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:3.1 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["AliCloud/KLM.AliCloud.Web/KLM.Web.csproj", "AliCloud/KLM.AliCloud.Web/"]
COPY ["AWS/Kitsune.Identifier/Kitsune.Identifier.csproj", "AWS/Kitsune.Identifier/"]
COPY ["Common/FloatingpointLayoutManager/FloatingpointLayoutManager.csproj", "Common/FloatingpointLayoutManager/"]
RUN dotnet restore "AliCloud/KLM.AliCloud.Web/KLM.Web.csproj"
COPY . .
WORKDIR "/src/AliCloud/KLM.AliCloud.Web"
RUN dotnet build "KLM.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "KLM.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "KLM.Web.dll"]
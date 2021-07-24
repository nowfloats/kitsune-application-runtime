FROM mcr.microsoft.com/dotnet/runtime:3.1
WORKDIR /app
EXPOSE 80
COPY "bin/Release/netcoreapp3.1/publish" .
ENTRYPOINT ["dotnet", "Kitsune.Identifier.dll"]
FROM microsoft/dotnet:2.1.0-aspnetcore-runtime
WORKDIR /app
EXPOSE 80
COPY "bin/Release/netcoreapp2.1/publish" .
ENTRYPOINT ["dotnet", "Kitsune.Identifier.dll"]
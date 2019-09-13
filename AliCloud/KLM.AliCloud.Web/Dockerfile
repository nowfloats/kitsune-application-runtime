FROM microsoft/dotnet:2.1-aspnetcore-runtime
WORKDIR /app
COPY "bin/Release/PublishOutput" .
ENTRYPOINT ["dotnet", "KLM.Web.dll"]
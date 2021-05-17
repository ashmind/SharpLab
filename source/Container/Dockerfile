FROM mcr.microsoft.com/dotnet/aspnet:5.0
EXPOSE 80
WORKDIR /app
COPY /app/publish .
ENTRYPOINT ["dotnet", "SharpLab.Container.dll"]
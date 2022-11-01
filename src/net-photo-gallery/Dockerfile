FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine3.16

WORKDIR /app
COPY publish/ /app/

ENTRYPOINT ["dotnet", "net-photo-gallery.dll"]
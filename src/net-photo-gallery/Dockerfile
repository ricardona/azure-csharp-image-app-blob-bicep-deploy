FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine3.18

EXPOSE 80
ENV ASPNETCORE_URLS=http://+:80

WORKDIR /app
COPY publish/ /app/

CMD ["dotnet", "net-photo-gallery.dll"]
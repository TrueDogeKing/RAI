# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY TransitDesk.Core/TransitDesk.Core.csproj TransitDesk.Core/
COPY TransitDesk.Web/TransitDesk.Web.csproj TransitDesk.Web/
RUN dotnet restore TransitDesk.Web/TransitDesk.Web.csproj

COPY TransitDesk.Core/ TransitDesk.Core/
COPY TransitDesk.Web/ TransitDesk.Web/
RUN dotnet publish TransitDesk.Web/TransitDesk.Web.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    TZ=Europe/Warsaw

RUN mkdir -p /app/App_Data/Uploads && chown -R $APP_UID:$APP_UID /app/App_Data

COPY --from=build /app/publish .

USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "TransitDesk.Web.dll"]

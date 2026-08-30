FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Packages.props .
COPY PillAppBackend/PillApp.Api/PillApp.Api.csproj PillAppBackend/PillApp.Api/
RUN dotnet restore PillAppBackend/PillApp.Api/PillApp.Api.csproj

COPY . .
WORKDIR /src/PillAppBackend/PillApp.Api
RUN dotnet publish PillApp.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["sh", "-c", "dotnet PillApp.Api.dll --urls http://0.0.0.0:${PORT:-8080}"]

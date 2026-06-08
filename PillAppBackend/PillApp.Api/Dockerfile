FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY PillApp.Api.csproj ./
RUN dotnet restore PillApp.Api.csproj

COPY . .
RUN dotnet publish PillApp.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["sh", "-c", "dotnet PillApp.Api.dll --urls http://0.0.0.0:${PORT:-8080}"]
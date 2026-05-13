FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ClinicManagement.sln ./
COPY ClinicManagement/ClinicManagement.csproj ClinicManagement/
RUN dotnet restore ClinicManagement/ClinicManagement.csproj

COPY ClinicManagement/ ClinicManagement/
RUN dotnet publish ClinicManagement/ClinicManagement.csproj \
    --configuration Release \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ClinicManagement.dll"]

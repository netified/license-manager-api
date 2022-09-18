#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["src/LicenseManager.Api.Service/LicenseManager.Api.Service.csproj", "src/LicenseManager.Api.Service/"]
COPY ["src/LicenseManager.Api.Domain/LicenseManager.Api.Domain.csproj", "src/LicenseManager.Api.Domain/"]
COPY ["src/LicenseManager.Api.Data.SQLServer/LicenseManager.Api.Data.SQLServer.csproj", "src/LicenseManager.Api.Data.SQLServer/"]
COPY ["src/LicenseManager.Api.Data/LicenseManager.Api.Data.csproj", "src/LicenseManager.Api.Data/"]
COPY ["src/LicenseManager.Api.Data.Shared/LicenseManager.Api.Data.Shared.csproj", "src/LicenseManager.Api.Data.Shared/"]
COPY ["src/LicenseManager.Api.Data.Configuration/LicenseManager.Api.Data.Configuration.csproj", "src/LicenseManager.Api.Data.Configuration/"]
COPY ["src/LicenseManager.Api.Configuration/LicenseManager.Api.Configuration.csproj", "src/LicenseManager.Api.Configuration/"]
COPY ["src/LicenseManager.Api.Data.PostgreSQL/LicenseManager.Api.Data.PostgreSQL.csproj", "src/LicenseManager.Api.Data.PostgreSQL/"]
RUN dotnet restore "src/LicenseManager.Api.Service/LicenseManager.Api.Service.csproj"
COPY . .
WORKDIR "/src/src/LicenseManager.Api.Service"
RUN dotnet build "LicenseManager.Api.Service.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LicenseManager.Api.Service.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LicenseManager.Api.Service.dll"]
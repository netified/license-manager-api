#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Src/LicenseManager.Api.Service/LicenseManager.Api.Service.csproj", "Src/LicenseManager.Api.Service/"]
COPY ["Src/LicenseManager.Api.Domain/LicenseManager.Api.Domain.csproj", "Src/LicenseManager.Api.Domain/"]
COPY ["Src/LicenseManager.Api.Data.PostgreSQL/LicenseManager.Api.Data.PostgreSQL.csproj", "Src/LicenseManager.Api.Data.PostgreSQL/"]
COPY ["Src/LicenseManager.Api.Data/LicenseManager.Api.Data.csproj", "Src/LicenseManager.Api.Data/"]
COPY ["Src/LicenseManager.Api.Data.Shared/LicenseManager.Api.Data.Shared.csproj", "Src/LicenseManager.Api.Data.Shared/"]
COPY ["Src/LicenseManager.Api.Data.Configuration/LicenseManager.Api.Data.Configuration.csproj", "Src/LicenseManager.Api.Data.Configuration/"]
RUN dotnet restore "Src/LicenseManager.Api.Service/LicenseManager.Api.Service.csproj"
COPY . .
WORKDIR "/src/Src/LicenseManager.Api.Service"
RUN dotnet build "LicenseManager.Api.Service.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LicenseManager.Api.Service.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LicenseManager.Api.Service.dll"]
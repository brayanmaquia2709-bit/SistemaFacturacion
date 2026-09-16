# Dockerfile de Producción para SistemaFacturacion.API
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["SistemaFacturacion.API/SistemaFacturacion.API.csproj", "SistemaFacturacion.API/"]
COPY ["SistemaFacturacion.Core/SistemaFacturacion.Core.csproj", "SistemaFacturacion.Core/"]
RUN dotnet restore "SistemaFacturacion.API/SistemaFacturacion.API.csproj"
COPY . .
WORKDIR "/src/SistemaFacturacion.API"
RUN dotnet build "SistemaFacturacion.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SistemaFacturacion.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SistemaFacturacion.API.dll"]

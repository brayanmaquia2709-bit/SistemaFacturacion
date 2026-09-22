# Dockerfile de Producción para SistemaFacturacion.Blazor & API en Render / Koyeb / Railway / Fly.io
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["SistemaFacturacion.Blazor/SistemaFacturacion.Blazor.csproj", "SistemaFacturacion.Blazor/"]
COPY ["SistemaFacturacion.API/SistemaFacturacion.API.csproj", "SistemaFacturacion.API/"]
COPY ["SistemaFacturacion.Core/SistemaFacturacion.Core.csproj", "SistemaFacturacion.Core/"]
RUN dotnet restore "SistemaFacturacion.Blazor/SistemaFacturacion.Blazor.csproj"
COPY . .
WORKDIR "/src/SistemaFacturacion.Blazor"
RUN dotnet publish "SistemaFacturacion.Blazor.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SistemaFacturacion.Blazor.dll"]

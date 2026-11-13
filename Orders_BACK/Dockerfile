# Etapa 1: compilar con el SDK completo.
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# Primero solo los .csproj: la capa del restore queda en caché mientras no cambien las dependencias.
COPY CapaDTO/CapaDTO.csproj CapaDTO/
COPY CapaDatos/CapaDatos.csproj CapaDatos/
COPY CapaNegocios/CapaNegocios.csproj CapaNegocios/
COPY Orders_Api/Orders_Api.csproj Orders_Api/
RUN dotnet restore Orders_Api/Orders_Api.csproj

COPY . .
RUN dotnet publish Orders_Api/Orders_Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# Etapa 2: solo el runtime de ASP.NET sobre Alpine (imagen liviana, sin SDK ni código fuente).
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final
# SqlClient necesita ICU para las intercalaciones de SQL Server.
RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Orders_Api.dll"]

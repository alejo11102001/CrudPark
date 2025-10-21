# Etapa 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copia los archivos del proyecto
COPY . ./

# Restaura dependencias
RUN dotnet restore

# Compila la aplicación en modo Release
RUN dotnet publish -c Release -o out

# Etapa 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .

# Puerto que Render usará
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Variable de entorno para Render
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "backend_crudpark.dll"]

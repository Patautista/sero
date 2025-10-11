# Base image (runtime)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy csproj files for restore
COPY ["SupportServer/SupportServer.csproj", "SupportServer/"]
COPY ["Application/Business.csproj", "Application/"]
COPY ["Domain/Domain.csproj", "Domain/"]
COPY ["AnkiNet/AnkiNet.csproj", "AnkiNet/"]
COPY ["Infrastructure/Infrastructure.csproj", "Infrastructure/"]

# Copy embedded SQL files explicitly
COPY AnkiNet/CollectionFile/Database/sql/ AnkiNet/CollectionFile/Database/Sql/

# Restore dependencies
RUN dotnet restore "./SupportServer/SupportServer.csproj"

# Copy all source code
COPY . .

WORKDIR "/src/SupportServer"

# Build without warnings as errors and suppress nullable/namespace warnings
RUN dotnet build "./SupportServer.csproj" -c $BUILD_CONFIGURATION -o /app/build \
    -p:TreatWarningsAsErrors=false \
    -nowarn:CS1591,CS8618,CS8632,CS8602,CS0169,CS0649,CS0108

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./SupportServer.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SupportServer.dll"]

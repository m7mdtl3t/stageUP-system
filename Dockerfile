# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY ["VivuqeQRSystem.csproj", "./"]
RUN dotnet restore

# Copy everything and build
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install libgdiplus for QR code generation (System.Drawing)
RUN apt-get update && apt-get install -y libgdiplus libc6-dev && rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=build /app/publish .

# Create data directory for SQLite
RUN mkdir -p /data

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ConnectionStrings__DefaultConnection="Data Source=/data/vivuqe.db"

# Expose port
EXPOSE 8080

# Run the app
ENTRYPOINT ["dotnet", "VivuqeQRSystem.dll"]

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["UTC_DATN/UTC_DATN.csproj", "UTC_DATN/"]
RUN dotnet restore "UTC_DATN/UTC_DATN.csproj"

# Copy all source code
COPY . .

# Build the application
WORKDIR "/src/UTC_DATN"
RUN dotnet build "UTC_DATN.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "UTC_DATN.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Set environment for Render.com
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "UTC_DATN.dll"]

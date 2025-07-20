# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/services/Identity/SecureVault.Identity.Api/SecureVault.Identity.Api.csproj", "src/services/Identity/SecureVault.Identity.Api/"]
COPY ["src/services/Identity/SecureVault.Identity.Infrastructure/SecureVault.Identity.Infrastructure.csproj", "src/services/Identity/SecureVault.Identity.Infrastructure/"]
COPY ["src/services/Identity/SecureVault.Identity.Application/SecureVault.Identity.Application.csproj", "src/services/Identity/SecureVault.Identity.Application/"]
COPY ["src/shared/SecureVault.Shared.Dto/SecureVault.Shared.Dto.csproj", "src/shared/SecureVault.Shared.Dto/"]
COPY ["src/shared/SecureVault.Shared.Result/SecureVault.Shared.Result.csproj", "src/shared/SecureVault.Shared.Result/"]
COPY ["src/services/Identity/SecureVault.Identity.Domain/SecureVault.Identity.Domain.csproj", "src/services/Identity/SecureVault.Identity.Domain/"]
RUN dotnet restore "./src/services/Identity/SecureVault.Identity.Api/SecureVault.Identity.Api.csproj"
COPY . .
WORKDIR "/src/src/services/Identity/SecureVault.Identity.Api"
RUN dotnet build "./SecureVault.Identity.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./SecureVault.Identity.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SecureVault.Identity.Api.dll"]
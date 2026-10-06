# Build stage: restore and publish the API with the full .NET SDK.
FROM mcr.microsoft.com/dotnet/sdk:10.0.302 AS build
WORKDIR /src

# Copy only the project files first, so the restore layer stays cached until a dependency changes.
COPY VebTur.slnx ./
COPY src/VebTur.Api/VebTur.Api.csproj src/VebTur.Api/
COPY src/VebTur.Application/VebTur.Application.csproj src/VebTur.Application/
COPY src/VebTur.Domain/VebTur.Domain.csproj src/VebTur.Domain/
COPY src/VebTur.Infrastructure/VebTur.Infrastructure.csproj src/VebTur.Infrastructure/
RUN dotnet restore src/VebTur.Api/VebTur.Api.csproj

COPY src/ src/
RUN dotnet publish src/VebTur.Api/VebTur.Api.csproj --configuration Release --no-restore --output /app/publish

# Runtime stage: only the ASP.NET Core runtime and the published output, no SDK or source code.
FROM mcr.microsoft.com/dotnet/aspnet:10.0.10
WORKDIR /app
COPY --from=build /app/publish .

# The official image listens on port 8080 and ships a non-root "app" user.
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "VebTur.Api.dll"]

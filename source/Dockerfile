#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Builds the Vue 3 SPA independently of the .NET stages below — this
# stage has no FROM dependency on `build`, so Docker can build it in
# parallel. Output lands in wwwroot just before `dotnet publish` runs,
# so the Web SDK's automatic wwwroot->publish-output copy (built into
# Microsoft.NET.Sdk.Web, no .csproj changes needed) picks it up for free.
FROM node:22-alpine AS client-build
WORKDIR /client
COPY ["ArchitectureToolkit.Client/package.json", "ArchitectureToolkit.Client/package-lock.json", "./"]
RUN npm ci
COPY ["ArchitectureToolkit.Client/", "./"]
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ArchitectureToolkit.Presentation.API/ArchitectureToolkit.Presentation.API.csproj", "ArchitectureToolkit.Presentation.API/"]
COPY ["ArchitectureToolkit.Application/ArchitectureToolkit.Application.csproj", "ArchitectureToolkit.Application/"]
COPY ["ArchitectureToolkit.Domain/ArchitectureToolkit.Domain.csproj", "ArchitectureToolkit.Domain/"]
COPY ["ArchitectureToolkit.Infrastructure/ArchitectureToolkit.Infrastructure.csproj", "ArchitectureToolkit.Infrastructure/"]
COPY ["ArchitectureToolkit.Persistence/ArchitectureToolkit.Persistence.csproj", "ArchitectureToolkit.Persistence/"]
RUN dotnet restore "./ArchitectureToolkit.Presentation.API/ArchitectureToolkit.Presentation.API.csproj"
COPY . .
WORKDIR "/src/ArchitectureToolkit.Presentation.API"
RUN dotnet build "./ArchitectureToolkit.Presentation.API.csproj" -c "$BUILD_CONFIGURATION" -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
COPY --from=client-build /client/dist ./wwwroot
RUN dotnet publish "./ArchitectureToolkit.Presentation.API.csproj" -c "$BUILD_CONFIGURATION" -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ArchitectureToolkit.Presentation.API.dll"]
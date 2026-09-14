FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Beam.sln Directory.Packages.props ./
COPY src/Beam.Api/Beam.Api.csproj src/Beam.Api/
COPY src/Beam.Shared/Beam.Shared.csproj src/Beam.Shared/
COPY src/Beam.Infrastructure/Beam.Infrastructure.csproj src/Beam.Infrastructure/
COPY src/Beam.Modules/Beam.Modules.Identity/Beam.Modules.Identity.csproj src/Beam.Modules/Beam.Modules.Identity/
COPY src/Beam.Modules/Beam.Modules.Organizations/Beam.Modules.Organizations.csproj src/Beam.Modules/Beam.Modules.Organizations/
COPY src/Beam.Modules/Beam.Modules.Projects/Beam.Modules.Projects.csproj src/Beam.Modules/Beam.Modules.Projects/
COPY src/Beam.Modules/Beam.Modules.Scenes/Beam.Modules.Scenes.csproj src/Beam.Modules/Beam.Modules.Scenes/
COPY src/Beam.Modules/Beam.Modules.Elements/Beam.Modules.Elements.csproj src/Beam.Modules/Beam.Modules.Elements/
COPY src/Beam.Modules/Beam.Modules.Assets/Beam.Modules.Assets.csproj src/Beam.Modules/Beam.Modules.Assets/
RUN dotnet restore src/Beam.Api/Beam.Api.csproj

COPY src/ src/
RUN dotnet publish src/Beam.Api/Beam.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Beam.Api.dll"]

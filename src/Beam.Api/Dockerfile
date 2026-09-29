FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Beam.slnx Directory.Packages.props ./
COPY src/Beam.Api/Beam.Api.csproj src/Beam.Api/
COPY src/Beam.Shared/Beam.Shared.csproj src/Beam.Shared/
COPY src/Beam.Infrastructure/Beam.Infrastructure.csproj src/Beam.Infrastructure/
COPY src/Beam.Modules/Beam.Modules.Identity/Beam.Modules.Identity.csproj src/Beam.Modules/Beam.Modules.Identity/
COPY src/Beam.Modules/Beam.Modules.Organizations/Beam.Modules.Organizations.csproj src/Beam.Modules/Beam.Modules.Organizations/
COPY src/Beam.Modules/Beam.Modules.Projects/Beam.Modules.Projects.csproj src/Beam.Modules/Beam.Modules.Projects/
COPY src/Beam.Modules/Beam.Modules.Scenes/Beam.Modules.Scenes.csproj src/Beam.Modules/Beam.Modules.Scenes/
COPY src/Beam.Modules/Beam.Modules.Elements/Beam.Modules.Elements.csproj src/Beam.Modules/Beam.Modules.Elements/
COPY src/Beam.Modules/Beam.Modules.Assets/Beam.Modules.Assets.csproj src/Beam.Modules/Beam.Modules.Assets/
COPY src/Beam.Modules/Beam.Modules.Overlays/Beam.Modules.Overlays.csproj src/Beam.Modules/Beam.Modules.Overlays/
COPY src/Beam.Modules/Beam.Modules.Templates/Beam.Modules.Templates.csproj src/Beam.Modules/Beam.Modules.Templates/
COPY src/Beam.Modules/Beam.Modules.LiveControl/Beam.Modules.LiveControl.csproj src/Beam.Modules/Beam.Modules.LiveControl/
COPY src/Beam.Modules/Beam.Modules.StreamingDestinations/Beam.Modules.StreamingDestinations.csproj src/Beam.Modules/Beam.Modules.StreamingDestinations/
COPY src/Beam.Modules/Beam.Modules.AiGeneration/Beam.Modules.AiGeneration.csproj src/Beam.Modules/Beam.Modules.AiGeneration/
COPY src/Beam.Modules/Beam.Modules.Audit/Beam.Modules.Audit.csproj src/Beam.Modules/Beam.Modules.Audit/
RUN dotnet restore src/Beam.Api/Beam.Api.csproj

COPY src/ src/
RUN dotnet publish src/Beam.Api/Beam.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Beam.Api.dll"]

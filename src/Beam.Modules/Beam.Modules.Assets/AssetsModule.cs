using Beam.Modules.Assets.Abstractions;
using Beam.Modules.Assets.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Beam.Modules.Assets;

public static class AssetsModule
{
    public static IServiceCollection AddAssetsModule(this IServiceCollection services)
    {
        services.AddScoped<IAssetService, AssetService>();
        services.AddScoped<IAssetAccessService>(sp => (IAssetAccessService)sp.GetRequiredService<IAssetService>());
        return services;
    }
}

using Microsoft.Extensions.DependencyInjection;
using Unity.Services.CloudCode.Apis.Extensions;
using Unity.Services.CloudCode.Core;

namespace BasculePush
{
    /// <summary>Services donnés aux fonctions du module (une seule instance de chaque, réutilisée d'un appel à l'autre).</summary>
    public sealed class ModuleConfig : ICloudCodeSetup
    {
        public void Setup(ICloudCodeConfig config)
        {
            config.AddGameApiClient();
            config.Dependencies.AddSingleton<IApnsSender>(new ApnsSender());
        }
    }
}

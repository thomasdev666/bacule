using System;
using System.Threading.Tasks;
using Bascule.Core;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;

namespace BasculePush
{
    /// <summary>
    /// Fonction appelée par le jeu de l'invitant, juste après avoir écrit son invitation (GameApp.InviteFriendToDuel).
    /// Renvoie un mot qui dit ce qui s'est passé (« sent », « off », « quiet »…, voir <see cref="PushStatus"/>) : le jeu
    /// l'écrit dans la console et n'affiche rien au joueur. Le même mot est gardé dans les données protégées de
    /// l'invitant (push_status) pour le diagnostic dans le tableau de bord.
    /// </summary>
    public sealed class DuelPushModule
    {
        [CloudCodeFunction(DuelPush.Function)]
        public Task<string> DuelInvite(IExecutionContext context, IGameApiClient gameApi, IApnsSender apns, string targetId)
        {
            var store = new CloudSaveStore(context, gameApi);
            var secrets = new SecretManagerCredentials(context, gameApi);
            return new DuelPushService(store, secrets, apns).SendAsync(context.PlayerId, targetId, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        }
    }
}

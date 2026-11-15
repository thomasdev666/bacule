using System;
using System.Threading.Tasks;
using Bascule.Core;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;

namespace BasculePush
{
    /// <summary>
    /// Fonctions appelées par le jeu. Elles renvoient un mot qui dit ce qui s'est passé (« sent », « off », « quiet »…,
    /// voir <see cref="PushStatus"/>) : le jeu l'écrit dans la console et n'affiche rien au joueur.
    /// </summary>
    public sealed class DuelPushModule
    {
        /// <summary>
        /// Par le jeu de l'invitant, juste après avoir écrit son invitation (GameApp.InviteFriendToDuel). Le résultat est
        /// aussi gardé dans les données protégées de l'invitant (push_status), pour le diagnostic dans le tableau de bord.
        /// </summary>
        [CloudCodeFunction(DuelPush.Function)]
        public Task<string> DuelInvite(IExecutionContext context, IGameApiClient gameApi, IApnsSender apns, string targetId) =>
            Service(context, gameApi, apns).SendAsync(context.PlayerId, targetId, DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        /// <summary>Auto-test de la liaison avec Apple (déploiement, vérification avant publication).</summary>
        [CloudCodeFunction(DuelPush.CheckFunction)]
        public Task<string> DuelPushCheck(IExecutionContext context, IGameApiClient gameApi, IApnsSender apns) =>
            Service(context, gameApi, apns).SelfTestAsync(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        static DuelPushService Service(IExecutionContext context, IGameApiClient gameApi, IApnsSender apns) =>
            new DuelPushService(new CloudSaveStore(context, gameApi), new SecretManagerCredentials(context, gameApi), apns);
    }
}

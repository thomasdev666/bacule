#if BASCULE_NOTIFICATIONS
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Bascule.EditorTools
{
    /// <summary>
    /// Paquet « Mobile Notifications » (rappels du jeu, GameApp.ScheduleNotifications) : par défaut, il pose la question
    /// « Autoriser les notifications ? » dès le premier lancement de l'appli, avant même l'accueil. Le jeu la pose
    /// lui-même, au bon moment (après le premier coffre, avec son explication). On la coupe donc au lancement, et on
    /// n'ajoute pas les notifications à distance (aucun serveur ne les envoie). Appliqué à chaque chargement des scripts
    /// et avant chaque build : rien à cocher à la main.
    /// </summary>
    [InitializeOnLoad]
    public sealed class NotificationsSetup : IPreprocessBuildWithReport
    {
        static NotificationsSetup() => EditorApplication.delayCall += Apply;

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report) => Apply();

        static void Apply()
        {
            if (Unity.Notifications.NotificationSettings.iOSSettings.RequestAuthorizationOnAppLaunch)
                Unity.Notifications.NotificationSettings.iOSSettings.RequestAuthorizationOnAppLaunch = false;
            if (Unity.Notifications.NotificationSettings.iOSSettings.AddRemoteNotificationCapability)
                Unity.Notifications.NotificationSettings.iOSSettings.AddRemoteNotificationCapability = false;
        }
    }
}
#endif

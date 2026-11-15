#if BASCULE_NOTIFICATIONS
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Bascule.EditorTools
{
    /// <summary>
    /// Réglages du paquet « Mobile Notifications », appliqués à chaque chargement des scripts et avant chaque build :
    /// rien à cocher à la main.
    ///
    /// Rappels (GameApp.ScheduleNotifications) : par défaut, le paquet pose la question « Autoriser les notifications ? »
    /// dès le premier lancement, avant même l'accueil. Le jeu la pose lui-même, au bon moment (après le premier coffre,
    /// avec son explication) : on la coupe au lancement.
    ///
    /// Notifications de duel (1.5, docs/NOTIFICATIONS.md) : il faut la capacité « Push Notifications » dans l'app. Elle
    /// n'est ajoutée que si l'interrupteur Bascule > Notifications de duel (iOS) est allumé, car elle exige aussi un
    /// réglage chez Apple (App ID et profil) : sans lui, Codemagic ne pourrait plus signer l'app. Éteint, le jeu marche
    /// comme avant (rappels compris), seule la notification de duel ne part pas.
    /// </summary>
    [InitializeOnLoad]
    public sealed class NotificationsSetup : IPreprocessBuildWithReport
    {
        /// <summary>Symbole des Player Settings iOS qui allume les notifications de duel (posé par le menu).</summary>
        public const string PushDefine = "BASCULE_PUSH";
        const string MenuOn = "Bascule/Notifications de duel (iOS)/Activer";
        const string MenuOff = "Bascule/Notifications de duel (iOS)/Désactiver";

        static NotificationsSetup() => EditorApplication.delayCall += Apply;

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            Apply();
            if (report.summary.platform == BuildTarget.iOS)
                Debug.Log(PushEnabled
                    ? "[Bascule] Notifications de duel : ACTIVÉES (capacité Push ajoutée à l'app, environnement de production)."
                    : "[Bascule] Notifications de duel : désactivées (Bascule > Notifications de duel (iOS) > Activer, voir docs/NOTIFICATIONS.md).");
        }

        public static bool PushEnabled
        {
            get
            {
                string defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.iOS) ?? "";
                foreach (var d in defines.Split(';'))
                    if (d.Trim() == PushDefine) return true;
                return false;
            }
        }

        static void Apply()
        {
            if (Unity.Notifications.NotificationSettings.iOSSettings.RequestAuthorizationOnAppLaunch)
                Unity.Notifications.NotificationSettings.iOSSettings.RequestAuthorizationOnAppLaunch = false;
            if (Unity.Notifications.NotificationSettings.iOSSettings.NotificationRequestAuthorizationForRemoteNotificationsOnAppLaunch)
                Unity.Notifications.NotificationSettings.iOSSettings.NotificationRequestAuthorizationForRemoteNotificationsOnAppLaunch = false;
            bool push = PushEnabled;
            if (Unity.Notifications.NotificationSettings.iOSSettings.AddRemoteNotificationCapability != push)
                Unity.Notifications.NotificationSettings.iOSSettings.AddRemoteNotificationCapability = push;
            if (!push) return;
            // App Store et TestFlight passent par le serveur de production d'Apple (le module n'utilise que lui).
            if (!Unity.Notifications.NotificationSettings.iOSSettings.UseAPSReleaseEnvironment)
                Unity.Notifications.NotificationSettings.iOSSettings.UseAPSReleaseEnvironment = true;
            // Jeu ouvert : pas de bandeau iOS par-dessus la partie, l'invitation s'affiche déjà dans le jeu (CheckInvites).
            if (Unity.Notifications.NotificationSettings.iOSSettings.RemoteNotificationForegroundPresentationOptions != Unity.Notifications.iOS.PresentationOption.None)
                Unity.Notifications.NotificationSettings.iOSSettings.RemoteNotificationForegroundPresentationOptions = Unity.Notifications.iOS.PresentationOption.None;
        }

        [MenuItem(MenuOn, priority = 50)]
        static void TurnOn()
        {
            if (!EditorUtility.DisplayDialog("Notifications de duel",
                "À activer seulement une fois fait chez Apple (docs/NOTIFICATIONS.md, étapes 1 à 3) :\n\n" +
                "• « Push Notifications » coché sur l'identifiant fr.orpheex.bascule ;\n" +
                "• l'ancien profil App Store de l'app supprimé (Codemagic en refait un avec Push) ;\n" +
                "• la clé .p8 rangée dans le Secret Manager de Unity.\n\n" +
                "Sinon Codemagic ne pourra pas signer le prochain build.",
                "C'est fait, activer", "Annuler"))
                return;
            SetPush(true);
        }

        [MenuItem(MenuOn, true)] static bool CanTurnOn() => !PushEnabled;

        [MenuItem(MenuOff, priority = 51)]
        static void TurnOff() => SetPush(false);

        [MenuItem(MenuOff, true)] static bool CanTurnOff() => PushEnabled;

        static void SetPush(bool on)
        {
            string defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.iOS) ?? "";
            var kept = new System.Collections.Generic.List<string>();
            foreach (var d in defines.Split(';'))
                if (d.Trim().Length > 0 && d.Trim() != PushDefine) kept.Add(d.Trim());
            if (on) kept.Add(PushDefine);
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.iOS, string.Join(";", kept));
            Apply();
            AssetDatabase.SaveAssets();
            Debug.Log("[Bascule] Notifications de duel (iOS) : " + (on ? "activées. Refaire le build iOS puis la copie vers bacule-ios." : "désactivées."));
        }
    }
}
#endif

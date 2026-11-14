#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace Bascule.EditorTools
{
    /// <summary>
    /// Réglages du projet Xcode après chaque build iOS, pour qu'App Store Connect n'ait rien à redemander :
    /// - ITSAppUsesNonExemptEncryption = NO : le jeu n'utilise que le chiffrement standard d'iOS (HTTPS) ;
    ///   sans cette clé, chaque build reste bloqué sur la question « conformité à l'export » avant TestFlight.
    /// - Langue de développement : français (le jeu est publié en français).
    /// - NSPhotoLibraryAddUsageDescription : la feuille de partage d'iOS propose « Enregistrer l'image » pour la carte
    ///   de score ; sans ce texte, iOS ferme l'application quand le joueur touche ce bouton (et Apple refuse le build).
    /// - Schéma « bascule:// » (invitation au duel) : déclaré par PlayerSettings.iOS.iOSUrlSchemes, vérifié ici.
    /// - Pubs personnalisées (1.4) : texte de la demande de suivi (NSUserTrackingUsageDescription), cadre
    ///   AppTrackingTransparency, identifiants SKAdNetwork de tous les réseaux, identifiant AdMob s'il est rempli
    ///   (AdNetworkSettings, docs/PUBS.md).
    /// </summary>
    public static class IosBuildPost
    {
        /// <summary>Texte montré par iOS quand le joueur enregistre sa carte de score dans ses photos.</summary>
        const string PhotoAddUsage = "Bascule enregistre ta carte de score dans tes photos quand tu choisis « Enregistrer l'image ».";
        /// <summary>
        /// Texte de la demande de suivi d'iOS, montrée seulement après l'accord du joueur dans le jeu (15 ans ou plus).
        /// Apple exige qu'il dise clairement à quoi sert le suivi.
        /// </summary>
        const string TrackingUsage = "Ton accord permet d'afficher des pubs plus adaptées à tes goûts. Le jeu reste gratuit et identique, que tu acceptes ou non.";

        [PostProcessBuild(900)]
        public static void OnPostprocessBuild(BuildTarget target, string path)
        {
            if (target != BuildTarget.iOS) return;
            var plistPath = Path.Combine(path, "Info.plist");
            if (!File.Exists(plistPath)) return;
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            var root = plist.root;
            root.SetBoolean("ITSAppUsesNonExemptEncryption", false);
            root.SetString("CFBundleDevelopmentRegion", "fr");
            var langs = root.CreateArray("CFBundleLocalizations");
            langs.AddString("fr");
            // Partage de la carte de score : « Enregistrer l'image » ajoute une photo à la photothèque (ajout seul, jamais de lecture).
            root.SetString("NSPhotoLibraryAddUsageDescription", PhotoAddUsage);

            // Schéma du lien d'invitation, au cas où il manquerait dans les réglages du projet.
            bool hasScheme = false;
            if (root.values.TryGetValue("CFBundleURLTypes", out var typesEl))
            {
                foreach (var t in typesEl.AsArray().values)
                {
                    var dict = t.AsDict();
                    if (dict.values.TryGetValue("CFBundleURLSchemes", out var schemes))
                        foreach (var sc in schemes.AsArray().values)
                            if (sc.AsString() == Core.DuelRules.AppScheme) hasScheme = true;
                }
            }
            if (!hasScheme)
            {
                var types = root.values.ContainsKey("CFBundleURLTypes") ? root["CFBundleURLTypes"].AsArray() : root.CreateArray("CFBundleURLTypes");
                var d = types.AddDict();
                d.SetString("CFBundleURLName", "fr.orpheex.bascule.duel");
                d.CreateArray("CFBundleURLSchemes").AddString(Core.DuelRules.AppScheme);
            }
            // Pubs personnalisées : texte de la demande de suivi, réseaux publicitaires, AdMob.
            root.SetString("NSUserTrackingUsageDescription", TrackingUsage);
            var skan = root.values.ContainsKey("SKAdNetworkItems") ? root["SKAdNetworkItems"].AsArray() : root.CreateArray("SKAdNetworkItems");
            var known = new System.Collections.Generic.HashSet<string>();
            foreach (var item in skan.values)
            {
                var d = item.AsDict();
                if (d != null && d.values.TryGetValue("SKAdNetworkIdentifier", out var idEl)) known.Add(idEl.AsString().ToLowerInvariant());
            }
            foreach (var id in AdNetworkSettings.SkAdNetworkIds)
            {
                if (!known.Add(id.ToLowerInvariant())) continue;
                skan.AddDict().SetString("SKAdNetworkIdentifier", id.ToLowerInvariant());
            }
            if (!string.IsNullOrEmpty(AdNetworkSettings.AdMobIosAppId) && !root.values.ContainsKey("GADApplicationIdentifier"))
                root.SetString("GADApplicationIdentifier", AdNetworkSettings.AdMobIosAppId);
            plist.WriteToFile(plistPath);

            // Cadre de la demande de suivi (Assets/Plugins/iOS/BasculeTracking.mm), lié en « faible » par prudence.
            var projPath = PBXProject.GetPBXProjectPath(path);
            var proj = new PBXProject();
            proj.ReadFromFile(projPath);
            proj.AddFrameworkToProject(proj.GetUnityFrameworkTargetGuid(), "AppTrackingTransparency.framework", true);
            proj.WriteToFile(projPath);

            KeepRunningWhenInactive(path);
        }

        /// <summary>
        /// 1.5 : Unity arrête sa boucle d'affichage dès que l'appli n'est plus active (Centre de contrôle, notification
        /// tirée), même en mode « Custom ». Un duel en direct se figeait alors : plus rien n'était envoyé, et le relais
        /// coupe au bout de 10 s. On la laisse tourner tant que l'appli est seulement « inactive » (toujours à l'écran) ;
        /// en arrière-plan elle s'arrête comme avant (iOS interdit d'y dessiner).
        /// Si le texte attendu n'est pas trouvé (autre version de Unity), on ne touche à rien et on le dit.
        /// </summary>
        /// <summary>
        /// En mode « Custom » sans cette retouche, le jeu ne tourne plus du tout quand l'appli devient inactive : les
        /// sorties ne seraient plus enregistrées à temps. Erreur bien visible : ne pas envoyer ce build, prévenir Claude.
        /// </summary>
        const string NotPatched = "BASCULE : NE PAS ENVOYER CE BUILD. La retouche de la boucle d'affichage n'a pas pu être appliquée (autre version de Unity ?) : envoyer une capture à Claude";

        static void KeepRunningWhenInactive(string path)
        {
            var file = Path.Combine(path, "Classes", "UnityAppController+Rendering.mm");
            if (!File.Exists(file)) { UnityEngine.Debug.LogError(NotPatched + " (UnityAppController+Rendering.mm introuvable)"); return; }
            var text = File.ReadAllText(file);
            const string Done = "UIApplicationStateInactive /* Bascule */";
            if (text.Contains(Done)) return;
            int method = text.IndexOf("- (void)repaintDisplayLink", System.StringComparison.Ordinal);
            const string Guard = "if (!_didResignActive)";
            int at = method < 0 ? -1 : text.IndexOf(Guard, method, System.StringComparison.Ordinal);
            if (at < 0 || at - method > 200)
            {
                UnityEngine.Debug.LogError(NotPatched + " (boucle d'affichage de Unity non reconnue)");
                return;
            }
            text = text.Remove(at, Guard.Length).Insert(at,
                "if (!_didResignActive || UIApplication.sharedApplication.applicationState == " + Done + ")");
            File.WriteAllText(file, text);
            UnityEngine.Debug.Log("Bascule : le jeu continue quand le Centre de contrôle est ouvert (UnityAppController+Rendering.mm)");
        }
    }
}
#endif

using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Bascule.EditorTools
{
    /// <summary>
    /// Versions de test sur téléphone, construites depuis le PC, et captures d'écran pour les stores.
    /// </summary>
    public static class DeviceBuilds
    {
        /// <summary>APK Android de test (signé avec la clé de débogage de Unity : à installer directement, pas pour Google Play).</summary>
        [MenuItem("Bascule/Android : construire l'APK de test", priority = 31)]
        public static void BuildApk()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                EditorUtility.DisplayDialog("Bascule", "Le module « Android Build Support » n'est pas installé.\n\nUnity Hub → Installs → ta version de Unity → roue dentée → Add modules → Android Build Support (avec OpenJDK et Android SDK & NDK). Puis relance Unity.", "OK");
                return;
            }
            EditorUserBuildSettings.buildAppBundle = false;
            string path = "Builds/Bascule_V" + BasculeSetup.Version.Split('.')[0] + ".apk";
            Directory.CreateDirectory("Builds");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = System.Array.ConvertAll(EditorBuildSettings.scenes, s => s.path),
                locationPathName = path,
                target = BuildTarget.Android,
                options = BuildOptions.None,
            });
            if (report.summary.result != BuildResult.Succeeded)
            {
                EditorUtility.DisplayDialog("Bascule", "L'APK n'a pas pu être construit (voir la console).", "OK");
                return;
            }
            EditorUtility.RevealInFinder(Path.GetFullPath(path));
            EditorUtility.DisplayDialog("Bascule",
                "APK prêt : " + path + "\n\n" +
                "Pour l'installer : copie-le sur le téléphone Android (câble, Google Drive…), ouvre-le et autorise « installer des applis inconnues ».\n" +
                "Ou branche le téléphone (débogage USB activé) et utilise File → Build And Run.", "OK");
        }

        /// <summary>
        /// Fichier à envoyer sur Google Play : un Android App Bundle (.aab), signé avec TA clé d'envoi (keystore), en
        /// IL2CPP + ARM64, ciblant le niveau d'API Android le plus récent installé. Chaque envoi doit avoir un numéro de
        /// version (versionCode) plus grand que le précédent : il est augmenté tout seul ici.
        /// </summary>
        [MenuItem("Bascule/Android : construire le fichier Google Play (.aab)", priority = 30)]
        public static void BuildPlayBundle()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                EditorUtility.DisplayDialog("Bascule", "Le module « Android Build Support » n'est pas installé.\n\nUnity Hub → Installs → ta version de Unity → roue dentée → Add modules → Android Build Support (avec OpenJDK et Android SDK & NDK). Puis relance Unity.", "OK");
                return;
            }
            // Signature : Google Play refuse un fichier signé avec la clé de débogage de Unity.
            if (!PlayerSettings.Android.useCustomKeystore || string.IsNullOrEmpty(PlayerSettings.Android.keystoreName) || !File.Exists(PlayerSettings.Android.keystoreName)
                || string.IsNullOrEmpty(PlayerSettings.Android.keyaliasName)
                || string.IsNullOrEmpty(PlayerSettings.Android.keystorePass) || string.IsNullOrEmpty(PlayerSettings.Android.keyaliasPass))
            {
                EditorUtility.DisplayDialog("Bascule : clé de signature",
                    "Il faut signer le fichier avec ta clé d'envoi (keystore).\n\n" +
                    "Edit → Project Settings → Player → onglet Android → Publishing Settings :\n" +
                    "• la première fois : Keystore Manager → Create New → enregistre le fichier .keystore HORS du projet, " +
                    "garde le mot de passe précieusement (sans lui, plus de mise à jour possible) ;\n" +
                    "• ensuite : coche Custom Keystore, choisis ce fichier, tape le mot de passe, choisis l'alias et son mot de passe.\n\n" +
                    "Unity oublie les mots de passe à chaque redémarrage : retape-les avant chaque construction.", "OK");
                SettingsService.OpenProjectSettings("Project/Player");
                return;
            }
            BasculeSetup.ConfigureMenuSilent();
            // Google Play : 64 bits obligatoire, niveau d'API le plus récent installé.
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24; // Google Play : API 24 minimum
            // Un numéro de plus à chaque envoi (Google refuse deux envois avec le même).
            PlayerSettings.Android.bundleVersionCode += 1;
            EditorUserBuildSettings.buildAppBundle = true;
            EditorUserBuildSettings.development = false;
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

            int code = PlayerSettings.Android.bundleVersionCode;
            string path = "Builds/Bascule_" + BasculeSetup.Version + "_" + code + ".aab";
            Directory.CreateDirectory("Builds");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = System.Array.ConvertAll(EditorBuildSettings.scenes, s => s.path),
                locationPathName = path,
                target = BuildTarget.Android,
                options = BuildOptions.None,
            });
            if (report.summary.result != BuildResult.Succeeded)
            {
                PlayerSettings.Android.bundleVersionCode -= 1; // rien n'a été produit : on garde le numéro pour le prochain essai
                EditorUtility.DisplayDialog("Bascule", "Le fichier Google Play n'a pas pu être construit (voir la console).", "OK");
                return;
            }
            AssetDatabase.SaveAssets();
            EditorUtility.RevealInFinder(Path.GetFullPath(path));
            EditorUtility.DisplayDialog("Bascule",
                "Fichier Google Play prêt : " + path + "\n(version " + BasculeSetup.Version + ", versionCode " + code + ")\n\n" +
                "Play Console → Bascule → Tests (interne) ou Production → Créer une release → importe ce fichier .aab.", "OK");
        }

        /// <summary>
        /// Capture d'écran pendant le jeu, à la taille de la vue Game. Pour les stores, choisir d'abord le format
        /// (Bascule → Format) : iPhone 1290 × 2796 ou 1179 × 2556, Android 1080 × 1920 ou plus.
        /// </summary>
        [MenuItem("Bascule/Capture d'écran pour les stores %#k", priority = 32)]
        public static void Capture()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Bascule", "Appuie d'abord sur Play, mets le jeu dans l'état voulu (accueil, partie, fin de partie), puis relance cette commande (Ctrl+Maj+K).", "OK");
                return;
            }
            Directory.CreateDirectory("Builds/Captures");
            string file = "Builds/Captures/bascule_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
            ScreenCapture.CaptureScreenshot(file);
            Debug.Log("[Bascule] Capture enregistrée : " + Path.GetFullPath(file));
        }
    }
}

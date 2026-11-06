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

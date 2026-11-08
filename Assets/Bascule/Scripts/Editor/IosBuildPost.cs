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
    /// - Schéma « bascule:// » (invitation au duel) : déclaré par PlayerSettings.iOS.iOSUrlSchemes, vérifié ici.
    /// </summary>
    public static class IosBuildPost
    {
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
            plist.WriteToFile(plistPath);
        }
    }
}
#endif

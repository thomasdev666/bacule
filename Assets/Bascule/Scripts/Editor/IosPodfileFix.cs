#if UNITY_IOS
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Callbacks;

namespace Bascule.EditorTools
{
    /// <summary>
    /// Régies livrées en bibliothèque dynamique (.framework à embarquer) : le Podfile généré par External Dependency
    /// Manager ne les lie qu'à UnityFramework, la cible de l'app (Unity-iPhone) reste vide, donc Xcode ne les copie
    /// pas dans l'app et iOS la ferme au lancement (« Library not loaded », 1.4 build 21).
    /// On ajoute ces SDK à la cible Unity-iPhone : CocoaPods les embarque alors dans l'app. Les SDK statiques ne
    /// sont pas ajoutés (sinon leur code serait en double).
    /// Vérifié le 13/11/2026 sur les binaires : InMobiSDK 11.5 et AppLovinSDK 13.7 sont dynamiques ; Unity Ads,
    /// Google, Liftoff (version statique), Chartboost, Mintegral, Pangle et DT Exchange sont statiques.
    /// À revoir à chaque nouvelle régie (docs/PUBS.md).
    /// </summary>
    public static class IosPodfileFix
    {
        /// <summary>Adaptateur LevelPlay → SDK dynamique qu'il tire.</summary>
        static readonly (string adapter, string sdk)[] DynamicSdks =
        {
            ("IronSourceInMobiAdapter", "InMobiSDK"),
            ("IronSourceAppLovinAdapter", "AppLovinSDK"),
        };

        const string MainTarget = "target 'Unity-iPhone' do";

        // Après External Dependency Manager (40 : création du Podfile, 50 : pod install).
        [PostProcessBuild(950)]
        public static void OnPostprocessBuild(BuildTarget target, string path)
        {
            if (target != BuildTarget.iOS) return;
            var podfile = Path.Combine(path, "Podfile");
            if (!File.Exists(podfile)) return;
            var text = File.ReadAllText(podfile);
            int at = text.IndexOf(MainTarget, System.StringComparison.Ordinal);
            if (at < 0) return;
            var add = new StringBuilder();
            foreach (var (adapter, sdk) in DynamicSdks)
                if (text.Contains("'" + adapter + "'") && !text.Contains("pod '" + sdk + "'"))
                    add.Append("  pod '").Append(sdk).Append("'\n");
            if (add.Length == 0) return;
            int lineEnd = text.IndexOf('\n', at);
            if (lineEnd < 0) return;
            text = text.Insert(lineEnd + 1, add.ToString());
            File.WriteAllText(podfile, text);
            UnityEngine.Debug.Log("Bascule : SDK dynamiques ajoutés à la cible Unity-iPhone du Podfile\n" + add);
        }
    }
}
#endif

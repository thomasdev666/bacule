#if UNITY_ANDROID
using System.IO;
using System.Xml;
using UnityEditor.Android;

namespace Bascule.EditorTools
{
    /// <summary>
    /// Android : marque l'écran portrait et le Wi-Fi comme facultatifs, et ajoute l'identifiant d'application Google AdMob au manifeste quand il est rempli
    /// (AdNetworkSettings.AdMobAndroidAppId). Sans lui, le SDK de Google ferme l'application au lancement dès que
    /// l'adaptateur AdMob de LevelPlay est installé. Rien n'est ajouté s'il est vide ou déjà présent.
    /// </summary>
    public sealed class AndroidAdsPost : IPostGenerateGradleAndroidProject
    {
        const string Android = "http://schemas.android.com/apk/res/android";
        const string Key = "com.google.android.gms.ads.APPLICATION_ID";

        public int callbackOrder => 900;

        /// <summary>
        /// Fonctionnalités que Unity ou les régies déclarent obligatoires alors que le jeu s'en passe : Google Play cachait
        /// l'app aux appareils qui ne les ont pas (tablettes, Chromebooks, Google Play Jeux sur PC). Marquées facultatives
        /// dans le manifeste de l'app (il l'emporte sur ceux des bibliothèques).
        /// </summary>
        static readonly string[] OptionalFeatures = { "android.hardware.screen.portrait", "android.hardware.wifi" };
        const string Tools = "http://schemas.android.com/tools";

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            MarkFeaturesOptional(Path.Combine(path, "..", "launcher", "src", "main", "AndroidManifest.xml"));
            if (string.IsNullOrEmpty(AdNetworkSettings.AdMobAndroidAppId)) return;
            var manifest = Path.Combine(path, "src", "main", "AndroidManifest.xml");
            if (!File.Exists(manifest)) return;
            var doc = new XmlDocument();
            doc.Load(manifest);
            var app = doc.SelectSingleNode("/manifest/application") as XmlElement;
            if (app == null) return;
            foreach (XmlNode n in app.ChildNodes)
                if (n is XmlElement e && e.Name == "meta-data" && e.GetAttribute("name", Android) == Key) return;
            var meta = doc.CreateElement("meta-data");
            meta.SetAttribute("name", Android, Key);
            meta.SetAttribute("value", Android, AdNetworkSettings.AdMobAndroidAppId);
            app.AppendChild(meta);
            doc.Save(manifest);
        }

        static void MarkFeaturesOptional(string manifest)
        {
            if (!File.Exists(manifest)) return;
            var doc = new XmlDocument();
            doc.Load(manifest);
            var root = doc.DocumentElement;
            if (root == null) return;
            if (!root.HasAttribute("xmlns:tools")) root.SetAttribute("xmlns:tools", Tools);
            foreach (var name in OptionalFeatures)
            {
                XmlElement feature = null;
                foreach (XmlNode n in root.ChildNodes)
                    if (n is XmlElement e && e.Name == "uses-feature" && e.GetAttribute("name", Android) == name) feature = e;
                if (feature == null)
                {
                    feature = doc.CreateElement("uses-feature");
                    var app = root.SelectSingleNode("application");
                    if (app != null) root.InsertBefore(feature, app); else root.AppendChild(feature);
                    feature.SetAttribute("name", Android, name);
                }
                feature.SetAttribute("required", Android, "false");
                feature.SetAttribute("node", Tools, "replace");
            }
            doc.Save(manifest);
        }
    }
}
#endif

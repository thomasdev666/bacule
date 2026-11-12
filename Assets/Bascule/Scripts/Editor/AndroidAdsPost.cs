#if UNITY_ANDROID
using System.IO;
using System.Xml;
using UnityEditor.Android;

namespace Bascule.EditorTools
{
    /// <summary>
    /// Android : ajoute l'identifiant d'application Google AdMob au manifeste quand il est rempli
    /// (AdNetworkSettings.AdMobAndroidAppId). Sans lui, le SDK de Google ferme l'application au lancement dès que
    /// l'adaptateur AdMob de LevelPlay est installé. Rien n'est ajouté s'il est vide ou déjà présent.
    /// </summary>
    public sealed class AndroidAdsPost : IPostGenerateGradleAndroidProject
    {
        const string Android = "http://schemas.android.com/apk/res/android";
        const string Key = "com.google.android.gms.ads.APPLICATION_ID";

        public int callbackOrder => 900;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
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
    }
}
#endif

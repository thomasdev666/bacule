using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Bascule.EditorTools
{
    /// <summary>
    /// Vue Game au format téléphone. Unity Remote envoie au téléphone l'image de la vue Game telle quelle :
    /// si elle est en paysage, l'image est écrasée sur l'iPhone. Au moment d'appuyer sur Play, si la vue Game est
    /// plus large que haute, on la passe au format portrait du téléphone choisi (iPhone 14 Pro par défaut).
    /// Unity n'offre pas d'API publique pour la taille de la vue Game : on passe par ses classes internes,
    /// et en cas d'échec on l'explique dans la console sans rien casser.
    /// </summary>
    [InitializeOnLoad]
    public static class PhoneView
    {
        const string PrefKey = "Bascule.PhoneView";
        const string MenuAuto = "Bascule/Vue téléphone automatique";

        struct Device { public string Name; public int W, H; }

        static readonly Device[] Devices =
        {
            new Device { Name = "iPhone 14 Pro", W = 1179, H = 2556 },
            new Device { Name = "iPhone SE", W = 750, H = 1334 },
            new Device { Name = "Android 20:9", W = 1080, H = 2400 },
            new Device { Name = "iPad", W = 1640, H = 2360 },
            new Device { Name = "Store iPhone 6,7 pouces", W = 1290, H = 2796 },
        };

        static PhoneView()
        {
            // Unity Remote prêt pour l'iPhone dès l'ouverture du projet, réglé pour le moins de décalage possible.
            if (string.IsNullOrEmpty(EditorSettings.unityRemoteDevice) || EditorSettings.unityRemoteDevice == "None")
                EditorSettings.unityRemoteDevice = "Any iOS Device";
            if (!EditorPrefs.GetBool(PrefKey + ".RemoteSharp", false))
            {
                EditorSettings.unityRemoteResolution = "Downsize";
                EditorSettings.unityRemoteCompression = "JPEG";
            }
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.ExitingEditMode || !EditorPrefs.GetBool(PrefKey, true)) return;
                var size = Handles.GetMainGameViewSize();
                if (size.x > size.y * 0.9f) Apply(Devices[EditorPrefs.GetInt(PrefKey + ".Device", 0)], false);
            };
        }

        [MenuItem(MenuAuto, priority = 10)]
        static void ToggleAuto() => EditorPrefs.SetBool(PrefKey, !EditorPrefs.GetBool(PrefKey, true));

        [MenuItem(MenuAuto, true)]
        static bool ToggleAutoCheck() { Menu.SetChecked(MenuAuto, EditorPrefs.GetBool(PrefKey, true)); return true; }

        [MenuItem("Bascule/Format/iPhone 14 Pro (1179 × 2556)", priority = 11)] static void D0() => Pick(0);
        [MenuItem("Bascule/Format/iPhone SE (750 × 1334)", priority = 12)] static void D1() => Pick(1);
        [MenuItem("Bascule/Format/Android 20:9 (1080 × 2400)", priority = 13)] static void D2() => Pick(2);
        [MenuItem("Bascule/Format/iPad (1640 × 2360)", priority = 14)] static void D3() => Pick(3);
        [MenuItem("Bascule/Format/Captures App Store (1290 × 2796)", priority = 15)] static void D4() => Pick(4);

        static void Pick(int i)
        {
            EditorPrefs.SetInt(PrefKey + ".Device", i);
            Apply(Devices[i], true);
        }

        /// <summary>
        /// Unity Remote renvoie chaque image de l'ordinateur vers le téléphone par le câble : plus l'image est grande
        /// et peu compressée, plus il y a de décalage. « Réactif » (par défaut) : image réduite et compressée, peu de décalage.
        /// « Net » : image pleine résolution, plus de décalage. Aucun des deux ne montre la vraie qualité du jeu.
        /// </summary>
        [MenuItem("Bascule/Unity Remote : réactif (recommandé)", priority = 15)]
        public static void RemoteFast() => SetupRemote(false);

        [MenuItem("Bascule/Unity Remote : image nette (plus de décalage)", priority = 16)]
        public static void RemoteSharp() => SetupRemote(true);

        static void SetupRemote(bool sharp)
        {
            EditorPrefs.SetBool(PrefKey + ".RemoteSharp", sharp);
            EditorSettings.unityRemoteDevice = "Any iOS Device";
            EditorSettings.unityRemoteResolution = sharp ? "Normal" : "Downsize";
            EditorSettings.unityRemoteCompression = sharp ? "PNG" : "JPEG";
            Pick(0);
            EditorUtility.DisplayDialog("Bascule",
                "Unity Remote est réglé pour un iPhone (" + (sharp ? "image nette, plus de décalage" : "réactif, image réduite") + ").\n\n" +
                "1. Branche l'iPhone en USB (sur PC, iTunes doit être installé) et ouvre Unity Remote 5.\n" +
                "2. Appuie sur Play.\n\nUnity Remote ne montre ni la vraie fluidité ni la vraie qualité : pour ça, utilise « Bascule → Version web » et ouvre le lien sur l'iPhone.", "OK");
        }

        static void Apply(Device d, bool verbose)
        {
            try
            {
                var asm = typeof(Editor).Assembly;
                var sizesType = asm.GetType("UnityEditor.GameViewSizes");
                var single = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
                var sizes = single.GetProperty("instance").GetValue(null, null);
                var group = sizesType.GetProperty("currentGroup").GetValue(sizes, null);
                var groupType = group.GetType();
                int index = Find(group, d);
                if (index < 0)
                {
                    var sizeType = asm.GetType("UnityEditor.GameViewSize");
                    var kind = asm.GetType("UnityEditor.GameViewSizeType");
                    var ctor = sizeType.GetConstructor(new[] { kind, typeof(int), typeof(int), typeof(string) });
                    var size = ctor.Invoke(new object[] { Enum.Parse(kind, "FixedResolution"), d.W, d.H, "Bascule " + d.Name });
                    groupType.GetMethod("AddCustomSize").Invoke(group, new[] { size });
                    sizesType.GetMethod("SaveToHDD")?.Invoke(sizes, null);
                    index = Find(group, d);
                }
                if (index < 0) throw new Exception("taille introuvable");

                var gvType = asm.GetType("UnityEditor.GameView");
                var gv = EditorWindow.GetWindow(gvType, false, null, false);
                const BindingFlags any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                var select = gvType.GetMethod("SizeSelectionCallback", any);
                if (select != null) select.Invoke(gv, new object[] { index, null });
                else gvType.GetProperty("selectedSizeIndex", any).SetValue(gv, index, null);
                gv.Repaint();
                if (verbose) Debug.Log("[Bascule] Vue Game : " + d.Name + " (" + d.W + " × " + d.H + ")");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Bascule] Impossible de régler la vue Game automatiquement (" + e.Message + "). " +
                                 "Dans la vue Game, ouvre la liste des résolutions (en haut), clique sur « + » et ajoute " + d.W + " × " + d.H + ".");
            }
        }

        static int Find(object group, Device d)
        {
            var t = group.GetType();
            int total = (int)t.GetMethod("GetTotalCount").Invoke(group, null);
            var get = t.GetMethod("GetGameViewSize");
            for (int i = 0; i < total; i++)
            {
                var s = get.Invoke(group, new object[] { i });
                var st = s.GetType();
                int w = (int)st.GetProperty("width").GetValue(s, null);
                int h = (int)st.GetProperty("height").GetValue(s, null);
                string kind = st.GetProperty("sizeType").GetValue(s, null).ToString();
                if (w == d.W && h == d.H && kind == "FixedResolution") return i;
            }
            return -1;
        }
    }
}

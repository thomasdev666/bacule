using System.IO;
using Bascule.Core;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Bascule.EditorTools
{
    /// <summary>
    /// Configure le projet automatiquement à la première ouverture (portrait, identifiants, scène principale)
    /// et ajoute le menu « Bascule » dans l'éditeur.
    /// </summary>
    [InitializeOnLoad]
    public static class BasculeSetup
    {
        const string ScenePath = "Assets/Bascule/Scenes/Main.unity";
        const string TuningPath = "Assets/Bascule/Resources/bascule_tuning.json";
        /// <summary>Identifiant définitif (domaine de l'éditeur à l'envers : orpheex.fr). Ne peut plus changer une fois publié.</summary>
        public const string BundleId = "fr.orpheex.bascule";
        public const string Company = "Orphex";
        /// <summary>Version du jeu : la même que celle du zip (Bascule_V9.zip → 9.0).</summary>
        public const string Version = "1.4";
        /// <summary>Numéro de build iOS : à augmenter à chaque envoi à Apple (deux envois ne peuvent pas avoir le même).</summary>
        const string BuildNumber = "8";
        const int VersionCode = 40;

        static BasculeSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (NeedsSetup()) Configure(false);
            };
        }

        static bool NeedsSetup() =>
            !File.Exists(ScenePath) || EditorBuildSettings.scenes.Length == 0 || PlayerSettings.bundleVersion != Version || PlayerSettings.iOS.buildNumber != BuildNumber || PlayerSettings.companyName != Company ||
            PlayerSettings.defaultInterfaceOrientation != UIOrientation.Portrait || PlayerSettings.SplashScreen.show ||
            System.Array.IndexOf(PlayerSettings.iOS.iOSUrlSchemes ?? new string[0], DuelRules.AppScheme) < 0;

        [MenuItem("Bascule/Configurer le projet", priority = 0)]
        public static void ConfigureMenu() => Configure(true);

        /// <summary>Même configuration, sans fenêtre (avant une construction).</summary>
        public static void ConfigureMenuSilent() => Configure(false);

        static void Configure(bool verbose)
        {
            PlayerSettings.productName = "Bascule";
            PlayerSettings.bundleVersion = Version;
            PlayerSettings.iOS.buildNumber = BuildNumber;
            // iOS 15 minimum : Apple refusera les apps qui ciblent iOS 13 à partir d'avril 2027 (ITMS-90068).
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            // Bascule est un jeu d'iPhone, en portrait : pas de version iPad (moins de captures à fournir à Apple).
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneOnly;
            // Lien d'invitation au duel : bascule://duel/CODE ouvre le jeu (la page web duel.html y renvoie).
            PlayerSettings.iOS.iOSUrlSchemes = new[] { DuelRules.AppScheme };
            PlayerSettings.Android.bundleVersionCode = Mathf.Max(PlayerSettings.Android.bundleVersionCode, VersionCode);
            // Google Play exige Android 7.0 (API 24) minimum pour la protection automatique.
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            // Textures et rendu : qualité maximale (pas de réduction de résolution des textures sur téléphone).
            QualitySettings.globalTextureMipmapLimit = 0;
            PlayerSettings.companyName = Company;

            // Portrait uniquement (section 12).
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.statusBarHidden = true;

            foreach (var target in new[] { NamedBuildTarget.Android, NamedBuildTarget.iOS })
            {
                if (PlayerSettings.GetApplicationIdentifier(target) != BundleId)
                    PlayerSettings.SetApplicationIdentifier(target, BundleId);
            }
            // Google Play exige le 64 bits : IL2CPP + ARM64.
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            FixInputHandling();
            SetIcon();
            SetLaunchScreen();

            if (!File.Exists(ScenePath))
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
                var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            if (EditorSceneManager.GetActiveScene().path != ScenePath) EditorSceneManager.OpenScene(ScenePath);
            AssetDatabase.SaveAssets();

            var msg = "[Bascule] Projet configuré : portrait, identifiant " + PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) +
                      ", scène " + ScenePath + ". Appuie sur Play.";
            Debug.Log(msg);
            if (verbose) EditorUtility.DisplayDialog("Bascule", msg, "OK");
        }

        const string IconPath = "Assets/Bascule/Icone/icone_1024.png";
        /// <summary>Bleu du ciel du matin : même couleur que l'écran de chargement web et le rideau du jeu.</summary>
        public static readonly Color SkyColor = new Color(0x5D / 255f, 0xB8 / 255f, 0xF0 / 255f);

        /// <summary>
        /// Démarrage aux couleurs du jeu : plus de logo Unity ni de « Made with Unity » (possible avec Unity 6),
        /// fond bleu ciel, et page de chargement web « Bascule » (Assets/WebGLTemplates/Bascule).
        /// </summary>
        public static void SetLaunchScreen()
        {
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            PlayerSettings.SplashScreen.backgroundColor = SkyColor;
            if (Directory.Exists("Assets/WebGLTemplates/Bascule")) PlayerSettings.WebGL.template = "PROJECT:Bascule";
        }

        /// <summary>Icône de l'application (dessinée par le code : Core/UiArt.cs → IconArtist), pour tous les téléphones.</summary>
        static void SetIcon()
        {
            var importer = AssetImporter.GetAtPath(IconPath) as TextureImporter;
            if (importer != null && (importer.textureCompression != TextureImporterCompression.Uncompressed || importer.mipmapEnabled))
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (icon == null) return;
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
        }

        /// <summary>
        /// Sans le paquet Input System, le jeu utilise l'ancien gestionnaire d'entrées : il ne doit pas être désactivé.
        /// </summary>
        static void FixInputHandling()
        {
            bool hasInputSystemPackage = File.Exists("Packages/manifest.json") &&
                File.ReadAllText("Packages/manifest.json").Contains("\"com.unity.inputsystem\"");
            if (hasInputSystemPackage) return;
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets == null || assets.Length == 0) return;
            var so = new SerializedObject(assets[0]);
            var prop = so.FindProperty("activeInputHandler");
            if (prop == null || prop.intValue != 1) return; // 0 = ancien, 1 = nouveau seulement, 2 = les deux
            prop.intValue = 0;
            so.ApplyModifiedProperties();
            EditorUtility.DisplayDialog("Bascule",
                "Le gestionnaire d'entrées a été remis sur « Input Manager (Old) » (le paquet Input System n'est pas installé). Redémarre Unity pour l'appliquer.",
                "OK");
        }

        [MenuItem("Bascule/Ouvrir les réglages de jeu (JSON)", priority = 20)]
        static void OpenTuning()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(TuningPath);
            if (asset == null) { Debug.LogError("[Bascule] Introuvable : " + TuningPath); return; }
            Selection.activeObject = asset;
            AssetDatabase.OpenAsset(asset);
        }

        [MenuItem("Bascule/Effacer la sauvegarde locale", priority = 40)]
        static void ClearSave()
        {
            if (!EditorUtility.DisplayDialog("Bascule", "Effacer la collection, le record et la partie en cours de l'éditeur ?", "Effacer", "Annuler")) return;
            foreach (var f in new[] { "profile.json", "run.json" })
            {
                var p = Path.Combine(Application.persistentDataPath, f);
                if (File.Exists(p)) File.Delete(p);
            }
            Debug.Log("[Bascule] Sauvegarde effacée : " + Application.persistentDataPath);
        }

        [MenuItem("Bascule/Exporter les 40 têtes en PNG", priority = 60)]
        static void ExportHeads()
        {
            var dir = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Exports/Tetes");
            Directory.CreateDirectory(dir);
            foreach (var h in HeadCatalog.All)
            {
                var r = HeadArtist.Portrait(h, 512);
                var tex = new Texture2D(r.Width, r.Height, TextureFormat.RGBA32, false);
                var px = new Color32[r.Pixels.Length];
                for (int i = 0; i < px.Length; i++) px[i] = new Color32(r.Pixels[i].r, r.Pixels[i].g, r.Pixels[i].b, r.Pixels[i].a);
                tex.SetPixels32(px);
                tex.Apply();
                File.WriteAllBytes(Path.Combine(dir, $"{h.Id:00}_{h.NameEn.Replace(' ', '_')}.png"), tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }
            EditorUtility.RevealInFinder(dir);
        }
    }
}

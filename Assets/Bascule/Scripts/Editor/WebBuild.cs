using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Bascule.EditorTools
{
    /// <summary>
    /// Version web du jeu, pour y jouer sur l'iPhone sans Mac et sans le décalage de Unity Remote :
    /// on la publie (gratuitement, en privé) sur itch.io et on ouvre le lien dans Safari.
    /// Il faut le module « Web Build Support » (Unity Hub → Installs → Unity 6 → Add modules).
    /// </summary>
    public static class WebBuild
    {
        /// <summary>
        /// Page web adaptée au téléphone : pas de zoom (sinon iOS zoome sur le clavier et la page reste bloquée),
        /// champ de saisie en 16 px (en dessous, Safari zoome tout seul), retour en place quand le clavier se ferme.
        /// </summary>
        static void PatchPage(string path)
        {
            if (!File.Exists(path)) return;
            var html = File.ReadAllText(path);
            html = System.Text.RegularExpressions.Regex.Replace(html, "<meta name=\"viewport\"[^>]*>",
                "<meta name=\"viewport\" content=\"width=device-width, height=device-height, initial-scale=1, maximum-scale=1, user-scalable=no, viewport-fit=cover\">");
            const string extra =
                "<style>html,body{margin:0;height:100%;overflow:hidden;overscroll-behavior:none;touch-action:none;background:#17162a}" +
                "input,textarea,select{font-size:16px!important}</style>\n" +
                "<script>document.addEventListener('focusout',function(){setTimeout(function(){window.scrollTo(0,0);document.body.scrollTop=0;},50);});" +
                "document.addEventListener('gesturestart',function(e){e.preventDefault();});</script>\n";
            if (!html.Contains("overscroll-behavior:none")) html = html.Replace("</head>", extra + "</head>");
            File.WriteAllText(path, html);
        }

        const string Output = "Builds/Bascule_Web";

        [MenuItem("Bascule/Version web (jouer sur iPhone dans Safari)", priority = 30)]
        public static void Build()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            {
                EditorUtility.DisplayDialog("Bascule", "Le module « Web Build Support » n'est pas installé.\n\nUnity Hub → Installs → ta version de Unity → roue dentée → Add modules → Web Build Support. Puis relance Unity.", "OK");
                return;
            }
            // Sans compression côté serveur : la version marche sur n'importe quel hébergeur (itch.io compris).
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.runInBackground = false;
            // Mémoire : réservée d'emblée (agrandir la mémoire en cours de partie échoue souvent sur iPhone → erreur « OOM »).
            PlayerSettings.WebGL.initialMemorySize = 256;
            PlayerSettings.WebGL.maximumMemorySize = 1024;
            PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.Geometric;
            // Textures au format des téléphones (ASTC) : sans ça, l'iPhone les décompresse et sature la mémoire.
            EditorUserBuildSettings.webGLBuildSubtarget = WebGLTextureSubtarget.ASTC;
            PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.WebGL, ManagedStrippingLevel.Low);
            // Page de chargement Bascule (logo, Basile, barre jaune) au lieu de celle de Unity, et pas de « Made with Unity ».
            BasculeSetup.SetLaunchScreen();

            var scenes = System.Array.ConvertAll(EditorBuildSettings.scenes, s => s.path);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Output,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            });
            if (report.summary.result != BuildResult.Succeeded)
            {
                EditorUtility.DisplayDialog("Bascule", "La version web n'a pas pu être construite (voir la console).", "OK");
                return;
            }
            PatchPage(Path.Combine(Output, "index.html"));
            EditorUtility.RevealInFinder(Path.GetFullPath(Output));
            EditorUtility.DisplayDialog("Bascule",
                "Version web prête dans " + Output + ".\n\n" +
                "1. Compresse ce dossier en .zip (clic droit → Envoyer vers → Dossier compressé).\n" +
                "2. Sur itch.io (compte gratuit) : Upload new project → Kind : HTML → envoie le zip → coche « This file will be played in the browser » → visibilité Draft ou Restricted.\n" +
                "3. Ouvre la page du jeu sur l'iPhone dans Safari, puis Partager → Sur l'écran d'accueil : il s'ouvre en plein écran comme une appli.", "OK");
        }
    }
}

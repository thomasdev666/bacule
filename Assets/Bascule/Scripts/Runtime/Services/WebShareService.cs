#if UNITY_WEBGL && !UNITY_EDITOR
using System;
using System.IO;
using System.Runtime.InteropServices;
using Bascule.Core;
using UnityEngine;

namespace Bascule.Services
{
    /// <summary>
    /// Partage de la version web : NativeShare n'existe pas dans un navigateur, et le presse-papiers de Unity
    /// n'atteint pas celui du téléphone. On ouvre donc un panneau de la page (Plugins/WebGL/BasculeShare.jslib)
    /// avec l'image de fin et les boutons Partager, Enregistrer l'image, Copier le texte.
    /// </summary>
    public sealed class WebShareService : IShareService
    {
        [DllImport("__Internal")] static extern void BasculeShowShare(string imageBase64, string text, string labelsJson);

        [Serializable]
        sealed class Labels { public string title, share, save, saved, copy, copied, close, hint, sub; }

        public bool ShowsOwnPanel => true;

        public void ShareImage(string imagePath, string text)
        {
            string b64 = "";
            try { if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath)) b64 = Convert.ToBase64String(File.ReadAllBytes(imagePath)); }
            catch (Exception e) { Debug.LogWarning("[Bascule] Image de partage : " + e.Message); }
            BasculeShowShare(b64, text, LabelsJson(b64.Length > 0));
        }

        public void ShareText(string text) => BasculeShowShare("", text, LabelsJson(false));

        static string LabelsJson(bool image) => JsonUtility.ToJson(new Labels
        {
            title = Loc.Get("share_panel_title"), share = Loc.Get("share"), save = Loc.Get("share_save"),
            saved = Loc.Get("share_saved"), copy = Loc.Get("share_copy"), copied = Loc.Get("share_copied"), close = Loc.Get("share_close"),
            hint = image ? Loc.Get("share_hint") : "", sub = image ? "" : "1",
        });
    }
}
#endif

using UnityEditor;

namespace Bascule.EditorTools
{
    /// <summary>
    /// Réglages d'import des images dessinées à l'avance (Resources/ArtPack) : pas de mipmaps, bords nets,
    /// compression GPU haute qualité (ASTC sur téléphone : quasi identique à l'original, 4 fois moins de mémoire).
    /// </summary>
    public sealed class ArtPackImport : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').Contains("/Resources/ArtPack/")) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Default;
            ti.mipmapEnabled = false;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.alphaIsTransparency = true;
            ti.alphaSource = TextureImporterAlphaSource.FromInput;
            ti.sRGBTexture = true;
            ti.wrapMode = UnityEngine.TextureWrapMode.Clamp;
            ti.filterMode = UnityEngine.FilterMode.Bilinear;
            ti.isReadable = false;
            ti.maxTextureSize = 2048;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.crunchedCompression = false;
            // Corps de la mascotte (une tenue par tête, 63 corps) : ASTC 6×6, deux fois plus léger, invisible à l'œil
            // sur ces dessins à aplats. Le reste de l'ArtPack garde la qualité haute.
            bool body = System.IO.Path.GetFileName(assetPath).StartsWith("mascot_");
            foreach (var platform in new[] { "iPhone", "Android" })
            {
                var ps = ti.GetPlatformTextureSettings(platform);
                ps.overridden = body;
                if (body)
                {
                    ps.format = TextureImporterFormat.ASTC_6x6;
                    ps.maxTextureSize = 2048;
                }
                ti.SetPlatformTextureSettings(ps);
            }
        }
    }
}

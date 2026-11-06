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
        }
    }
}

using UnityEditor;
using UnityEngine;

namespace Bascule.EditorTools
{
    /// <summary>
    /// Musique (Resources/Musique) : Vorbis compressé en mémoire, décodé au fil de la lecture.
    /// Le format Vorbis boucle sans aucun trou (contrairement au MP3) et prend peu de mémoire.
    /// </summary>
    public sealed class AudioImport : AssetPostprocessor
    {
        void OnPreprocessAudio()
        {
            if (!assetPath.Replace('\\', '/').Contains("/Resources/Musique/")) return;
            var ai = (AudioImporter)assetImporter;
            ai.forceToMono = false; // stéréo gardée sur téléphone
            ai.loadInBackground = false;
            var s = ai.defaultSampleSettings;
            s.loadType = AudioClipLoadType.CompressedInMemory;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = 0.75f;
            s.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            ai.defaultSampleSettings = s;
            // Web : le navigateur décode tout le morceau en mémoire (4 octets par échantillon). En 32 kHz,
            // les deux couches prennent 20 Mo au lieu de 60 : Safari sur iPhone ne coupe plus le son faute de mémoire.
            var web = s;
            web.sampleRateSetting = AudioSampleRateSetting.OverrideSampleRate;
            web.sampleRateOverride = 32000;
            ai.SetOverrideSampleSettings("WebGL", web);
        }
    }
}

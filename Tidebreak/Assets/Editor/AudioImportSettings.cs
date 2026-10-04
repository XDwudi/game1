using UnityEditor;
using UnityEngine;

namespace Tidebreak.Editor
{
    public class AudioImportSettings : AssetPostprocessor
    {
        void OnPreprocessAudio()
        {
            if(!assetPath.StartsWith("Assets/Resources/Audio/")||!assetPath.EndsWith(".wav"))return;
            var importer=(AudioImporter)assetImporter;
            bool longBed=assetPath.Contains("/Music/")||assetPath.Contains("/Ambience/");
            importer.forceToMono=false;importer.loadInBackground=longBed;importer.preloadAudioData=!longBed;
            var settings=importer.defaultSampleSettings;
            settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=longBed?.72f:.82f;
            settings.loadType=longBed?AudioClipLoadType.Streaming:AudioClipLoadType.DecompressOnLoad;
            settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;importer.defaultSampleSettings=settings;
        }
    }
}

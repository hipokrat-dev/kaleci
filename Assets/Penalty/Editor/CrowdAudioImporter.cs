using UnityEditor;
using UnityEngine;
namespace PenaltyArena.Editor
{
    public sealed class CrowdAudioImporter : AssetPostprocessor
    {
        void OnPreprocessAudio()
        {
            if(!assetPath.StartsWith("Assets/Penalty/Resources/Audio/Crowd"))return;
            var importer=(AudioImporter)assetImporter;
            importer.forceToMono=false;importer.loadInBackground=true;
            var settings=importer.defaultSampleSettings;settings.preloadAudioData=true;
            settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.65f;
            settings.loadType=assetPath.Contains("CrowdBed")?AudioClipLoadType.Streaming:AudioClipLoadType.DecompressOnLoad;
            importer.defaultSampleSettings=settings;
        }
    }
}

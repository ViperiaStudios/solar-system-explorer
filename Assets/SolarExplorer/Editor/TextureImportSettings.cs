using UnityEditor;
using UnityEngine;

namespace SolarExplorer.Editor
{
    public sealed class TextureImportSettings : AssetPostprocessor
    {
        public override uint GetVersion() { return 3; }
        private void OnPreprocessTexture()
        {
            if(!assetPath.StartsWith("Assets/SolarExplorer/Resources/Textures/")) return;
            var importer=(TextureImporter)assetImporter;
            importer.textureType=TextureImporterType.Default;
            importer.sRGBTexture=!(assetPath.Contains("_normal_map") || assetPath.Contains("_specular_map")); importer.mipmapEnabled=true;
            importer.wrapModeU=TextureWrapMode.Repeat; importer.wrapModeV=TextureWrapMode.Clamp;
            importer.filterMode=FilterMode.Trilinear; importer.anisoLevel=16;
            importer.mipmapFilter=TextureImporterMipFilter.KaiserFilter;
            importer.mipMapBias=-0.25f;
            importer.maxTextureSize=assetPath.Contains("8k_") || assetPath.Contains("eso0932a")?8192:(assetPath.Contains("4k_")?4096:2048);
            importer.textureCompression=TextureImporterCompression.CompressedHQ;
            var desktop=importer.GetPlatformTextureSettings("Standalone");
            desktop.overridden=true; desktop.maxTextureSize=importer.maxTextureSize;
            desktop.format=TextureImporterFormat.BC7; desktop.compressionQuality=100;
            importer.SetPlatformTextureSettings(desktop);
            foreach(var platform in new[]{"iPhone","Android"})
            {
                var mobile=importer.GetPlatformTextureSettings(platform);
                mobile.overridden=true; mobile.maxTextureSize=Mathf.Min(4096,importer.maxTextureSize);
                mobile.format=TextureImporterFormat.ASTC_6x6; mobile.compressionQuality=100;
                importer.SetPlatformTextureSettings(mobile);
            }
            importer.alphaSource=TextureImporterAlphaSource.FromInput;
        }
    }
}

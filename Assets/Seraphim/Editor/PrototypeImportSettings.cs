using System;
using UnityEditor;

namespace Seraphim.Editor
{
    public sealed class PrototypeImportSettings : AssetPostprocessor
    {
        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith("Assets/ThirdParty/CC0/Kenney",StringComparison.Ordinal)) return;
            var importer = (ModelImporter)assetImporter;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.isReadable = true;
            if (assetPath.Contains("KenneyCharacters"))
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.importAnimation = true;
                importer.globalScale = 1;
            }
        }

        void OnPreprocessTexture()
        {
            if (!assetPath.Contains("PolyHavenStone")) return;
            var importer = (TextureImporter)assetImporter;
            importer.maxTextureSize = 1024;
            if (assetPath.Contains("_nor_")) importer.textureType = TextureImporterType.NormalMap;
        }
    }
}

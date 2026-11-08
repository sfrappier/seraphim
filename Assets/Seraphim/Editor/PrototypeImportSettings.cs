using System;
using UnityEditor;

namespace Seraphim.Editor
{
    public sealed class PrototypeImportSettings : AssetPostprocessor
    {
        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith("Assets/ThirdParty/CC0/Kenney",StringComparison.Ordinal) && !assetPath.Contains("KayKitAngel")) return;
            var importer = (ModelImporter)assetImporter;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.isReadable = true;
            if (assetPath.Contains("KenneyCharacters") || assetPath.Contains("KayKitAngel"))
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.importAnimation = true;
                importer.globalScale = 1;
            }
        }

        void OnPreprocessTexture()
        {
            if (!assetPath.Contains("PolyHavenStone") && !assetPath.Contains("CathedralTextures")) return;
            var importer = (TextureImporter)assetImporter;
            importer.maxTextureSize = assetPath.Contains("CathedralTextures")?2048:1024;
            importer.anisoLevel=8;
            if (assetPath.Contains("_nor_")) importer.textureType = TextureImporterType.NormalMap;
        }
    }
}

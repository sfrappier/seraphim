using System;
using UnityEditor;

namespace Seraphim.Editor
{
    public sealed class PrototypeImportSettings : AssetPostprocessor
    {
        void OnPreprocessModel()
        {
            if (assetPath.StartsWith("Assets/ThirdParty/CCBY/Characters/",StringComparison.Ordinal))
            {
                var character=(ModelImporter)assetImporter;
                character.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
                character.materialLocation=ModelImporterMaterialLocation.InPrefab;
                character.isReadable=true;
                character.importAnimation=false;
                character.importCameras=false; character.importLights=false;
                character.optimizeGameObjects=false;
                character.animationType=assetPath.Contains("fallen-angel/")?ModelImporterAnimationType.Human:ModelImporterAnimationType.Generic;
                character.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
                return;
            }
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
            if (assetPath.StartsWith("Assets/ThirdParty/CCBY/Characters/",StringComparison.Ordinal))
            {
                var texture=(TextureImporter)assetImporter;
                texture.maxTextureSize=2048; texture.mipmapEnabled=true; texture.anisoLevel=4;
                texture.isReadable=false; texture.textureCompression=TextureImporterCompression.CompressedHQ;
                texture.sRGBTexture=!assetPath.EndsWith("_metalSmooth.png",StringComparison.Ordinal) && !assetPath.EndsWith("_normal.png",StringComparison.Ordinal);
                if (assetPath.EndsWith("_normal.png",StringComparison.Ordinal)) texture.textureType=TextureImporterType.NormalMap;
                return;
            }
            if (!assetPath.Contains("PolyHavenStone") && !assetPath.Contains("CathedralTextures")) return;
            var importer = (TextureImporter)assetImporter;
            importer.maxTextureSize = assetPath.Contains("CathedralTextures")?2048:1024;
            importer.anisoLevel=8;
            if (assetPath.Contains("_nor_")) importer.textureType = TextureImporterType.NormalMap;
        }
    }
}

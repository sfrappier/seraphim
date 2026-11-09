using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

namespace Seraphim.Editor
{
    // Deterministic Unity-side import; ordinary builds need only these committed
    // runtime derivatives, never Blender, the original ZIPs or authenticated services.
    public static class CharacterContentBuilder
    {
        const string Source="Assets/ThirdParty/CCBY/Characters/";
        const string Generated="Assets/Seraphim/Generated/Characters/";
        [Serializable] sealed class ModelSpec { public string key,model; public bool lod; public MaterialSpec[] materials; }
        [Serializable] sealed class MaterialSpec { public string name,baseColor,normal,metalSmooth,emission; public bool alphaClip,doubleSided; }
        [Serializable] sealed class SourceReceipt { public string license,source,originalArchiveSha256; public RuntimeFile[] runtimeFiles; }
        [Serializable] sealed class RuntimeFile { public string file,sha256; }
        static readonly string[] Keys={"fallen-angel","cinder-reaver","ash-invoker","obsidian-brute","rift-scourger","infernal-wyrm"};
        static readonly string[] Names={"SERAPHIM","CINDER REAVER","ASH INVOKER","OBSIDIAN BRUTE","RIFT SCOURGER","VORATH, THE INFERNAL WYRM"};
        static readonly float[] Heights={1.8f,2,2.1f,2.4f,2.7f,7.2f};
        static readonly float[] Health={100,42,32,60,84,9600};
        static readonly float[] Speeds={6,2.6f,3.2f,2,3.5f,2.1f};

        public static void Configure(PrototypeModels catalog)
        {
            Directory.CreateDirectory(Generated);
            AssetDatabase.Refresh();
            var definitions=new List<CharacterModelDefinition>();
            for (var i=0;i<Keys.Length;i++) definitions.Add(Prepare(i));
            catalog.playerModel=definitions[0];
            catalog.character=definitions[0].prefab;
            catalog.characterTexture=null;
            catalog.humanoidAvatar=catalog.character.GetComponentInChildren<Animator>().avatar;
            catalog.demonModels=definitions.Skip(1).Take(DemonRoster.KindCount).ToArray();
            catalog.bossModel=definitions[5];
            EditorUtility.SetDirty(catalog);
        }
        static CharacterModelDefinition Prepare(int index)
        {
            var key=Keys[index]; var folder=Source+key+"/";
            VerifySource(folder);
            if (!File.Exists(folder+"model.json")) throw new BuildFailedException("Character derivatives missing: "+key+". See Tools/Prepare-CharacterModels.ps1.");
            var spec=JsonUtility.FromJson<ModelSpec>(File.ReadAllText(folder+"model.json"));
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(folder+spec.model);
            if (source==null) throw new BuildFailedException("Character model did not import: "+key);
            var materials=new Dictionary<string,Material>();
            foreach (var entry in spec.materials)
            {
                var path=Generated+key+"-"+entry.name+".mat";
                var material=AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material==null)
                {
                    material=new Material(Shader.Find("Universal Render Pipeline/Lit")) {name=entry.name};
                    AssetDatabase.CreateAsset(material,path);
                }
                material.SetColor("_BaseColor",Color.white);
                material.SetTexture("_BaseMap",Texture(folder,entry.baseColor));
                material.SetTexture("_BumpMap",Texture(folder,entry.normal));
                material.SetFloat("_BumpScale",.7f);
                if (!string.IsNullOrEmpty(entry.normal)) material.EnableKeyword("_NORMALMAP"); else material.DisableKeyword("_NORMALMAP");
                material.SetTexture("_MetallicGlossMap",Texture(folder,entry.metalSmooth));
                material.EnableKeyword("_METALLICSPECGLOSSMAP"); material.SetFloat("_Metallic",1); material.SetFloat("_Smoothness",.8f);
                material.SetFloat("_SmoothnessTextureChannel",0);
                material.SetFloat("_Cull",entry.doubleSided?0:2);
                material.SetFloat("_AlphaClip",entry.alphaClip?1:0); material.SetFloat("_Cutoff",.35f);
                if (entry.alphaClip) { material.EnableKeyword("_ALPHATEST_ON"); material.renderQueue=(int)RenderQueue.AlphaTest; material.SetOverrideTag("RenderType","TransparentCutout"); }
                else { material.DisableKeyword("_ALPHATEST_ON"); material.renderQueue=(int)RenderQueue.Geometry; material.SetOverrideTag("RenderType","Opaque"); }
                material.SetTexture("_EmissionMap",Texture(folder,entry.emission));
                // Keep the emission shader variant for combat hit/burn property blocks.
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor",string.IsNullOrEmpty(entry.emission)?Color.black:new Color(.7f,.7f,.7f));
                material.enableInstancing=true;
                EditorUtility.SetDirty(material); materials.Add(entry.name,material);
            }
            var definitionPath=Generated+key+".asset";
            var definition=AssetDatabase.LoadAssetAtPath<CharacterModelDefinition>(definitionPath);
            if (definition==null)
            {
                definition=ScriptableObject.CreateInstance<CharacterModelDefinition>();
                definition.displayName=Names[index]; definition.height=Heights[index]; definition.baseHealth=Health[index]; definition.moveSpeed=Speeds[index];
                definition.rig=index==3?DemonRig.Minotaur:index==4?DemonRig.Scourger:DemonRig.GameRig;
                AssetDatabase.CreateAsset(definition,definitionPath);
            }
            var root=new GameObject(index==0?"FallenAngel":key);
            try
            {
                var art=UnityEngine.Object.Instantiate(source,root.transform); art.name="Imported rig";
                // Unity also infers LOD groups from FBX node suffixes. The explicit
                // prefab group below is authoritative; remove the inferred groups.
                foreach (var inferred in art.GetComponentsInChildren<LODGroup>(true)) UnityEngine.Object.DestroyImmediate(inferred);
                var renderers=art.GetComponentsInChildren<Renderer>(true);
                foreach (var renderer in renderers)
                {
                    var slots=renderer.sharedMaterials;
                    for (var j=0;j<slots.Length;j++)
                    {
                        var name=slots[j]!=null?slots[j].name:null;
                        if (name==null || !materials.TryGetValue(name,out var replacement)) throw new BuildFailedException("Unmapped character material "+key+" / "+name);
                        slots[j]=replacement;
                    }
                    renderer.sharedMaterials=slots;
                    renderer.shadowCastingMode=ShadowCastingMode.On;
                    if (renderer is SkinnedMeshRenderer skin)
                    {
                        skin.updateWhenOffscreen=false; skin.quality=SkinQuality.Bone4;
                        if (skin.bones.Length<3 || skin.bones.Any(b=>b==null)) throw new BuildFailedException("Character skin has missing bones: "+key+" / "+renderer.name);
                    }
                }
                // Normalize once in the asset pipeline, rather than baking 96 meshes
                // every time the main menu opens. Both LODs use the same skeleton.
                var bounds=BoundsOf(renderers);
                var scale=definition.height/Mathf.Max(.001f,bounds.size.y);
                art.transform.localScale*=scale;
                art.transform.localPosition-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z)*scale;
                definition.nearTriangles=TriangleCount(renderers.Where(r=>!spec.lod || r.name.EndsWith("_LOD0",StringComparison.Ordinal)));
                definition.distantTriangles=spec.lod?TriangleCount(renderers.Where(r=>r.name.EndsWith("_LOD1",StringComparison.Ordinal))):0;
                if (spec.lod)
                {
                    var high=renderers.Where(r=>r.name.EndsWith("_LOD0",StringComparison.Ordinal)).ToArray();
                    var low=renderers.Where(r=>r.name.EndsWith("_LOD1",StringComparison.Ordinal)).ToArray();
                    if (high.Length==0 || low.Length!=high.Length) throw new BuildFailedException("Character LODs are incomplete: "+key);
                    var group=root.AddComponent<LODGroup>(); group.fadeMode=LODFadeMode.None;
                    group.SetLODs(new[]{new LOD(.16f,high),new LOD(.007f,low)}); group.RecalculateBounds();
                }
                if (index==0)
                {
                    var animator=art.GetComponent<Animator>();
                    if (animator==null || animator.avatar==null || !animator.avatar.isValid || !animator.avatar.isHuman)
                        throw new BuildFailedException("FallenAngel needs a valid humanoid avatar.");
                }
                definition.prefab=PrefabUtility.SaveAsPrefabAsset(root,Generated+key+".prefab");
                EditorUtility.SetDirty(definition);
                Debug.Log($"SERAPHIM_CHARACTER: {key}; {definition.height:F2}m; {definition.nearTriangles}/{definition.distantTriangles} triangles; {materials.Count} PBR materials.");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
            return definition;
        }
        static void VerifySource(string folder)
        {
            var receiptPath=folder+"source.json";
            if (!File.Exists(receiptPath)) throw new BuildFailedException("Character provenance missing: "+receiptPath);
            var receipt=JsonUtility.FromJson<SourceReceipt>(File.ReadAllText(receiptPath));
            if (receipt.license!="CC-BY-4.0" || string.IsNullOrEmpty(receipt.source) || receipt.originalArchiveSha256?.Length!=64 || receipt.runtimeFiles==null)
                throw new BuildFailedException("Character provenance is incomplete: "+folder);
            foreach (var file in receipt.runtimeFiles)
            {
                if (Path.GetFileName(file.file)!=file.file) throw new BuildFailedException("Invalid derivative receipt path: "+file.file);
                var path=folder+file.file;
                if (!File.Exists(path)) throw new BuildFailedException("Character derivative missing: "+path);
                using (var sha=SHA256.Create()) using (var stream=File.OpenRead(path))
                    if (!string.Equals(BitConverter.ToString(sha.ComputeHash(stream)).Replace("-",""),file.sha256,StringComparison.OrdinalIgnoreCase))
                        throw new BuildFailedException("Character derivative checksum mismatch: "+path+". Re-record intentional asset changes with Tools/Prepare-CharacterModels.ps1 -RecordOnly.");
            }
        }
        static Texture2D Texture(string folder,string file) => string.IsNullOrEmpty(file)?null:AssetDatabase.LoadAssetAtPath<Texture2D>(folder+file)??throw new BuildFailedException("Character texture missing: "+folder+file);
        static int TriangleCount(IEnumerable<Renderer> renderers)
        {
            var count=0;
            foreach (var renderer in renderers)
            {
                var mesh=renderer is SkinnedMeshRenderer skin?skin.sharedMesh:renderer.GetComponent<MeshFilter>()?.sharedMesh;
                if (mesh!=null) for (var i=0;i<mesh.subMeshCount;i++) count+=(int)mesh.GetIndexCount(i)/3;
            }
            return count;
        }
        static Bounds BoundsOf(Renderer[] renderers)
        {
            var bounds=new Bounds(); var first=true;
            foreach (var renderer in renderers)
            {
                if (renderer is SkinnedMeshRenderer skin)
                {
                    var mesh=new Mesh(); skin.BakeMesh(mesh,true);
                    foreach (var vertex in mesh.vertices)
                    {
                        var point=skin.transform.TransformPoint(vertex);
                        if (first) { bounds=new Bounds(point,Vector3.zero); first=false; } else bounds.Encapsulate(point);
                    }
                    UnityEngine.Object.DestroyImmediate(mesh);
                }
                else if (first) { bounds=renderer.bounds; first=false; } else bounds.Encapsulate(renderer.bounds);
            }
            return bounds;
        }
    }
}

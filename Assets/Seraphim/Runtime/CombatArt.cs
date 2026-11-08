using UnityEngine;

namespace Seraphim
{
    public enum DivineDamage { Radiance,WordOfGod,Lance,Chain,Inferno,Burning }
    public enum DemonKind { CinderImp,AshStalker,ObsidianGuard }

    internal static class CombatArt
    {
        public static Mesh DoubleSided(string name,Vector3[] front,Vector2[] frontUv,System.Collections.Generic.List<int> indices)
        {
            var count=front.Length; var vertices=new Vector3[count*2]; var uv=new Vector2[count*2];
            System.Array.Copy(front,vertices,count); System.Array.Copy(front,0,vertices,count,count);
            System.Array.Copy(frontUv,uv,count); System.Array.Copy(frontUv,0,uv,count,count);
            var triangles=new int[indices.Count*2];
            for (var i=0;i<indices.Count;i+=3)
            {
                triangles[i]=indices[i]; triangles[i+1]=indices[i+1]; triangles[i+2]=indices[i+2];
                triangles[indices.Count+i]=indices[i]+count; triangles[indices.Count+i+1]=indices[i+2]+count; triangles[indices.Count+i+2]=indices[i+1]+count;
            }
            var mesh=new Mesh {name=name,vertices=vertices,uv=uv,triangles=triangles};
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
        public static Mesh CreateCone()
        {
            const int sides=8;
            var vertices=new Vector3[sides*3];
            var triangles=new int[vertices.Length];
            for (var i=0;i<sides;i++)
            {
                var a=i*Mathf.PI*2/sides; var b=(i+1)*Mathf.PI*2/sides;
                vertices[i*3]=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                vertices[i*3+1]=Vector3.up;
                vertices[i*3+2]=new Vector3(Mathf.Cos(b),0,Mathf.Sin(b));
                triangles[i*3]=i*3; triangles[i*3+1]=i*3+1; triangles[i*3+2]=i*3+2;
            }
            var mesh=new Mesh {name="Obsidian horn",vertices=vertices,triangles=triangles};
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
        public static Transform Spike(Transform parent,Vector3 from,Vector3 to,float radius,Material material,Mesh mesh)
        {
            var obj=new GameObject("Horn / claw"); obj.transform.SetParent(parent,false);
            obj.transform.localPosition=from;
            obj.transform.localRotation=Quaternion.FromToRotation(Vector3.up,(to-from).normalized);
            obj.transform.localScale=new Vector3(radius,Vector3.Distance(from,to),radius);
            obj.AddComponent<MeshFilter>().sharedMesh=mesh;
            obj.AddComponent<MeshRenderer>().sharedMaterial=material;
            return obj.transform;
        }
        public static ParticleSystem Motes(Transform parent,Material material,int max,Color color,float speed,float lifetime)
        {
            var ps=new GameObject("Combat motes").AddComponent<ParticleSystem>(); ps.transform.SetParent(parent,false);
            ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main; main.playOnAwake=false; main.loop=false; main.maxParticles=max;
            main.startSpeed=new ParticleSystem.MinMaxCurve(speed*.4f,speed);
            main.startSize=new ParticleSystem.MinMaxCurve(.045f,.14f);
            main.startLifetime=new ParticleSystem.MinMaxCurve(lifetime*.5f,lifetime);
            main.startColor=color; main.simulationSpace=ParticleSystemSimulationSpace.World;
            var emission=ps.emission; emission.enabled=false;
            var shape=ps.shape; shape.shapeType=ParticleSystemShapeType.Sphere; shape.radius=.3f;
            var fade=ps.colorOverLifetime; fade.enabled=true;
            var gradient=new Gradient();
            gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(color,1)},
                new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(0,1)});
            fade.color=gradient; ps.GetComponent<ParticleSystemRenderer>().sharedMaterial=material;
            return ps;
        }
        public static float SegmentDistance(Vector3 point,Vector3 start,Vector3 end)
        {
            point.y=start.y=end.y=0;
            var delta=end-start; var t=delta.sqrMagnitude<.0001f?0:Mathf.Clamp01(Vector3.Dot(point-start,delta)/delta.sqrMagnitude);
            return Vector3.Distance(point,start+delta*t);
        }
    }
}

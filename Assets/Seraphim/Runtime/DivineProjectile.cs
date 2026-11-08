using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Seraphim
{
    public sealed class DivineProjectile : MonoBehaviour
    {
        EnemyDirector enemies;
        DivinePowers owner;
        TrailRenderer trail;
        Vector3 direction;
        float age,damage;
        readonly HashSet<int> hit=new HashSet<int>();
        public bool IsFlying { get; private set; }
        public void Initialize(DivinePowers powers,EnemyDirector targets,PrototypeModels art)
        {
            owner=powers; enemies=targets;
            CombatArt.Spike(transform,new Vector3(0,0,-.4f),new Vector3(0,0,1.15f),.12f,art.gold,enemies.ConeMesh);
            var shaft=new GameObject("Laser shaft").AddComponent<LineRenderer>();
            shaft.transform.SetParent(transform,false); shaft.useWorldSpace=false; shaft.positionCount=2;
            shaft.SetPosition(0,new Vector3(0,0,-1.4f)); shaft.SetPosition(1,new Vector3(0,0,1.05f));
            shaft.widthMultiplier=.08f; shaft.sharedMaterial=powers.WhiteMaterial;
            trail=gameObject.AddComponent<TrailRenderer>(); trail.sharedMaterial=art.glowCyan;
            trail.time=.3f; trail.minVertexDistance=.12f; trail.widthMultiplier=.18f;
            trail.startColor=new Color(.8f,1,1,.9f); trail.endColor=new Color(.15f,.6f,1,0);
            trail.emitting=false;
            gameObject.SetActive(false);
        }
        public void Launch(Vector3 origin,Vector3 forward,float amount)
        {
            damage=amount; age=0; hit.Clear(); direction=forward.normalized;
            transform.position=origin; transform.rotation=Quaternion.LookRotation(direction);
            gameObject.SetActive(true); trail.Clear(); trail.emitting=true; IsFlying=true;
        }
        void Update()
        {
            if (!IsFlying) return;
            age+=Time.deltaTime;
            var from=transform.position; var to=from+direction*(24*Time.deltaTime);
            var groundFrom=from; groundFrom.y=0; var groundTo=to; groundTo.y=0;
            var blocked=NavMesh.Raycast(groundFrom,groundTo,out var boundary,NavMesh.AllAreas);
            if (blocked) to=new Vector3(boundary.position.x,from.y,boundary.position.z);
            var hits=enemies.Segment(from,to,.45f,damage,hit);
            if (hits>0) owner.SparksAt(to,22,new Color(.65f,.95f,1));
            transform.position=to;
            if (age>1.25f || blocked) { if (blocked) owner.SparksAt(to,18,new Color(.7f,.9f,1)); Stop(); }
        }
        void Stop() { IsFlying=false; trail.emitting=false; gameObject.SetActive(false); }
        public void ResetProjectile() { Stop(); trail.Clear(); hit.Clear(); }
    }
}

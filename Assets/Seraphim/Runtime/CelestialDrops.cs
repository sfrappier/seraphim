using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Seraphim
{
    public enum DropKind { Life,Spirit }
    public sealed class CelestialDrops : MonoBehaviour
    {
        public sealed class Orb
        {
            public Transform root,visual;
            public Vector3 position;
            public DropKind kind;
            public float spawned;
            public bool Active => root.gameObject.activeSelf;
        }
        SanctuaryGame game;
        readonly List<Orb> pool=new List<Orb>();
        Material lifeMaterial,spiritMaterial;
        ParticleSystem collection;
        public IReadOnlyList<Orb> Orbs => pool;
        public int ActiveCount { get { var count=0; foreach (var orb in pool) if (orb.Active) count++; return count; } }
        public int Collected { get; private set; }
        public string LastPickup { get; private set; }="";
        public float LastPickupAt { get; private set; }=-10;
        public void Initialize(SanctuaryGame owner)
        {
            game=owner;
            lifeMaterial=new Material(game.models.glowGold); lifeMaterial.SetColor("_BaseColor",new Color(2,.35f,.15f));
            spiritMaterial=new Material(game.models.glowCyan);
            collection=CombatArt.Motes(transform,game.models.celestialMote,180,Color.white,2,.6f);
        }
        public void SpawnPair(Vector3 point)
        {
            Spawn(point+Vector3.left*.42f,DropKind.Life); Spawn(point+Vector3.right*.42f,DropKind.Spirit);
        }
        public Orb Spawn(Vector3 point,DropKind kind)
        {
            if (!NavMesh.SamplePosition(point,out var hit,1,NavMesh.AllAreas)) return null;
            Orb orb=null;
            foreach (var item in pool) if (!item.Active && item.kind==kind) { orb=item; break; }
            if (orb==null)
            {
                if (pool.Count>=48)
                {
                    foreach (var item in pool) if (item.kind==kind && (orb==null || item.spawned<orb.spawned)) orb=item;
                    if (orb==null) return null;
                }
                else
                {
                    orb=new Orb {kind=kind,root=new GameObject(kind+" restoration orb").transform};
                    orb.root.SetParent(transform,false);
                    orb.visual=new GameObject("Hovering light").transform; orb.visual.SetParent(orb.root,false);
                    var mat=kind==DropKind.Life?lifeMaterial:spiritMaterial;
                    SceneArt.Shape(PrimitiveType.Sphere,"Orb core",orb.visual,Vector3.zero,Vector3.one*.28f,mat);
                    var ring=SceneArt.Ring("Orb orbit",orb.visual,.24f,.018f,mat,32); ring.transform.localRotation=Quaternion.Euler(55,0,0);
                    pool.Add(orb);
                }
            }
            orb.position=hit.position; orb.spawned=Time.time; orb.root.gameObject.SetActive(true); Position(orb); return orb;
        }
        void Position(Orb orb)
        {
            orb.root.position=orb.position;
            orb.visual.localPosition=Vector3.up*(.43f+Mathf.Sin(Time.time*3+orb.spawned)*.1f);
            orb.visual.localRotation=Quaternion.Euler(0,Time.time*65,0);
        }
        public void Clear() { foreach (var orb in pool) orb.root.gameObject.SetActive(false); }
        void Update()
        {
            if (game==null) return;
            foreach (var orb in pool)
            {
                if (!orb.Active) continue;
                if (Time.time-orb.spawned>45) { orb.root.gameObject.SetActive(false); continue; }
                var player=game.Player.transform.position; player.y=orb.position.y;
                var distance=Vector3.Distance(player,orb.position);
                var needs=orb.kind==DropKind.Life?game.Vitals.Life<game.Vitals.MaxLife-.05f:game.Vitals.Spirit<game.Vitals.MaxSpirit-.05f;
                if (needs && distance<3 && Time.time-orb.spawned>.3f && !NavMesh.Raycast(orb.position,player,out _,NavMesh.AllAreas))
                {
                    if (distance<1.15f) Collect(orb);
                    else orb.position=Vector3.MoveTowards(orb.position,player,Time.deltaTime*5);
                }
                Position(orb);
            }
        }
        void Collect(Orb orb)
        {
            float amount;
            if (orb.kind==DropKind.Life)
            {
                amount=Mathf.Min(game.settings.lifeDropRestore,game.Vitals.MaxLife-game.Vitals.Life); game.Vitals.RestoreLife(amount);
            }
            else { amount=Mathf.Min(game.settings.spiritDropRestore,game.Vitals.MaxSpirit-game.Vitals.Spirit); game.Vitals.RestoreSpirit(amount); }
            Collected++; LastPickupAt=Time.time; LastPickup="+"+Mathf.CeilToInt(amount)+" "+orb.kind;
            game.Audio?.Play(orb.kind==DropKind.Life?SoundCue.Life:SoundCue.Spirit,game.Player.transform.position);
            collection.transform.position=game.Player.transform.position+Vector3.up*.7f;
            var main=collection.main; main.startColor=orb.kind==DropKind.Life?new Color(1,.65f,.3f):new Color(.3f,.85f,1); collection.Emit(28);
            orb.root.gameObject.SetActive(false);
        }
        void OnDestroy() { if (lifeMaterial!=null) Destroy(lifeMaterial); if (spiritMaterial!=null) Destroy(spiritMaterial); }
    }
}

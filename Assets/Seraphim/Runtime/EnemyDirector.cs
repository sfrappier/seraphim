using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Seraphim
{
    public sealed class EnemyDirector : MonoBehaviour
    {
        readonly List<DemonEnemy> pool=new List<DemonEnemy>();
        System.Random random;
        int serial;
        float nextSpawn;
        public SanctuaryGame Game { get; private set; }
        public Material[] BodyMaterials { get; private set; }
        public Material[] EyeMaterials { get; private set; }
        public Material HornMaterial { get; private set; }
        public Mesh ConeMesh { get; private set; }
        public bool SpawnAutomatic { get; set; }
        public int KillCount { get; private set; }
        public int Score { get; private set; }
        public float TotalDamage { get; private set; }
        public float LastRecoveryAt { get; private set; }=-10;
        public IReadOnlyList<DemonEnemy> Enemies => pool;
        public int ActiveCount { get { var count=0; foreach (var enemy in pool) if (enemy.IsAlive) count++; return count; } }
        public int ActiveSpecialAttacks { get { var count=0; foreach (var enemy in pool) if (enemy.IsAlive && enemy.Offense.SpecialActive) count++; return count; } }
        public int PopulationLimit => Game.Attract!=null && Game.Attract.IsRunning?Game.settings.menuEnemyLimit:Game.settings.enemyLimit;
        public int SpecialAttackLimit => Game.Attract!=null && Game.Attract.IsRunning?8:Game.settings.maxConcurrentSpecialAttacks;
        public int PoolCapacity => (PopulationLimit+4)*3+1;

        public void Initialize(SanctuaryGame game,bool spawn)
        {
            Game=game; random=new System.Random(); ConeMesh=CombatArt.CreateCone();
            BodyMaterials=new Material[3]; EyeMaterials=new Material[3];
            var tones=new[]{new Color(.18f,.08f,.075f),new Color(.13f,.08f,.23f),new Color(.09f,.13f,.16f)};
            var eyes=new[]{new Color(3,.7f,.12f),new Color(1.6f,.3f,3),new Color(3,1.5f,.15f)};
            for (var i=0;i<3;i++)
            {
                BodyMaterials[i]=new Material(game.models.ivory) {name="Demon obsidian "+i};
                BodyMaterials[i].SetColor("_BaseColor",tones[i]); BodyMaterials[i].SetFloat("_Metallic",.3f);
                BodyMaterials[i].EnableKeyword("_EMISSION"); BodyMaterials[i].SetColor("_EmissionColor",tones[i]*.25f);
                EyeMaterials[i]=new Material(game.models.celestialMote); EyeMaterials[i].SetColor("_BaseColor",eyes[i]);
            }
            HornMaterial=new Material(game.models.ivory); HornMaterial.SetColor("_BaseColor",new Color(.32f,.29f,.25f));
            SpawnAutomatic=spawn;
            if (spawn) Populate();
            nextSpawn=Time.time+game.settings.enemySpawnInterval;
        }
        public void Populate()
        {
            for (var i=0;i<Mathf.Min(Game.settings.initialEnemies,Game.settings.enemyLimit);i++) SpawnRandom((DemonKind)(i%3));
        }
        public DemonEnemy SpawnRandom(DemonKind? preferred=null,int wave=0)
        {
            if (ActiveCount>=PopulationLimit) return null;
            for (var attempt=0;attempt<30;attempt++)
            {
                var angle=(float)random.NextDouble()*Mathf.PI*2;
                var radius=10+(float)random.NextDouble()*8;
                var candidate=Game.Player.transform.position+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*radius;
                if (Mathf.Abs(candidate.x)>Game.settings.cathedralSize.x/2-3 || Mathf.Abs(candidate.z)>Game.settings.cathedralSize.y/2-3) continue;
                if (!NavMesh.SamplePosition(candidate,out var sample,1,NavMesh.AllAreas)) continue;
                var path=new NavMeshPath();
                if (!NavMesh.CalculatePath(sample.position,Game.Player.transform.position,NavMesh.AllAreas,path) || path.status!=NavMeshPathStatus.PathComplete) continue;
                return SpawnAt(sample.position,preferred??(DemonKind)random.Next(3),false,wave);
            }
            return null;
        }
        public DemonEnemy SpawnAt(Vector3 point,DemonKind kind,bool passive=false,int wave=0)
        {
            if (Game==null || ActiveCount>=PopulationLimit || !NavMesh.SamplePosition(point,out var sample,.8f,NavMesh.AllAreas)) return null;
            DemonEnemy enemy=null;
            foreach (var item in pool) if (!item.IsBoss && !item.gameObject.activeSelf && item.Kind==kind) { enemy=item; break; }
            if (enemy==null)
            {
                var sameKind=0;
                foreach (var item in pool) if (!item.IsBoss && item.Kind==kind) sameKind++;
                if (sameKind>=PopulationLimit+4) return null;
                enemy=new GameObject("Demon · "+kind).AddComponent<DemonEnemy>(); enemy.transform.SetParent(transform,false);
                enemy.Initialize(this,kind); pool.Add(enemy);
            }
            return enemy.Activate(sample.position,++serial,passive,wave)?enemy:null;
        }
        public DemonEnemy SpawnBoss(Vector3 point)
        {
            if (!NavMesh.SamplePosition(point,out var sample,3,NavMesh.AllAreas)) return null;
            var enemy=pool.Find(item=>item.IsBoss);
            if (enemy==null)
            {
                enemy=new GameObject("Vorath · final boss").AddComponent<DemonEnemy>(); enemy.transform.SetParent(transform,false);
                enemy.Initialize(this,DemonKind.ObsidianGuard,true); pool.Add(enemy);
            }
            return enemy.Activate(sample.position,++serial,true,10)?enemy:null;
        }
        public void DespawnAll() { foreach (var enemy in pool) enemy.ReturnToPool(); }
        public void TrimShowcasePool()
        {
            var retained=new int[3];
            for (var i=pool.Count-1;i>=0;i--)
            {
                var enemy=pool[i];
                if (enemy.IsBoss) continue;
                if (++retained[(int)enemy.Kind]<=8) continue;
                pool.RemoveAt(i); Destroy(enemy.gameObject);
            }
        }
        public void RecordDamage(float amount) { TotalDamage+=amount; }
        public void ResetScore() { Score=0; KillCount=0; }
        public void RecordKill(DemonEnemy enemy)
        {
            KillCount++; Score+=enemy.IsBoss?5000:100+(int)enemy.Kind*25+Mathf.Max(0,enemy.WaveNumber-1)*10;
            Game.Audio?.Play(SoundCue.Banish,enemy.transform.position);
            Game.Drops.SpawnPair(enemy.transform.position);
            if (enemy.IsBoss) Game.Boss.Defeated(); else Game.Waves.RecordSlay(enemy);
        }
        public DemonEnemy Nearest(Vector3 origin,float range,HashSet<int> excluded=null)
        {
            DemonEnemy closest=null; var best=range*range;
            foreach (var enemy in pool)
            {
                if (!enemy.IsAlive || (excluded!=null && excluded.Contains(enemy.TargetId))) continue;
                var distance=(enemy.transform.position-origin).sqrMagnitude;
                if (distance<best) { best=distance; closest=enemy; }
            }
            return closest;
        }
        public DemonEnemy AimTarget(Vector3 origin,Vector3 aim,float range)
        {
            DemonEnemy closest=null; var best=float.MaxValue;
            foreach (var enemy in pool)
            {
                if (!enemy.IsAlive || Vector3.Distance(origin,enemy.transform.position)>range) continue;
                var distance=(enemy.transform.position-aim).sqrMagnitude;
                if (distance<best) { closest=enemy; best=distance; }
            }
            return closest;
        }
        public int Radial(Vector3 origin,float radius,float damage,DivineDamage source,HashSet<int> hit=null,float push=0,float stun=0,float burn=0)
        {
            var count=0;
            foreach (var enemy in pool)
            {
                if (!enemy.IsAlive || (hit!=null && hit.Contains(enemy.TargetId))) continue;
                var offset=enemy.transform.position-origin; offset.y=0;
                if (offset.sqrMagnitude>(radius+enemy.HitRadius)*(radius+enemy.HitRadius)) continue;
                hit?.Add(enemy.TargetId);
                enemy.Hit(damage,source,origin,push,stun,burn); count++;
            }
            return count;
        }
        public int Segment(Vector3 from,Vector3 to,float radius,float damage,HashSet<int> hit)
        {
            var count=0;
            foreach (var enemy in pool)
            {
                if (!enemy.IsAlive || hit.Contains(enemy.TargetId) || CombatArt.SegmentDistance(enemy.transform.position,from,to)>radius+enemy.HitRadius) continue;
                hit.Add(enemy.TargetId); enemy.Hit(damage,DivineDamage.Lance,from,1.2f,.18f); count++;
            }
            return count;
        }
        void Update()
        {
            if (Game==null) return;
            if (Game.Vitals.Life<=0)
            {
                Game.Boss.Clear();
                DespawnAll(); Game.Ultimate.Cancel(); Game.Player.StopKeyboardMovement();
                Game.Player.Agent.ResetPath(); Game.Player.Agent.Warp(Game.settings.spawnPoint);
                Game.Vitals.Revive(); LastRecoveryAt=Time.time; nextSpawn=Time.time+4;
                Game.Drops.Clear(); Game.Waves.Recover();
            }
            if (!SpawnAutomatic || Time.time<nextSpawn) return;
            nextSpawn=Time.time+Game.settings.enemySpawnInterval*(.85f+(float)random.NextDouble()*.3f);
            SpawnRandom();
        }
        void OnDestroy()
        {
            if (BodyMaterials!=null) foreach (var mat in BodyMaterials) Destroy(mat);
            if (EyeMaterials!=null) foreach (var mat in EyeMaterials) Destroy(mat);
            if (HornMaterial!=null) Destroy(HornMaterial);
            if (ConeMesh!=null) Destroy(ConeMesh);
        }
    }
}

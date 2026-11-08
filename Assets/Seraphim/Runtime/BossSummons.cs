using UnityEngine;
using UnityEngine.AI;

namespace Seraphim
{
    public sealed class BossSummons : MonoBehaviour
    {
        SanctuaryGame game;
        CathedralBoss boss;
        float nextWave=float.PositiveInfinity;
        int sequence;
        public bool AutoReplenish { get; set; }=true;
        public int TotalSummoned { get; private set; }
        public int ActiveCount
        {
            get { var count=0; foreach (var enemy in game.Enemies.Enemies) if (enemy.IsAlive && enemy.IsBossSummon) count++; return count; }
        }
        public int Limit => boss.Phase>=3?game.settings.bossPhaseThreeSummonLimit:game.settings.bossPhaseTwoSummonLimit;
        public void Initialize(SanctuaryGame world,CathedralBoss owner) { game=world; boss=owner; }
        public void ResetForEncounter() { Clear(); AutoReplenish=true; TotalSummoned=sequence=0; }
        public void EnterPhase(int phase)
        {
            if (phase>=3)
                foreach (var enemy in game.Enemies.Enemies) if (enemy.IsAlive && enemy.IsBossSummon)
                    enemy.GrowTo(game.settings.bossSummonScale,game.settings.bossTransformationSeconds);
            SpawnWave(); nextWave=Time.time+(phase==3?6:8);
        }
        public int SpawnWave()
        {
            if (!boss.IsAlive || boss.Phase<2) return 0;
            var count=boss.Phase==3?4:3; var spawned=0;
            for (var i=0;i<count && ActiveCount<Limit && game.Enemies.ActiveCount<game.Enemies.PopulationLimit;i++)
            {
                for (var attempt=0;attempt<20;attempt++)
                {
                    var angle=(sequence*137.5f+attempt*37)*Mathf.Deg2Rad;
                    var center=boss.Enemy.transform.position;
                    var candidate=center+new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle))*(5+attempt%3);
                    if (Mathf.Abs(candidate.x)>game.settings.cathedralSize.x/2-3 || Mathf.Abs(candidate.z)>game.settings.cathedralSize.y/2-3
                        || Vector3.Distance(candidate,game.Player.transform.position)<3 || !NavMesh.SamplePosition(candidate,out var point,1,NavMesh.AllAreas)) continue;
                    var path=new NavMeshPath();
                    if (!NavMesh.CalculatePath(point.position,game.Player.transform.position,NavMesh.AllAreas,path) || path.status!=NavMeshPathStatus.PathComplete) continue;
                    var kind=sequence%4==3?DemonKind.ObsidianGuard:sequence%4==2?DemonKind.AshStalker:DemonKind.CinderImp;
                    var enemy=game.Enemies.SpawnAt(point.position,kind,false,8);
                    if (enemy==null) continue;
                    enemy.MarkBossSummon();
                    if (boss.Phase>=3) enemy.GrowTo(game.settings.bossSummonScale,game.settings.bossTransformationSeconds);
                    sequence++; spawned++; TotalSummoned++; break;
                }
            }
            if (spawned>0) game.Audio.Play(SoundCue.Charge,boss.Enemy.transform.position);
            return spawned;
        }
        public void Clear()
        {
            if (game!=null) foreach (var enemy in game.Enemies.Enemies) if (enemy.IsBossSummon) enemy.ReturnToPool();
            nextWave=float.PositiveInfinity;
        }
        void Update()
        {
            if (!AutoReplenish || !boss.IsAlive || boss.Phase<2 || boss.IsTransitioning || Time.time<nextWave) return;
            SpawnWave(); nextWave=Time.time+(boss.Phase>=3?6:8);
        }
    }
}

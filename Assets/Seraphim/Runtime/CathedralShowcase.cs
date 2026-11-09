using UnityEngine;
using UnityEngine.AI;

namespace Seraphim
{
    public sealed class CathedralShowcase : MonoBehaviour
    {
        SanctuaryGame game;
        // Fixed destinations, rather than a moving target the hero can catch up to.
        // Paths still come from the cathedral's baked navigation, including its altar.
        static readonly Vector3[] destinations = {
            new Vector3(-7,0,-30), new Vector3(15,0,-16),
            new Vector3(7,0,20), new Vector3(-7,0,33),
            new Vector3(-15,0,12), new Vector3(7,0,-7),
            new Vector3(-15,0,-24), new Vector3(15,0,31)
        };
        readonly NavMeshPath path=new NavMeshPath();
        readonly Vector3[] corners=new Vector3[32];
        readonly float[] castAfter=new float[5];
        readonly int[] casts=new int[5];
        float nextFill,nextCast,progressAt;
        float savedSpeed;
        int savedPriority,goal,corner,cornerCount,castSlot;
        ObstacleAvoidanceType savedAvoidance;
        Vector3 progressPosition;
        public bool IsRunning { get; private set; }
        public int AutomatedCasts { get; private set; }
        public int CompletedRuns { get; private set; }
        public int NavigationRecoveries { get; private set; }
        public int CastsFor(int slot) => casts[slot];
        public void Initialize(SanctuaryGame owner) { game=owner; }
        public void Begin()
        {
            if (IsRunning) End();
            game.ResetPlayableWorld(); IsRunning=true; nextFill=0; AutomatedCasts=0;
            CompletedRuns=0; NavigationRecoveries=0; goal=-1; cornerCount=0; castSlot=0;
            var agent=game.Player.Agent;
            savedSpeed=agent.speed; savedPriority=agent.avoidancePriority; savedAvoidance=agent.obstacleAvoidanceType;
            agent.speed=game.settings.menuMoveSpeed;
            // The showcase runs through a dense crowd without yielding to every demon.
            // Static obstacles are still swept by SeraphimMotor and the path corners.
            agent.avoidancePriority=0; agent.obstacleAvoidanceType=ObstacleAvoidanceType.NoObstacleAvoidance;
            progressPosition=game.Player.transform.position; progressAt=Time.time;
            for (var slot=0;slot<5;slot++) { casts[slot]=0; castAfter[slot]=Time.time+slot*.28f; }
            nextCast=Time.time;
            SelectNextRun();
            game.Vitals.PresentationMode=true;
            Camera.main.GetComponent<SanctuaryCamera>().ShowcaseZoom=8;
            for (var i=0;i<72;i++) game.Enemies.SpawnRandom(i%12==11?DemonKind.RiftScourger:(DemonKind)(i%3),10);
        }
        public void End()
        {
            if (!IsRunning) return;
            game.Player.StopKeyboardMovement();
            var agent=game.Player.Agent;
            agent.speed=savedSpeed; agent.avoidancePriority=savedPriority; agent.obstacleAvoidanceType=savedAvoidance;
            IsRunning=false; Camera.main.GetComponent<SanctuaryCamera>().ShowcaseZoom=0;
            game.ResetPlayableWorld(); game.Enemies.TrimShowcasePool();
        }
        bool SelectNextRun()
        {
            var agent=game.Player.Agent;
            for (var attempt=0;attempt<destinations.Length;attempt++)
            {
                goal=(goal+1)%destinations.Length;
                if (!NavMesh.SamplePosition(destinations[goal],out var hit,2,agent.areaMask)
                    || (hit.position-game.Player.transform.position).sqrMagnitude<36
                    || !agent.CalculatePath(hit.position,path) || path.status!=NavMeshPathStatus.PathComplete) continue;
                cornerCount=path.GetCornersNonAlloc(corners); corner=1;
                if (cornerCount>1 && cornerCount<corners.Length) return true;
            }
            cornerCount=0; return false;
        }
        void RunAcrossCathedral()
        {
            var position=game.Player.transform.position;
            // Recover if crowd displacement or a corner ever prevents actual progress.
            if (Time.time-progressAt>=.65f)
            {
                if ((position-progressPosition).sqrMagnitude<1) { NavigationRecoveries++; SelectNextRun(); }
                progressAt=Time.time; progressPosition=position;
            }
            if (cornerCount<2 && !SelectNextRun()) return;
            var threshold=Mathf.Max(.9f,game.Player.Agent.speed*Time.deltaTime*1.5f);
            while (corner<cornerCount && Vector3.Distance(position,corners[corner])<threshold) corner++;
            if (corner>=cornerCount)
            {
                CompletedRuns++;
                if (!SelectNextRun()) return;
            }
            // Look ahead only when the next segment clears solid architecture.
            while (corner+1<cornerCount && !game.Player.Agent.Raycast(corners[corner+1],out _)) corner++;
            var direction=corners[corner]-position; direction.y=0; direction.Normalize();
            var forward=game.Player.KeyboardDirection(Vector2.up); var right=game.Player.KeyboardDirection(Vector2.right);
            game.Player.MoveWithKeyboard(new Vector2(Vector3.Dot(direction,right),Vector3.Dot(direction,forward)),Time.deltaTime);
        }
        void Update()
        {
            if (!IsRunning) return;
            if (Time.time>=nextFill)
            {
                nextFill=Time.time+.25f;
                // Reuse stragglers behind the camera so each new run still has a battle.
                var recycled=0;
                foreach (var demon in game.Enemies.Enemies)
                    if (demon.IsAlive && !demon.IsBoss && (demon.transform.position-game.Player.transform.position).sqrMagnitude>38*38)
                    {
                        demon.ReturnToPool();
                        if (++recycled==8) break;
                    }
                for (var i=0;i<8 && game.Enemies.ActiveCount<game.settings.menuEnemyLimit;i++) game.Enemies.SpawnRandom(null,10);
            }
            RunAcrossCathedral();
            if (Time.time<nextCast) return;
            var enemy=game.Enemies.Nearest(game.Player.transform.position,28);
            var aim=enemy!=null?enemy.transform.position:game.Player.transform.position+game.Player.transform.forward*20;
            for (var offset=0;offset<5;offset++)
            {
                var slot=(castSlot+offset)%5;
                if (Time.time<castAfter[slot] || game.Skills.Remaining(slot)>0 || !game.Skills.TryUseDirected(slot,aim)) continue;
                AutomatedCasts++; casts[slot]++; castSlot=(slot+1)%5;
                nextCast=Time.time+.16f;
                break;
            }
        }
    }
}

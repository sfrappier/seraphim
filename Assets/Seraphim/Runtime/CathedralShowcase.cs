using UnityEngine;

namespace Seraphim
{
    public sealed class CathedralShowcase : MonoBehaviour
    {
        SanctuaryGame game;
        float began,nextFill;
        public bool IsRunning { get; private set; }
        public int AutomatedCasts { get; private set; }
        public void Initialize(SanctuaryGame owner) { game=owner; }
        public void Begin()
        {
            game.ResetPlayableWorld(); IsRunning=true; began=Time.time; nextFill=0; AutomatedCasts=0;
            game.Vitals.PresentationMode=true;
            Camera.main.GetComponent<SanctuaryCamera>().ShowcaseZoom=8;
            for (var i=0;i<72;i++) game.Enemies.SpawnRandom((DemonKind)(i%3),10);
        }
        public void End()
        {
            IsRunning=false; Camera.main.GetComponent<SanctuaryCamera>().ShowcaseZoom=0;
            game.ResetPlayableWorld(); game.Enemies.TrimShowcasePool();
        }
        void Update()
        {
            if (!IsRunning) return;
            if (Time.time>=nextFill)
            {
                nextFill=Time.time+.25f;
                for (var i=0;i<8 && game.Enemies.ActiveCount<game.settings.menuEnemyLimit;i++) game.Enemies.SpawnRandom(null,10);
            }
            var t=Time.time-began;
            var target=new Vector3(Mathf.Sin(t*.24f)*6,0,-6+Mathf.Cos(t*.24f)*5);
            var delta=target-game.Player.transform.position; delta.y=0;
            var forward=game.Player.KeyboardDirection(Vector2.up); var right=game.Player.KeyboardDirection(Vector2.right);
            var direction=delta.normalized;
            game.Player.MoveWithKeyboard(new Vector2(Vector3.Dot(direction,right),Vector3.Dot(direction,forward)),Time.deltaTime);
            var enemy=game.Enemies.Nearest(game.Player.transform.position,28);
            var aim=enemy!=null?enemy.transform.position:game.Player.transform.position+game.Player.transform.forward*20;
            for (var slot=0;slot<5;slot++)
            {
                if (slot==4 && t<.7f) continue;
                if (game.Skills.Remaining(slot)<=0 && game.Skills.TryUseDirected(slot,aim)) AutomatedCasts++;
            }
        }
    }
}

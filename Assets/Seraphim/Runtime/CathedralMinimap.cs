using UnityEngine;
using UnityEngine.UI;

namespace Seraphim
{
    public sealed class CathedralMinimap : HudMeshGraphic
    {
        SanctuaryGame game;
        float refresh;
        public int EnemyMarkerCount { get; private set; }
        public Vector2 PlayerMapPosition => Project(game.Player.transform.position);
        public void Initialize(SanctuaryGame owner) { game=owner; raycastTarget=false; SetVerticesDirty(); }
        float Scale => Mathf.Min((rectTransform.rect.width-28)/(game.settings.cathedralSize.x+2),(rectTransform.rect.height-28)/(game.settings.cathedralSize.y+2));
        Vector2 Project(Vector3 world) => new Vector2(world.x,world.z)*Scale;
        Rect Project(Rect world) => new Rect(world.x*Scale,world.y*Scale,world.width*Scale,world.height*Scale);
        protected override void Draw(VertexHelper vh)
        {
            if (game==null) return;
            var rect=rectTransform.rect; Box(vh,rect,new Color(.015f,.025f,.04f,.97f),new Color(.035f,.055f,.08f,.97f));
            var size=game.settings.cathedralSize;
            Box(vh,Project(new Rect(-size.x/2,-size.y/2,size.x,size.y)),new Color(.2f,.23f,.25f),new Color(.25f,.28f,.3f));
            Box(vh,Project(new Rect(-3.2f,-size.y/2,6.4f,size.y)),new Color(.25f,.38f,.49f),new Color(.25f,.38f,.49f));
            foreach (var obstacle in game.MapObstacles) Box(vh,Project(obstacle),new Color(.55f,.49f,.34f),new Color(.55f,.49f,.34f));
            foreach (var stone in game.Waystones) Diamond(vh,Project(stone.transform.position),2.4f,3.2f,stone.IsAttuned?Pale:new Color(.3f,.85f,1));
            foreach (var orb in game.Drops.Orbs) if (orb.Active) Disc(vh,Project(orb.position),1.1f,orb.kind==DropKind.Life?Gold:new Color(.2f,.7f,1),Color.clear,8);
            EnemyMarkerCount=0;
            foreach (var enemy in game.Enemies.Enemies) if (enemy.IsAlive)
            {
                Disc(vh,Project(enemy.transform.position),enemy.IsBoss?5:2.6f,enemy.IsBoss?new Color(1,.25f,.85f):new Color(1,.18f,.12f),new Color(.8f,.02f,.015f),12); EnemyMarkerCount++;
            }
            var player=PlayerMapPosition; var forward=game.Player.transform.forward; var direction=new Vector2(forward.x,forward.z).normalized;
            var side=new Vector2(-direction.y,direction.x);
            Disc(vh,player,5,new Color(.95f,.8f,.4f,.2f),Color.clear,16);
            Triangle(vh,player+direction*5.5f,player-direction*3+side*3.3f,player-direction*3-side*3.3f,Pale);
            Line(vh,new Vector2(rect.xMin,rect.yMin),new Vector2(rect.xMax,rect.yMin),1.4f,Gold);
            Line(vh,new Vector2(rect.xMax,rect.yMin),new Vector2(rect.xMax,rect.yMax),1.4f,Gold);
            Line(vh,new Vector2(rect.xMax,rect.yMax),new Vector2(rect.xMin,rect.yMax),1.4f,Gold);
            Line(vh,new Vector2(rect.xMin,rect.yMax),new Vector2(rect.xMin,rect.yMin),1.4f,Gold);
        }
        void Update() { if (game!=null && Time.unscaledTime>=refresh) { refresh=Time.unscaledTime+.1f; SetVerticesDirty(); } }
    }
}

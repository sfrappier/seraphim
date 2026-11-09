using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seraphim
{
    public sealed partial class PrototypeSmokeTest
    {
        IEnumerator CharacterModelChecks()
        {
            ResetPressureFixture(new Vector3(0,0,-24));
            var camera=Camera.main; var follow=camera.GetComponent<SanctuaryCamera>();
            var position=camera.transform.position; var rotation=camera.transform.rotation;
            var facing=game.Player.transform.rotation;
            follow.enabled=false;
            game.Player.transform.rotation=Quaternion.Euler(0,180,0);
            camera.transform.position=game.Player.transform.position+new Vector3(2.8f,2.4f,-5.5f);
            camera.transform.LookAt(game.Player.transform.position+Vector3.up*1.1f);
            yield return new WaitForSeconds(.15f);
            var angel=game.Player.GetComponentInChildren<Animator>();
            Check(game.models.playerModel!=null && game.models.character==game.models.playerModel.prefab && angel!=null && angel.isHuman,"Imported FallenAngel uses a valid retargeted humanoid skeleton");
            var playerMaterials=new HashSet<Material>();
            foreach (var renderer in game.Player.transform.Find("Character visual").GetComponentsInChildren<SkinnedMeshRenderer>())
                foreach (var material in renderer.sharedMaterials) if (material!=null) playerMaterials.Add(material);
            Check(playerMaterials.Count>=12,"FallenAngel retains separate authored armor, skin, hair and cloth materials");
            var mapped=true;
            foreach (var material in playerMaterials) mapped &= material.GetTexture("_BaseMap")!=null && material.shader.isSupported;
            Check(mapped,"Every detailed player material has its diffuse texture and a supported shader");
            yield return Capture("33-fallen-angel.png");

            game.Player.Agent.Warp(new Vector3(0,0,-26));
            var roster=new List<DemonEnemy>();
            var unique=new HashSet<GameObject>();
            for (var i=0;i<DemonRoster.KindCount;i++)
            {
                var definition=game.models.demonModels[i]; unique.Add(definition.prefab);
                var enemy=game.Enemies.SpawnAt(new Vector3(-7.5f+i*5,0,-18), (DemonKind)i,true);
                roster.Add(enemy);
                enemy.transform.rotation=Quaternion.Euler(0,180,0);
                Check(enemy!=null && enemy.Presentation.HasAnimatedRig,"Imported demon skeleton drives locomotion: "+definition.displayName);
                var lod=enemy.GetComponentInChildren<LODGroup>();
                Check(lod!=null && lod.lodCount==2 && definition.distantTriangles<definition.nearTriangles*.5f,"Demon has a cheaper distant mesh: "+definition.displayName);
                var bounds=SanctuaryGame.VisualBounds(enemy.Presentation.GetComponentsInChildren<Renderer>());
                Debug.Log($"SERAPHIM_DEMON_BOUNDS: {definition.displayName}: {bounds}; head={enemy.Presentation.HeadPosition}; bar={enemy.BarHeight:F2}");
                Check(bounds.min.y>-.3f && bounds.size.y>1.6f && bounds.size.y<6 && enemy.BarHeight>enemy.Presentation.HeadPosition.y-enemy.transform.position.y,"Demon feet, scale and overhead health bar align: "+definition.displayName);
            }
            Check(unique.Count==DemonRoster.KindCount,"Four demon classes use four distinct imported models");
            camera.transform.position=new Vector3(0,8,-39); camera.transform.LookAt(new Vector3(0,1,-19));
            yield return new WaitForSeconds(.1f); yield return Capture("34-demon-roster.png");
            foreach (var enemy in roster) enemy.Hit(7,DivineDamage.Chain,game.Player.transform.position,0,.1f);
            yield return new WaitForSeconds(.2f);
            foreach (var enemy in roster)
            {
                Check(enemy.HealthBar.IsVisible && enemy.HealthBar.DisplayedFraction<1,"Imported demon overhead bar shows received damage: "+enemy.DisplayName);
                enemy.ReturnToPool();
                var reused=game.Enemies.SpawnAt(enemy.transform.position,enemy.Kind,true);
                Check(reused==enemy && reused.Health==reused.MaxHealth && !reused.IsStunned && !reused.IsBurning,"Imported demon pool reuse resets health and status: "+enemy.DisplayName);
            }
            var walker=roster[0]; Transform thigh=null;
            foreach (var bone in walker.Presentation.GetComponentsInChildren<Transform>()) if (bone.name=="UpperLeg.L") { thigh=bone; break; }
            var oldPose=thigh.localRotation; var oldPosition=walker.transform.position;
            walker.Agent.SetDestination(oldPosition+Vector3.right*3);
            yield return new WaitForSeconds(.3f);
            Check(Vector3.Distance(oldPosition,walker.transform.position)>.2f && Quaternion.Angle(oldPose,thigh.localRotation)>1,"Actual demon navigation animates the imported leg bones");
            game.Enemies.DespawnAll();
            ResetPressureFixture(new Vector3(-5,0,-24));
            var scourger=game.Enemies.SpawnAt(new Vector3(-5,0,-15),DemonKind.RiftScourger,true,10);
            yield return WaitForSpecial(scourger);
            Check(scourger.Offense.TrySpecial(),"Rift Scourger can cast its own telegraphed area attack");
            var locked=scourger.Offense.ThreatPoint;
            game.Player.Agent.Warp(new Vector3(0,0,-24));
            yield return new WaitForSeconds(1);
            Check(scourger.Offense.ThreatPoint==locked && game.Vitals.Life==game.Vitals.MaxLife,"Sidestepping the Scourger's fixed rift warning avoids damage");
            scourger.ReturnToPool();
            scourger=game.Enemies.SpawnAt(new Vector3(0,0,-15),DemonKind.RiftScourger,true,10);
            yield return WaitForSpecial(scourger); scourger.Offense.TrySpecial();
            scourger.Hit(1,DivineDamage.Chain,game.Player.transform.position,0,.5f);
            Check(!scourger.Offense.SpecialActive,"Lightning interrupts the new elite's pending rift attack");
            scourger.ReturnToPool();
            scourger=game.Enemies.SpawnAt(new Vector3(0,0,-15),DemonKind.RiftScourger,true,10);
            yield return WaitForSpecial(scourger); scourger.Offense.TrySpecial();
            yield return new WaitForSeconds(1);
            Check(game.Vitals.Life<game.Vitals.MaxLife && scourger.Offense.DamageApplied>0,"The Scourger's rift burst damages a player who remains in the warning");
            game.Enemies.DespawnAll(); game.Drops.Clear();
            game.Player.Agent.Warp(game.settings.spawnPoint); game.Player.transform.rotation=facing;
            game.Vitals.RestoreLife(game.Vitals.MaxLife);
            camera.transform.SetPositionAndRotation(position,rotation); follow.enabled=true;
        }
    }
}

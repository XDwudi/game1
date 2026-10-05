using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Tidebreak
{
    public partial class SmokePilot
    {
        Vector3 V08TargetAim(EncounterTarget target)
        {
            Physics.SyncTransforms();
            var hull=target.GetComponentsInChildren<Collider>().FirstOrDefault(c=>c.enabled);
            return hull?hull.bounds.center:target.transform.position;
        }
        void V08FireMechanism(BossMechanism m,WeaponKind weapon)
        {
            if(!m.MechanismTarget||!m.TargetVulnerable)return;
            g.Player.Equip(weapon);g.Player.AimAt(V08TargetAim(m.MechanismTarget));g.Player.Fire();
        }
        bool V08WhaleVisualsReset(BossMechanism m)
        {
            if(m.Nodes[0].Root.parent.GetComponentsInChildren<LineRenderer>().Any(line=>line.enabled))return false;
            var expected=new Color(1,.72f,.25f);var block=new MaterialPropertyBlock();
            for(int i=1;i<=3;i++){
                var indicator=m.Nodes[i].Root.GetComponentsInChildren<Renderer>().First(r=>r.name=="Readable action indicator");
                indicator.GetPropertyBlock(block);Color actual=block.GetColor("_Color");
                if(Mathf.Abs(actual.r-expected.r)+Mathf.Abs(actual.g-expected.g)+Mathf.Abs(actual.b-expected.b)>.03f)return false;
            }
            return true;
        }
        void V08FrostRouteVisibility(BossMechanism m)
        {
            var trees=g.World.Scenery.GetComponentsInChildren<Transform>().Where(t=>t.name=="Wind-shaped snow cedar").ToArray();
            bool land=true,clear=true;Vector3 previous=m.Nodes[0].Position;
            foreach(var point in m.RecordedRoute){
                for(int sample=0;sample<=16;sample++){
                    Vector3 p=Vector3.Lerp(previous,point,sample/16f);
                    land&=g.World.GroundAt(p)>=.5f&&g.World.GroundAt(p+new Vector3(-1.4f,0,-1.7f))>=.5f;
                }
                Vector3 view=point+new Vector3(-1.4f,0,-1.7f);view.y=g.World.GroundAt(view)+1.7f;
                Vector3 target=point+new Vector3(2.1f,2,-.25f),direction=target-view;
                foreach(var tree in trees)foreach(var renderer in tree.GetComponentsInChildren<Renderer>()){
                    float entry;if(renderer.enabled&&renderer.bounds.IntersectRay(new Ray(view,direction.normalized),out entry)&&entry<direction.magnitude)clear=false;
                }
                previous=point;
            }
            Check(land&&clear,"v08 all four frost escort legs and actual firing approaches stay on land outside snow-cedar canopy bounds, including non-colliding foliage");
        }

        // These are isolated mechanism contracts, not full encounter victory
        // claims. Fixtures select a phase; every progress action uses the same
        // movement, E and camera-ray firing paths as ordinary gameplay.
        IEnumerator V08BossContracts()
        {
            v05Speed=1;Time.timeScale=v05Speed;
            var boss=V05BossFixture(113);var m=boss.Encounter.Mechanism;WeaponKind weapon=g.Player.weapon;
            m.BeginPhase(3);yield return null;Physics.SyncTransforms();V08FrostRouteVisibility(m);m.BeginPhase(1);yield return null;
            float start=Time.time;
            while(m.Active&&!m.MechanismTarget&&g.State==VoyageState.Combat&&Time.time-start<45){V05Operate(m);yield return null;}
            Check(m.MechanismTarget&&m.StateStep==2&&m.CompletedActions==0,"v08 frost escort arrives and thaws an actual shootable ice lock without granting automatic checkpoint progress");
            if(m.MechanismTarget){
                float approach=Time.time;Vector3 viewPosition=m.Nodes[0].Position+new Vector3(-1.4f,0,-1.7f);
                while(GameDirector.FlatDistance(g.Player.transform.position,viewPosition)>.65f&&Time.time-approach<5){V05Operate(m);yield return null;}
                yield return null;g.Player.AimAt(V08TargetAim(m.MechanismTarget));yield return null;yield return new WaitForEndOfFrame();
                RaycastHit visibility;Vector3 ray=V08TargetAim(m.MechanismTarget)-g.Player.View.transform.position;
                bool reached=Physics.Raycast(g.Player.View.transform.position,ray.normalized,out visibility,ray.magnitude+1,~((1<<9)|(1<<30)));
                File.AppendAllText(Path.Combine(output,"late-target-sightlines.txt"),"Frost captain="+g.Player.transform.position+" furnace="+m.Nodes[0].Position+" target="+m.MechanismTarget.transform.position+" hull="+V08TargetAim(m.MechanismTarget)+" rayHit="+(reached?visibility.collider.name:"none")+"\n");
                Check(GameDirector.FlatDistance(g.Player.transform.position,m.Nodes[0].Position)>1.5f&&reached&&visibility.collider.GetComponentInParent<EncounterTarget>()==m.MechanismTarget,
                    "v08 real walking keeps the captain outside the hot furnace and provides an unobstructed ice-lock weapon sightline");
                V08TargetHud(m.MechanismTarget);CapturePresentation("v08-frost-thawed-lock",1600,900);
                int before=m.CompletedActions;start=Time.time;
                while(m.MechanismTarget&&g.State==VoyageState.Combat&&Time.time-start<8){V05Operate(m);V08FireMechanism(m,weapon);yield return null;}
                Check(m.CompletedActions==before+1,"v08 frost checkpoint only advances after actual camera-ray ice-lock destruction");
            }
            g.StartVoyage();yield return null;

            boss=V05BossFixture(114);m=boss.Encounter.Mechanism;m.BeginPhase(2);yield return null;
            At(m.Nodes[0].Position);yield return new WaitForSeconds(.35f);m.UseAction();
            Check(m.Carrying&&m.ChargeNeedsRelay,"v08 late lightning charge starts with incorrect polarity that requires the relay");
            At(m.Nodes[m.CurrentTargetIndex].Position);yield return new WaitForSeconds(.1f);
            Check(!m.Carrying&&m.CompletedActions==0&&m.Failures>0,"v08 taking unconverted charge directly to the correct ground pad cannot bypass polarity inversion");
            At(m.Nodes[0].Position);yield return new WaitForSeconds(.35f);m.UseAction();
            At(m.Nodes[3].Position);yield return new WaitForSeconds(.35f);m.UseAction();
            Check(m.Carrying&&!m.ChargeNeedsRelay,"v08 relay E action converts the carried charge without granting the entire circuit");
            CapturePresentation("v08-lightning-polarity-relay",1600,900);
            At(m.Nodes[m.CurrentTargetIndex].Position);yield return new WaitForSeconds(.1f);
            Check(m.CompletedActions==1&&!m.Carrying,"v08 converted charge grounds successfully and preserves a completed circuit");
            g.StartVoyage();yield return null;

            boss=V05BossFixture(115);m=boss.Encounter.Mechanism;weapon=g.Player.weapon;
            // Deliberately overcool with ordinary valve presses, after relieving
            // pressure. The guide must say to wait rather than lower it further.
            At(m.Nodes[1].Position);yield return new WaitForSeconds(.35f);m.UseAction();
            At(m.Nodes[0].Position);
            for(int tap=0;tap<3;tap++){yield return new WaitForSeconds(1.1f);m.UseAction();}
            yield return null;
            Check(m.Heat<25&&m.Pressure<65&&m.LateInput.Contains("停止冷却")&&m.TargetLabel.Contains("停止冷却"),"v08 overcooling through actual valve inputs points to waiting for reheating, never another cooling press");
            start=Time.time;
            while(!m.MechanismTarget&&g.State==VoyageState.Combat&&Time.time-start<35){V05Operate(m);yield return null;}
            Check(m.MechanismTarget&&m.CompletedActions==0,"v08 balanced forge exposes a real brittle plate but waiting on meter values grants no armor break");
            if(m.MechanismTarget){
                yield return null;g.Player.AimAt(V08TargetAim(m.MechanismTarget));yield return null;yield return new WaitForEndOfFrame();
                Check(m.MechanismTarget.GetComponentsInChildren<Collider>().Count(c=>c.enabled)==1,"v08 sculpted brittle armor keeps precisely one unobstructed gameplay aiming hull");
                V08TargetHud(m.MechanismTarget);CapturePresentation("v08-cinder-brittle-plate",1600,900);start=Time.time;
                while(m.MechanismTarget&&g.State==VoyageState.Combat&&Time.time-start<12){V05Operate(m);V08FireMechanism(m,weapon);yield return null;}
                Check(m.CompletedActions==1&&m.Heat>=60,"v08 actual plate fire completes one quench and resets heat for a distinct new valve cycle");
            }
            g.StartVoyage();yield return null;

            boss=V05BossFixture(116);m=boss.Encounter.Mechanism;m.BeginPhase(2);yield return null;
            At(m.Nodes[0].Position);yield return new WaitForSeconds(.35f);m.UseAction();start=Time.time;
            while(m.StateStep==1&&g.State==VoyageState.Combat&&Time.time-start<25){V05Operate(m);yield return null;}
            Check(m.StateStep==2&&m.RecordedRoute.Count==m.RequiredActions&&m.FalseFootprints.Count>0,"v08 mirror records the player-authored route and generates spatially separate false reflections in later phases");
            Check(m.FalseFootprints.All(p=>m.RecordedRoute.All(real=>GameDirector.FlatDistance(p,real)>=2.8f)),"v08 no false reflection overlaps a required real footprint");
            start=Time.time;
            while(!m.MemoryHasDeparted&&g.State==VoyageState.Combat&&Time.time-start<8){V05Operate(m);yield return null;}
            Check(m.MemoryHasDeparted&&m.CompletedActions==0,"v08 normal walking reaches the departure threshold before its navigation marker arrival tolerance, with no teleport or free collection");
            start=Time.time;
            while(m.CompletedActions==0&&g.State==VoyageState.Combat&&Time.time-start<8){V05Operate(m);yield return null;}
            Check(m.CompletedActions==1,"v08 normal walking folds back to the final recorded step and collects it in the correct reverse order");
            if(m.FalseFootprints.Count>0){
                int remembered=m.CompletedActions;
                At(m.FalseFootprints[0]);yield return new WaitForSeconds(.15f);
                Check(m.CompletedActions==remembered&&m.MemoryLockRemaining>0&&m.Failures>0,"v08 stepping into a false single-crystal reflection triggers a recoverable mirror-blade trap without collecting a true footprint");
                CapturePresentation("v08-mirror-false-footprint",1600,900);
                start=Time.time;while(m.Active&&g.State==VoyageState.Combat&&Time.time-start<40){V05Operate(m);yield return null;}
                Check(!m.Active,"v08 mirror trap recovery keeps the recorded route and permits actual reverse-route completion");
            }
            g.StartVoyage();yield return null;

            boss=V05BossFixture(117);m=boss.Encounter.Mechanism;m.BeginPhase(2);weapon=g.Player.weapon;start=Time.time;
            while(!m.MechanismTarget&&g.State==VoyageState.Combat&&Time.time-start<55){V05Operate(m);yield return null;}
            Check(m.MechanismTarget&&!m.Carrying&&m.CompletedActions==0,"v08 Kraken correct rod tension exposes a binding but cannot count an unbroken arm as severed");
            if(m.MechanismTarget){
                yield return null;g.Player.AimAt(V08TargetAim(m.MechanismTarget));yield return null;yield return new WaitForEndOfFrame();
                Check(m.MechanismTarget.GetComponentsInChildren<Collider>().Count(c=>c.enabled)==1,"v08 sculpted Kraken wrist and suckers add no bullet-blocking decoration colliders");
                V08TargetHud(m.MechanismTarget);CapturePresentation("v08-kraken-exposed-binding",1600,900);
                g.Player.SetRod(true);yield return new WaitForSeconds(.35f);
                Check(!m.UseAction()&&m.CompletedActions==0,"v08 Kraken cannot solve the exposed binding by repeatedly hooking with the rod");
                start=Time.time;while(m.MechanismTarget&&g.State==VoyageState.Combat&&Time.time-start<12){V05Operate(m);V08FireMechanism(m,weapon);yield return null;}
                Check(m.Active&&m.CompletedActions==1,"v08 Kraken rod-to-gun action destroys the real binding and counts precisely one severed arm");
                Check(m.Nodes.Count==m.RequiredActions&&!m.Nodes.Any(n=>n.Kind=="target-label"),"v08 destroyed Kraken binding removes its label and leaves only real arm nodes");
                At(m.Nodes[0].Position+Vector3.back*5,m.Nodes[0].Position+Vector3.up*1.4f);g.Player.SetRod(true);yield return new WaitForSeconds(.35f);
                Check(!m.UseAction()&&!m.Carrying&&m.CompletedActions==1,"v08 aiming and hooking the old severed arm or its deleted target label cannot farm extra arm progress");
            }
            g.StartVoyage();yield return null;

            boss=V05BossFixture(118);m=boss.Encounter.Mechanism;m.BeginPhase(2);yield return null;
            At(m.Nodes[0].Position+Vector3.back*2.2f);yield return new WaitForSeconds(.35f);m.UseAction();
            At(m.Nodes[m.CurrentTargetIndex+1].Position+Vector3.back*2.2f);yield return new WaitForSeconds(.35f);m.UseAction();
            Check(m.StateStep==3&&m.CompletedActions==0,"v08 late whale first correct bearing requires cross-observation rather than immediately granting a breach trial");
            yield return new WaitForSeconds(.35f);bool duplicate=m.UseAction();
            Check(!duplicate&&m.StateStep==3&&m.CompletedActions==0,"v08 repeating the first whale station cannot fake a second bearing");
            yield return new WaitForSeconds(10.1f);
            Check(m.StateStep==0&&V08WhaleVisualsReset(m),"v08 cross-observation timeout removes the old connecting line and restores all three rendered station colors");
            At(m.Nodes[0].Position+Vector3.back*2.2f);yield return new WaitForSeconds(.35f);m.UseAction();yield return null;
            Check(m.StateStep==1&&V08WhaleVisualsReset(m),"v08 retry sonar starts with no stale green station or previous bearing line");
            At(m.Nodes[m.CurrentTargetIndex+1].Position+Vector3.back*2.2f);yield return new WaitForSeconds(.35f);m.UseAction();yield return null;
            CapturePresentation("v08-whale-cross-observation",1600,900);
            At(m.Nodes[m.WhaleConfirmNode].Position+Vector3.back*2.2f);yield return new WaitForSeconds(.35f);m.UseAction();
            Check(m.StateStep==2&&FindObjectsOfType<TrackingBreach>().Length>0&&m.CompletedActions==0,"v08 a different whale observation station starts the actual separately telegraphed breach with no free observation credit");
            g.StartVoyage();yield return null;
            Check(!FindObjectsOfType<EncounterTarget>().Any()&&!FindObjectsOfType<BossMechanism>().Any(),"v08 returning to a new voyage releases late boss targets and mechanism state");
            Time.timeScale=1;
        }
    }
}

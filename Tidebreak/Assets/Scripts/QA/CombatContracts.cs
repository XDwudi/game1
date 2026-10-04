using System.Collections;
using System.Linq;
using UnityEngine;
namespace Tidebreak
{
    public partial class SmokePilot
    {
        IEnumerator WalkRoute(Vector3 target,string label)
        {
            At(new Vector3(0,0,2),target+Vector3.up*1.5f);yield return null;
            float begin=Time.realtimeSinceStartup;
            while(GameDirector.FlatDistance(g.Player.transform.position,target)>2.7f&&Time.realtimeSinceStartup-begin<14){
                Vector3 direction=target-g.Player.transform.position;direction.y=0;g.Player.Motor.Move(direction.normalized*5.2f*Time.deltaTime);yield return null;
            }
            if(GameDirector.FlatDistance(g.Player.transform.position,target)>=2.8f)Capture("blocked-trail-"+g.Run.stage+"-"+label);
            Check(GameDirector.FlatDistance(g.Player.transform.position,target)<2.8f,"island "+g.Run.stage+" walkable trail to "+label);
        }
        IEnumerator CombatContracts()
        {
            g.StartVoyage();Time.timeScale=3;g.Run.shotgun=g.Run.harpoon=g.Run.carbine=g.Run.burstRifle=g.Run.arcCaster=true;
            At(new Vector3(0,0,23),new Vector3(0,2,70));
            for(int i=0;i<6;i++){
                g.Player.Equip((WeaponKind)i);g.Player.Refill();yield return new WaitForSeconds(.5f);int before=g.Player.Ammo;
                Check(g.Player.Fire(),"weapon "+i+" accepts a trigger pull");yield return new WaitForSeconds(.4f);
                Check(g.Player.Ammo==before-(i==4?3:1),"weapon "+i+" spends its intended burst size");
                int spent=g.Player.Ammo;g.Player.Reload();g.Player.Equip((WeaponKind)((i+1)%6));g.Player.Equip((WeaponKind)i);
                Check(g.Player.Ammo==spent&&!g.Player.Reloading,"weapon "+i+" switch cancels reload without creating ammunition");
                g.Player.Reload();yield return new WaitForSeconds(2.3f);Check(g.Player.Ammo==g.Player.Capacity,"weapon "+i+" reload restores its magazine");
            }
            g.Player.Equip(WeaponKind.Carbine);yield return new WaitForSeconds(.5f);g.Player.Fire();Check(g.Player.AimBloom>0,"automatic fire expands aiming cone");yield return new WaitForSeconds(1);Check(g.Player.AimBloom<.0001f,"aiming cone recovers when trigger released");
            foreach(AttackStyle attack in System.Enum.GetValues(typeof(AttackStyle))){
                g.StartVoyage();Time.timeScale=3;g.SetState(VoyageState.Combat);g.Player.InvulnerableUntil=float.PositiveInfinity;At(new Vector3(0,0,8));
                var spec=ExpeditionContent.Species.First(s=>!s.boss&&s.attack==attack);var e=new GameObject("Attack fixture").AddComponent<Enemy>();e.InitSpecies(g,spec,false,new Vector3(0,3,14));
                float begin=Time.realtimeSinceStartup;while(e.AttackExecutions==0&&Time.realtimeSinceStartup-begin<5)yield return null;
                Check(e.AttackExecutions>0,"attack mechanic executes: "+attack);yield return new WaitForSeconds(2.2f);
            }
            g.StartVoyage();Time.timeScale=3;g.SetState(VoyageState.Combat);g.Player.InvulnerableUntil=float.PositiveInfinity;
            var boss=new GameObject("Stagger recovery fixture").AddComponent<Enemy>();boss.InitSpecies(g,ExpeditionContent.Species[108],false,new Vector3(0,1.1f,38));boss.maxHealth=1000;boss.health=10000;
            for(int pulse=0;pulse<50;pulse++){boss.Hit(80,false,false,false);yield return new WaitForSeconds(.1f);}
            Check(boss.AttackExecutions>0,"sustained stagger damage cannot permanently suppress boss attacks");
            // Exercise front-facing weak points against the real colliders, not a numeric multiplier alone.
            g.StartVoyage();Time.timeScale=3;
            for(int family=0;family<12;family++){
                var obj=new GameObject("Weak point fixture");obj.transform.position=new Vector3(10000,0,0);
                var model=SpeciesArt.Build(obj.transform,ExpeditionContent.Species[family],false);yield return null;Physics.SyncTransforms();
                var weak=model.GetComponentsInChildren<HitRegion>().First(h=>h.GetComponent<Collider>().enabled);
                RaycastHit hit;Vector3 origin=weak.transform.position+Vector3.forward*8;
                bool reaches=Physics.Raycast(origin,Vector3.back,out hit,10)&&hit.collider.GetComponent<HitRegion>()!=null;
                Check(reaches,"anatomy "+family+" exposes a shootable weak point");Destroy(obj);yield return null;
            }
            // A telegraph must be harmless during warning and damaging after its countdown.
            g.SetState(VoyageState.Combat);g.Player.InvulnerableUntil=0;At(new Vector3(0,0,10));float hp=g.Run.health;
            ThreatField.Line(g,g.Player.transform.position+Vector3.back*5,g.Player.transform.position+Vector3.forward*5,1,.8f,12,Color.red);
            yield return new WaitForSeconds(.3f);Check(g.Run.health==hp,"beam telegraph permits reaction time");yield return new WaitForSeconds(.7f);Check(g.Run.health<hp,"beam deals damage when countdown ends");
            g.StartVoyage();Time.timeScale=3;g.SetState(VoyageState.Combat);At(new Vector3(0,0,10));g.Player.InvulnerableUntil=float.PositiveInfinity;
            ThreatField.Pool(g,g.Player.transform.position,2,.1f,5,Color.green,SeaTrait.Venom);yield return new WaitForSeconds(.3f);Check(g.Player.StatusEffect=="","invulnerable player does not inherit pool poison");
            g.StartVoyage();Time.timeScale=3;g.Run.health=30;g.Run.medkits=1;g.UseUtility("medkit");Check(g.Run.health==75&&g.Run.medkits==0,"medical supply heals once and consumes one item");g.UseUtility("medkit");Check(g.Run.health==75,"empty medical inventory cannot heal");
            g.Run.sonarCharges=1;g.UseUtility("sonar");Check(g.SonarUntil>Time.time&&g.Run.sonarCharges==0,"sonar consumes a charge and opens the exploration window");
            g.Run.tonics=1;g.UseUtility("tonic");Check(g.TonicUntil>Time.time&&g.Run.tonics==0,"speed tonic consumes a charge and applies a timed effect");
            g.StartVoyage();Time.timeScale=3;g.SetState(VoyageState.Combat);At(new Vector3(0,0,10));g.Player.InvulnerableUntil=0;
            var volatileSpec=ExpeditionContent.Species.First(s=>!s.boss&&s.trait==SeaTrait.Volatile);var volatileEnemy=new GameObject("Volatile fixture").AddComponent<Enemy>();volatileEnemy.InitSpecies(g,volatileSpec,false,g.Player.transform.position);hp=g.Run.health;volatileEnemy.Hit(10000);
            Check(g.State==VoyageState.Combat,"volatile death keeps its escape window active");yield return new WaitForSeconds(1.4f);Check(g.Run.health<hp&&g.State==VoyageState.Sailing,"volatile last enemy explodes before encounter resolves");
        }
    }
}

using System.Collections;
using System.Linq;
using UnityEngine;

namespace Tidebreak
{
    public partial class SmokePilot
    {
        Enemy V05EliteFixture(int family)
        {
            g.StartVoyage();Time.timeScale=v05Speed;g.Run.hullLevel=3;g.Run.health=g.Run.MaxHealth;g.SetState(VoyageState.Combat);
            At(new Vector3(0,0,7));var p=new Vector3(0,0,15);p.y=g.World.GroundAt(p)+1.1f;
            var elite=new GameObject("V05 counter fixture "+family).AddComponent<Enemy>();elite.InitSpecies(g,ExpeditionContent.Species[family],true,p);
            g.Player.Equip(WeaponKind.Revolver);g.Player.Refill();return elite;
        }
        IEnumerator V05WaitEliteWindup(Enemy e)
        {
            float begin=Time.time;while(e&&e.Elite.Windup<=0&&Time.time-begin<7&&g.State==VoyageState.Combat)yield return null;
        }
        IEnumerator V05ShootAnchor(Enemy e,string label)
        {
            var anchor=e.Elite.Anchor;Check(anchor!=null,label+" creates its distinct shootable support object");if(!anchor)yield break;
            At(anchor.transform.position+Vector3.back*5,anchor.transform.position);float before=e.Elite.DamageFactor(false),begin=Time.time;
            while(anchor&&!anchor.Dead&&Time.time-begin<10&&g.State==VoyageState.Combat){g.Player.AimAt(anchor.transform.position);g.Player.Fire();yield return null;}
            Check(!anchor||anchor.Dead,label+" support object breaks from real weapon fire");
            Check(e&&e.Elite.DamageFactor(false)>before*2,label+" breaking support creates a real body-damage opportunity");
        }
        IEnumerator V05EliteCounterContracts()
        {
            // These are disclosed isolated counter contracts. Half-health setup and
            // hit classification probes are not represented as real combat clears.
            foreach(int family in new[]{0,1,10}){
                var e=V05EliteFixture(family);yield return V05WaitEliteWindup(e);
                if(family==10){int bolts=FindObjectsOfType<SeaProjectile>().Length;e.Hit(.01f,false,false,false);Check(FindObjectsOfType<SeaProjectile>().Length>bolts,"urchin body-hit classification triggers a counter projectile during windup");}
                float begin=Time.time;bool interrupted=false;
                while(e&&Time.time-begin<1&&g.State==VoyageState.Combat){g.Player.AimAt(V05Aim(e));g.Player.Fire();if(e.Elite.Cue.Contains("弱点打断")){interrupted=true;break;}yield return null;}
                Check(interrupted,"elite "+family+" actual weak-point shot interrupts its windup");
                if(family==1&&e)Check(e.Elite.DamageFactor(false)>1,"puffer interrupted inflation opens body damage instead of retaining armor");
            }
            {
                var e=V05EliteFixture(2);e.health=e.maxHealth*.4f;yield return null;float hp=e.health;yield return new WaitForSeconds(.4f);
                Check(e&&e.Elite.Anchor&&e.health>hp,"eel sheds a persistent shell and regenerates while the shell survives");
                yield return V05ShootAnchor(e,"eel molted shell");if(e){hp=e.health;yield return new WaitForSeconds(.5f);Check(e&&e.health<=hp+.001f,"destroying the eel shell actually stops regeneration");}
            }
            {
                var e=V05EliteFixture(3);float begin=Time.time;int volley=0;bool five=false;
                while(e&&volley<2&&Time.time-begin<12&&g.State==VoyageState.Combat){if(e.Elite.Moves>volley){volley=e.Elite.Moves;five|=FindObjectsOfType<SeaProjectile>().Length>=5;}yield return null;}
                Check(volley>=2&&five,"ray executes successive five-blade alternating-wing volleys");
            }
            {
                var e=V05EliteFixture(4);yield return null;Vector3 facing=e.transform.forward,origin=e.transform.position;At(origin+facing*3);float front=e.Elite.DamageFactor(false);At(origin-facing*3);
                Check(front<=.21f&&e.Elite.DamageFactor(false)>front*4,"crab front armor can be countered by reaching its rear");Check(e.Elite.DamageFactor(true)>=1,"crab eye remains a frontal weak-point alternative");
            }
            {
                var e=V05EliteFixture(5);yield return null;yield return V05ShootAnchor(e,"jelly symbiotic lamp");
            }
            {
                var e=V05EliteFixture(6);yield return V05WaitEliteWindup(e);float closed=e.Elite.DamageFactor(false);int moves=e.Elite.Moves;float begin=Time.time;
                while(e&&e.Elite.Moves==moves&&Time.time-begin<3)yield return null;
                Check(e&&closed<.2f&&e.Elite.DamageFactor(false)>1,"turtle retracts during windup and opens a real damage window after its ring");
            }
            {
                var e=V05EliteFixture(7);float begin=Time.time;while(e&&e.Elite.Moves<1&&Time.time-begin<4)yield return null;yield return null;
                var decoys=FindObjectsOfType<Transform>().Where(t=>t.name=="Ink silhouette · no luminous eye").ToArray();
                Check(decoys.Length==2&&decoys.All(t=>t.GetComponentsInChildren<HitRegion>().Length==0&&t.GetComponentsInChildren<Collider>().Length==0),"squid creates two visual decoys without fake damageable weak points or bullet blockers");
                Check(FindObjectsOfType<SeaProjectile>().Any(p=>p.returnAfter>0),"squid attack retains its returning projectile alongside decoys");
            }
            {
                var e=V05EliteFixture(8);yield return V05WaitEliteWindup(e);Vector3 old=g.Player.transform.position;At(old+Vector3.right*5);float begin=Time.time;
                while(e&&e.Elite.Moves<1&&Time.time-begin<3)yield return null;
                Check(FindObjectsOfType<ThreatField>().Any(f=>f.Kind==0&&f.Radius>.7f&&Mathf.Abs(f.End.x-old.x)<1),"swordfish strike commits to its telegraphed old lane, allowing lateral evasion");
            }
            {
                var e=V05EliteFixture(9);e.health=e.maxHealth*.65f;float hp=e.health;yield return new WaitForSeconds(.4f);
                Check(e&&e.health>hp,"seahorse brood egg provides actual sustained healing");yield return V05ShootAnchor(e,"seahorse brood egg");
                if(e){hp=e.health;yield return new WaitForSeconds(.5f);Check(e&&e.health<=hp+.001f,"destroying the brood egg stops its healing");}
            }
            {
                var e=V05EliteFixture(11);e.health=e.maxHealth*.4f;float begin=Time.time;
                while(e&&e.Elite.Moves<1&&Time.time-begin<4)yield return null;
                Vector3 second=g.Player.transform.position+Vector3.right*5;At(second);yield return new WaitForSeconds(1.2f);
                Check(FindObjectsOfType<DeckWarning>().Any(w=>GameDirector.FlatDistance(w.transform.position,second)<.5f),"half-health shark relocks its second pounce at the new player position");
            }
            g.StartVoyage();yield return null;
        }
    }
}

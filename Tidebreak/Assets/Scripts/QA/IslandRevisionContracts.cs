using System.Collections;
using System.Linq;
using UnityEngine;

namespace Tidebreak
{
    public partial class SmokePilot
    {
        void IslandFixture(int stage)
        {
            g.StartVoyage();g.Run.maxIsland=9;
            if(stage>1){g.ShowMap();g.Travel(stage);}
            g.Run.questStep=2;g.Run.landed=1;g.Run.eventMask=0;g.Run.exploredMask=0;
            g.Player.InvulnerableUntil=float.PositiveInfinity;Time.timeScale=4;
            g.TickIslandMission();
        }
        void ClearMissionFixtureEnemies()
        {
            foreach(var e in g.Enemies.ToArray())if(e&&!e.dead)e.Hit(100000,true,false,false);
        }
        IEnumerator OperateMissionUntil(System.Func<bool> finished,float seconds,bool followTarget=false)
        {
            float start=Time.realtimeSinceStartup;
            while(!finished()&&Time.realtimeSinceStartup-start<seconds){
                ClearMissionFixtureEnemies();
                if(followTarget)At(g.MissionTarget+Vector3.back);
                yield return null;
            }
        }
        IEnumerator IslandRevisionContracts()
        {
            // Mechanics fixtures deliberately isolate objectives from combat skill.
            // RevisionBossBattle separately tests incoming pressure without god mode.
            IslandFixture(1);At(g.SitePoint(1));g.UseSite(1);
            Check(!g.MissionActive,"lighthouse cannot start without its battery");
            At(g.SitePoint(0));g.UseSite(0);At(g.SitePoint(1));g.UseSite(1);
            Check(g.MissionActive,"lighthouse starts a timed power-up defense");
            At(g.SitePoint(1)+Vector3.forward*10);yield return new WaitForSeconds(.6f);
            Check(g.MissionProgress<.2f,"lighthouse power-up stops when captain leaves its radius");
            At(g.SitePoint(1));yield return OperateMissionUntil(()=>g.Run.questStep==3,8);
            Check(g.Run.questStep==3,"lighthouse defense grants boss access without another sample hand-in");

            IslandFixture(2);At(g.SitePoint(0));g.UseSite(0);yield return new WaitForSecondsRealtime(3.6f);
            At(g.SitePoint(1));g.UseSite(1);int[] melody=NarrativeContent.Melody(g.Run.seed);
            g.AnswerMissionTone((melody[0]+1)%3);Check(g.MelodyStep==0&&g.Run.questStep==2,"wrong coral response resets the phrase without advancing story");
            Capture("v04-coral-response");foreach(int note in melody)g.AnswerMissionTone(note);
            Check(g.Run.questStep==3,"four actual coral response buttons resolve the resonance puzzle");

            IslandFixture(3);yield return null;
            var toxins=FindObjectsOfType<EncounterTarget>().Where(t=>t.Label.StartsWith("毒囊")).ToArray();
            Check(toxins.Length==3,"mangrove presents three separate shootable pollution targets");
            foreach(var toxin in toxins)toxin.Hit(1000);yield return null;
            Check((g.Run.eventMask&1)!=0,"breaking all toxic sacs records the purified waterway");
            At(g.SitePoint(0));g.UseSite(0);Vector3 boat=g.MissionTarget;At(g.SitePoint(0)+Vector3.forward*12);yield return new WaitForSeconds(.7f);
            Check(GameDirector.FlatDistance(boat,g.MissionTarget)<.2f,"purifier skiff waits when escort leaves eight metres");
            yield return OperateMissionUntil(()=>g.Run.questStep==3,15,true);
            Check(g.Run.questStep==3,"moving skiff reaches the second site under escort");

            IslandFixture(4);At(g.SitePoint(0));g.UseSite(0);g.CloseDialogue();At(g.SitePoint(1));g.UseSite(1);
            Check(!g.ConfirmAstrolabe(),"astrolabe rejects a misaligned sky");
            g.RotateAstrolabe(0);g.RotateAstrolabe(1);g.RotateAstrolabe(1);Capture("v04-astrolabe");
            Check(g.ConfirmAstrolabe()&&g.Run.questStep==3,"three independently rotated axes satisfy the evidence relationships");

            IslandFixture(5);At(g.SitePoint(0));g.UseSite(0);At(g.SitePoint(0)+Vector3.forward*10);yield return new WaitForSeconds(.7f);
            Check(g.MissionProgress<.2f,"black-box download requires an actual nearby operator");
            At(g.SitePoint(0));yield return OperateMissionUntil(()=>(g.Run.eventMask&1)!=0,12);
            Check((g.Run.eventMask&1)!=0&&g.Run.questStep==2,"download preserves evidence and requires the separate ship-bell recording");
            At(g.SitePoint(1));g.UseSite(1);yield return null;Check(g.Run.questStep==3,"ship-bell evidence reveals the captain's own distress call");

            IslandFixture(6);int coins=g.Run.coins;At(g.SitePoint(0));g.UseSite(0);
            At(g.SitePoint(0)+Vector3.forward*8);yield return new WaitForSeconds(2);
            Check(g.MissionHeat<98,"carried rescue heat depletes in the open");
            Vector3 heater=Vector3.Lerp(g.SitePoint(0),g.SitePoint(1),1f/3);heater.z+=4;At(heater);float heat=g.MissionHeat;yield return new WaitForSeconds(1);
            Check(g.MissionHeat>heat,"standing by a real intermediate heater replenishes rescue heat");
            At(g.SitePoint(0)+Vector3.forward*8);yield return new WaitForSeconds(34);
            Check(!g.MissionActive&&g.Run.questStep==2&&g.Run.coins==coins,"cold rescue fails safely without spending coins or erasing the chapter");
            At(g.SitePoint(0));g.UseSite(0);At(g.SitePoint(1));yield return new WaitForSeconds(.65f);
            Check(g.Run.questStep==2&&g.MissionProgress>0&&g.MissionProgress<2,"arriving at the rescue pod cannot instantly complete the rescue");
            float rescueStart=Time.realtimeSinceStartup,lastThaw=g.MissionProgress;bool warming=false,thawPreserved=true;int reheats=0;
            Vector3 secondHeater=Vector3.Lerp(g.SitePoint(0),g.SitePoint(1),2f/3);secondHeater.z-=3;
            while(g.Run.questStep<3&&Time.realtimeSinceStartup-rescueStart<22){
                ClearMissionFixtureEnemies();
                if(!warming&&g.MissionHeat<45){warming=true;reheats++;}
                if(warming&&g.MissionHeat>91)warming=false;
                At(warming?secondHeater:g.SitePoint(1));
                if(g.MissionActive){thawPreserved&=g.MissionProgress>=lastThaw-.001f;lastThaw=g.MissionProgress;}
                yield return null;
            }
            Check(g.Run.questStep==3&&reheats>=2&&thawPreserved,"eighteen-second rescue requires repeated real heat replenishment and preserves thaw progress on retreat");

            IslandFixture(7);At(g.SitePoint(0));g.UseSite(0);yield return new WaitForSeconds(14.5f);
            string prompt;Check(g.TryMissionInteraction(out prompt)&&prompt.Contains("领取"),"late electrical delivery returns to recharge instead of completing for free");
            for(int charge=0;charge<3;charge++){
                At(g.SitePoint(0));g.UseMissionAction();At(Vector3.Lerp(g.SitePoint(0),g.SitePoint(1),.5f));yield return null;
                At(g.SitePoint(1));if(charge==2)g.SpawnMissionEnemy(1,false,g.SitePoint(1)+Vector3.forward*7);g.UseMissionAction();
                if(charge==2){At(g.SitePoint(0));bool canRecharge=g.TryMissionInteraction(out prompt);g.UseMissionAction();Check(g.MissionActive&&g.Enemies.Count>0&&!canRecharge&&g.DeliveredCharges==3,"completed relay cannot accept a fourth charge during remaining-guard cleanup");}
                ClearMissionFixtureEnemies();yield return null;
            }
            Check(g.Run.questStep==3&&g.DeliveredCharges==3,"three separately accepted live electrical deliveries restore the arrays");

            IslandFixture(8);At(g.SitePoint(0));g.UseSite(0);At(g.SitePoint(1));g.UseSite(1);
            float pressure=g.MissionPressure;At(g.SitePoint(1)+new Vector3(-3.6f,0,2));g.UseMissionAction();
            Check(g.MissionPressure>pressure&&g.MissionHeat>75,"forge fire valve increases both heat and dangerous pressure");
            yield return new WaitForSeconds(1.4f);pressure=g.MissionPressure;
            At(g.SitePoint(1)+new Vector3(3.6f,0,2));g.UseMissionAction();Check(g.MissionPressure<pressure,"separate forge relief valve lowers pressure");
            float forgeStart=Time.realtimeSinceStartup;
            while(g.Run.questStep<3&&Time.realtimeSinceStartup-forgeStart<24){
                ClearMissionFixtureEnemies();
                if(g.MissionPressure>61){At(g.SitePoint(1)+new Vector3(3.6f,0,2));g.UseMissionAction();}
                else if(g.MissionHeat<51){At(g.SitePoint(1)+new Vector3(-3.6f,0,2));g.UseMissionAction();}
                yield return new WaitForSeconds(.4f);
            }
            Check(g.Run.questStep==3,"controlling the two real valves maintains the forging band until the key is made");

            IslandFixture(9);At(g.SitePoint(0));g.UseSite(0);Capture("v04-memory-choice");g.ChooseMemory(1);
            Check(g.Run.storyChoice==1&&(g.Run.eventMask&1)!=0,"memory choice persists before the finale");
            At(g.SitePoint(1));g.UseSite(1);Check(GameDirector.FlatDistance(g.MissionTarget,g.SitePoint(0))<.1f,"preserving memory selects the western anchor first");
            yield return OperateMissionUntil(()=>g.Run.questStep==3,7,true);
            Check(g.Run.questStep==3&&g.SealedAnchors==3,"two actual capture zones seal the mirror tide");
            g.Checkpoint(false);var saved=SaveStore.Read<RunData>("voyage");
            Check(saved!=null&&saved.storyChoice==1&&saved.questStep==3&&saved.checkpointVersion>=4,"final choice and objective completion survive a saved checkpoint");

            IslandFixture(7);g.Run.eventMask=1|(3<<8);g.Checkpoint(false);g.ReturnHarbor();g.StartVoyage(true);Time.timeScale=4;g.Player.InvulnerableUntil=float.PositiveInfinity;
            At(g.SitePoint(0));g.UseSite(0);ClearMissionFixtureEnemies();yield return null;
            Check(g.Run.questStep==3&&g.DeliveredCharges==3,"resume after third charge only clears guards, never asks for a fourth charge");
            int earned=g.Run.coins;At(g.SitePoint(0));g.UseSite(0);Check(g.Run.coins==earned,"completed island investigation stipend cannot be claimed twice");
            IslandFixture(9);g.Run.eventMask=1|(3<<10);g.Run.storyChoice=1;g.Checkpoint(false);g.ReturnHarbor();g.StartVoyage(true);Time.timeScale=4;g.Player.InvulnerableUntil=float.PositiveInfinity;
            At(g.SitePoint(1));g.UseSite(1);ClearMissionFixtureEnemies();yield return null;
            Check(g.Run.questStep==3&&g.SealedAnchors==3,"resume after second anchor preserves its completion while clearing remaining guards");

            var old=JsonUtility.FromJson<RunData>(JsonUtility.ToJson(g.Run));old.stage=4;old.maxIsland=4;old.questStep=2;old.checkpointVersion=3;old.coins=541;old.weaponLevel=2;
            old.islands[0]=new IslandProgress{step=4,boss=true,explored=7};SaveStore.Write("voyage",old);g.StartVoyage(true);
            Check(g.Run.checkpointVersion==4&&g.Run.questStep==0&&g.Run.coins==541&&g.Run.weaponLevel==2&&g.Run.islands[0].step==4,"v0.3 migration restarts unfinished errands but preserves wallet, gear and finished islands");

            IslandFixture(3);yield return null;Check(FindObjectsOfType<EncounterTarget>().Any(t=>t.Label.StartsWith("毒囊")),"unbroken mission targets exist before leaving");
            g.ReturnHarbor();yield return null;Check(!FindObjectsOfType<EncounterTarget>().Any(t=>t.Label.StartsWith("毒囊")),"leaving a voyage destroys independently parented mission targets");
            Time.timeScale=1;
        }
    }
}

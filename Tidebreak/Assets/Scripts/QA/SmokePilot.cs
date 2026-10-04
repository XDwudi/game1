using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace Tidebreak
{
    public partial class SmokePilot : MonoBehaviour
    {
        GameDirector g;readonly List<string> results=new List<string>();string output;bool failed,visual;float startTime;
        void Check(bool ok,string label){results.Add((ok?"PASS ":"FAIL ")+label);Debug.Log(results[results.Count-1]);failed|=!ok;File.WriteAllLines(Path.Combine(output,"results.txt"),results);}
        void At(Vector3 point,Vector3? target=null){point.y=g.World.GroundAt(point)+1.72f;g.Player.Teleport(point);g.Player.AimAt(target??point+Vector3.forward*10);Physics.SyncTransforms();}
        void Capture(string name)
        {
            var cam=g.Player.View;var canvas=FindObjectsOfType<Canvas>().First(c=>c.name=="Tidebreak UI");var mode=canvas.renderMode;var camera=canvas.worldCamera;float plane=canvas.planeDistance;var previous=cam.targetTexture;var active=RenderTexture.active;
            var rt=RenderTexture.GetTemporary(1600,900,24,RenderTextureFormat.ARGB32);var img=new Texture2D(1600,900,TextureFormat.RGB24,false);
            try{canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=.2f;cam.targetTexture=rt;Canvas.ForceUpdateCanvases();cam.Render();RenderTexture.active=rt;img.ReadPixels(new Rect(0,0,1600,900),0,0);img.Apply();File.WriteAllBytes(Path.Combine(output,name+".png"),img.EncodeToPNG());}
            finally{cam.targetTexture=previous;RenderTexture.active=active;canvas.renderMode=mode;canvas.worldCamera=camera;canvas.planeDistance=plane;RenderTexture.ReleaseTemporary(rt);Destroy(img);}
        }
        IEnumerator Fight(string photo=null)
        {
            float start=Time.realtimeSinceStartup;bool hit=false,two=false,captured=false;
            while(g.State==VoyageState.Combat&&Time.realtimeSinceStartup-start<90){
                var e=g.Enemies.FirstOrDefault(x=>x&&!x.dead);if(e){float hp=e.health;g.Player.AimAt(e.transform.position+Vector3.up*(e.IsBoss?.45f:.05f));g.Player.Fire();hit|=e.health<hp;two|=e.phase==2;}
                if(photo!=null&&!captured&&Time.realtimeSinceStartup-start>1){Capture(photo);captured=true;}
                yield return null;
            }
            Check(hit,"live camera ray damages target");Check(g.State==VoyageState.Sailing||g.State==VoyageState.Victory,"encounter resolves with real weapon fire");
            if(photo!=null&&photo.StartsWith("boss"))Check(two,"boss second phase is reachable");
        }
        IEnumerator GatherCatch(bool photo=false)
        {
            At(new Vector3(0,0,23),new Vector3(0,0,50));g.Player.SetRod(true);g.CastCharge=.45f;int coins=g.Run.coins;g.Cast();Check(g.State==VoyageState.Fishing,"water cast accepted");
            float start=Time.realtimeSinceStartup;bool captured=false;
            while(g.State==VoyageState.Fishing&&Time.realtimeSinceStartup-start<35){g.TickFishing(!g.Surge,Time.deltaTime);if(photo&&!captured&&g.ReelProgress>.3f){Capture("fishing");captured=true;}yield return null;}
            Check(g.State==VoyageState.Combat,"fishing produces a live local species");if(g.State!=VoyageState.Combat)yield break;
            var species=g.Enemies[0].Spec;Check(species.island==Mathf.Min(8,g.Run.stage-1)&&species.lure<=g.Run.selectedLure,"habitat and equipped lure constrain catch pool");
            yield return Fight(photo?"combat":null);Check(g.Run.coins==coins,"ordinary kills do not award unsold catch coins");
            if(g.Loot.Count==0){Check(false,"physical catch exists");yield break;}
            var loot=g.Loot.Last();yield return new WaitForSeconds(.25f);At(loot.transform.position+new Vector3(0,0,-1.2f),loot.transform.position);g.Player.PickUp(loot);Check(g.Player.HeldFish==loot,"catch pickup succeeds");
            if(photo){Capture("held-catch");g.Player.ThrowHeld();yield return new WaitForSeconds(.2f);Check(!loot.Held&&loot.GetComponent<Rigidbody>().velocity.magnitude>1,"throw restores physical velocity");At(loot.transform.position+Vector3.back,loot.transform.position);g.Player.PickUp(loot);}
            g.Player.StowHeld();yield return null;Check(g.Run.bag.Any(x=>x.speciesId==species.id),"species identity persists in bag");
            if(photo){g.TogglePause();g.UI.ShowBag();yield return null;Capture("bag");g.TogglePause();}
        }
        void Guide(){At(g.World.QuestPoint+Vector3.forward*2,g.World.QuestPoint+Vector3.up*1.7f);g.TalkGuide();}
        void Shop(){At(g.World.ShopPoint+Vector3.forward*2,g.World.ShopPoint+Vector3.up*2);g.OpenShop();}
        void Sell(){At(g.World.SellPoint+Vector3.forward*2,g.World.SellPoint+Vector3.up*2);g.SellCatch();}
        bool Buy(string id){int price=g.Price(id);return price>=0&&g.Run.coins>=price&&g.Buy(id);}
        void Arm(){WeaponKind kind=g.Run.harpoon?WeaponKind.Harpoon:g.Run.carbine?WeaponKind.Carbine:g.Run.shotgun?WeaponKind.Scattergun:WeaponKind.Revolver;g.Player.Equip(kind);g.Player.Refill();}
        IEnumerator Start()
        {
            g=GameDirector.Instance;output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Artifacts/ExpeditionQA"));Directory.CreateDirectory(output);Application.logMessageReceived+=LogError;visual=Array.IndexOf(Environment.GetCommandLineArgs(),"-tidebreakVisual")>=0;startTime=Time.realtimeSinceStartup;
            yield return new WaitForSecondsRealtime(1);Capture("harbor");
            Check(ExpeditionContent.Species.Length==119&&ExpeditionContent.Species.Count(x=>!x.boss)==108,"108 normal species plus 11 bosses, excluding variants");Check(ExpeditionContent.Species.Select(x=>x.name).Distinct().Count()==119,"all species have unique names and stable IDs");
            Check(ExpeditionContent.Species.Where(x=>!x.boss).Select(x=>x.body).Distinct().Count()==12,"twelve anatomical families");Check(ExpeditionContent.Species.Where(x=>!x.boss).Select(x=>x.attack).Distinct().Count()==14,"fourteen primary attack mechanics");
            Check(ShopCatalog.All.Length==23&&Relic.All.Length==20,"23 purchasable equipment and supply offers plus 20 random perks");
            var legacy=new RunData{checkpointVersion=2,stage=4,coins=321,weaponLevel=2,shotgun=true};legacy.bag.Add(new CatchData{kind=CreatureKind.Puffer,speciesId=0,value=55});SaveStore.Write("voyage",legacy);g.StartVoyage(true);
            Check(g.Run.coins==321&&g.Run.weaponLevel==2&&g.Run.shotgun&&g.Run.questStep==0,"v0.2 checkpoint retains paid gear and wallet while migrating story");Check(g.Run.bag[0].speciesId==-1&&g.Run.bag[0].value==55,"v0.2 catch retains its legacy identity and appraisal");
            g.StartVoyage();yield return new WaitForSeconds(.4f);Check(g.Player.Grounded,"captain stands on solid island terrain");
            At(new Vector3(0,0,4),new Vector3(0,3,-25));yield return null;Capture("island-1");
            Guide();yield return null;Capture("story");Check(g.AdvanceStory()&&g.Run.questStep==1,"accepting local story commission advances to research");
            Guide();Check(!g.AdvanceStory()&&g.Run.questStep==1,"story cannot advance without required samples");
            Shop();int wallet=g.Run.coins;Check(!g.Buy("shotgun")&&g.Run.coins==wallet,"future island weapon cannot be bought early");g.Run.coins=0;Check(!g.Buy("weapon")&&g.Run.weaponLevel==0,"insufficient coins cannot buy upgrades");g.Run.coins=wallet;g.CloseShop();
            g.Player.InvulnerableUntil=float.PositiveInfinity;Time.timeScale=3;yield return GatherCatch(true);if(failed){Finish();yield break;}
            int before=g.Run.coins,value=g.Run.BagValue;Sell();Check(g.Run.coins==before+value&&g.Run.bag.Count==0,"market pays exact appraised value");
            Shop();yield return null;Capture("workshop");g.Run.coins=1000;int cost=g.Price("weapon");Check(g.Buy("weapon")&&g.Run.coins==1000-cost,"coin purchase charges exact price");before=g.Run.coins;cost=g.Price("relic0");Check(g.Buy("relic0")&&g.Run.coins==before-cost,"random perk costs coins");before=g.Run.coins;Check(!g.Buy("relic0")&&g.Run.coins==before,"local perk stock cannot be bought twice");g.CloseShop();
            g.TogglePause();g.UI.ShowJournal();yield return new WaitForSecondsRealtime(.2f);Capture("codex");g.TogglePause();
            g.StartVoyage();g.Player.InvulnerableUntil=float.PositiveInfinity;Time.timeScale=3;
            if(visual){g.Run.maxIsland=9;for(int i=2;i<=9;i++){g.ShowMap();g.Travel(i);yield return new WaitForSeconds(.3f);At(new Vector3(0,0,5),new Vector3(0,4,-31));yield return null;Capture("island-"+i);Check(g.World.Region==i-1,"distinct island "+i+" loads geometry");}g.ShowMap();yield return null;yield return null;Capture("island-map");g.CloseShop();Finish();yield break;}
            yield return CombatContracts();if(failed){Finish();yield break;}g.StartVoyage();Time.timeScale=3;yield return new WaitForSecondsRealtime(.5f);
            for(int island=1;island<=9&&!failed;island++){
                Check(g.Run.stage==island&&g.World.Region==island-1,"story reaches island "+island);
                At(new Vector3(0,0,4),new Vector3(0,4,-30));yield return null;Capture("island-"+island);
                yield return WalkRoute(g.World.QuestPoint,"guide");yield return WalkRoute(g.SitePoint(0),"first relic");yield return WalkRoute(g.SitePoint(1),"second relic");
                if(failed){Finish();yield break;}Guide();g.AdvanceStory();g.Player.InvulnerableUntil=float.PositiveInfinity;
                int attempts=0;while(!g.Run.SamplesReady&&attempts++<8&&!failed)yield return GatherCatch();
                Check(g.Run.SamplesReady,"local samples include two distinct species");Guide();Check(g.AdvanceStory()&&g.Run.questStep==2,"sample research opens exploration objectives");
                Sell();Shop();Buy("weapon");if(island==1){Buy("rod");Buy("bag");}foreach(string id in new[]{"shotgun","carbine","harpoon","burst","arc","lure1","lure2","lure3"}){var item=ShopCatalog.Find(id);if(item.island==island)Buy(id);}if(g.Run.health<60)Buy("heal");g.CloseShop();Arm();
                At(g.SitePoint(0)+Vector3.forward*1.8f,g.SitePoint(0)+Vector3.up);g.UseSite(0);Check((g.Run.exploredMask&1)!=0,"first relic recovered through world interaction");
                At(g.SitePoint(1)+Vector3.forward*2,g.SitePoint(1)+Vector3.up);g.UseSite(1);Check(g.State==VoyageState.Combat&&g.Enemies.Count==2,"second relic triggers local guardian encounter");g.Player.InvulnerableUntil=float.PositiveInfinity;yield return Fight();
                Check(g.Run.SurveyReady,"guardian victory completes investigation");Guide();Check(g.AdvanceStory()&&g.Run.questStep==3,"investigation grants boss access");
                At(new Vector3(0,0,23),new Vector3(0,1,38));Arm();g.BeginCombat(true);g.Player.InvulnerableUntil=float.PositiveInfinity;yield return Fight("boss-"+island);
                Check(g.Run.bossTrophy&&g.Run.questStep==3,"boss drops story trophy without skipping hand-in");Guide();Check(g.AdvanceStory()&&g.Run.questStep==4,"trophy hand-in completes island story");
                if(island==9)break;Check(g.Run.maxIsland==island+1,"next island and equipment license unlock together");
                var saved=SaveStore.Read<RunData>("voyage");Check(saved!=null&&saved.questStep==4&&saved.coins==g.Run.coins&&saved.maxIsland==island+1,"story, wallet and unlocks persist together");
                if(island==1){g.ReturnHarbor();g.StartVoyage(true);Check(g.Run.questStep==4&&g.Run.maxIsland==2,"resume preserves story hand-in and licenses");}
                g.ShowMap();g.Travel(island+1);g.Player.InvulnerableUntil=float.PositiveInfinity;
            }
            if(failed){Finish();yield break;}
            Check(g.State==VoyageState.Victory,"nine-island story reaches homecoming ending");Capture("ending");
            g.ExploreAfterEnding();g.ShowMap();g.Travel(1);Check(g.Run.questStep==4&&g.Run.bossCleared,"revisiting early island preserves story completion");
            At(g.SitePoint(2)+Vector3.forward*1.5f);g.UseSite(2);for(int i=0;i<3;i++)g.SolveRune(i);Check((g.Run.exploredMask&4)!=0&&g.Run.rareSignal,"hidden cache puzzle awards ancient signal");
            g.ShowMap();g.Travel(9);g.ShowMap();g.Run.abyssBait=true;g.ChallengeKraken();g.Run.harpoon=true;g.Run.weaponLevel=4;Arm();At(new Vector3(0,0,23),new Vector3(0,2,38));g.BeginCombat(true);g.Player.InvulnerableUntil=float.PositiveInfinity;yield return Fight("kraken");Check(g.Run.krakenDefeated,"optional Kraken branch completes");
            g.ExploreAfterEnding();g.ShowMap();g.ChallengeWhiteWhale();g.Run.harpoon=true;g.Run.weaponLevel=4;Arm();At(new Vector3(0,0,23),new Vector3(0,2,38));g.BeginCombat(true);g.Player.InvulnerableUntil=float.PositiveInfinity;yield return Fight("white-whale");Check(g.Run.whaleDefeated,"optional white whale branch completes");
            // Catalog fixtures instantiate the actual anatomical builders, including colliders and weak points.
            for(int id=0;id<119;id++){var root=new GameObject("Catalog fixture "+id);root.transform.position=new Vector3(10000,0,0);var model=SpeciesArt.Build(root.transform,ExpeditionContent.Species[id],false);Check(model.GetComponentsInChildren<Renderer>().Length>0&&model.GetComponentsInChildren<Collider>().Length>0,"species "+id+" has visible geometry and hit geometry");Destroy(root);yield return null;}
            g.ExploreAfterEnding();g.ShowMap();yield return null;Capture("island-map");g.CloseShop();g.TogglePause();
            g.UI.ShowSpecies(118);yield return new WaitForSecondsRealtime(.3f);Capture("codex-whale");g.UI.ShowSpecies(117);yield return new WaitForSecondsRealtime(.3f);Capture("codex-kraken");g.TogglePause();
            g.StartVoyage();g.BeginCombat(false);g.Player.InvulnerableUntil=0;float hp=g.Run.health;g.Player.TakeDamage(10);Check(g.Run.health<hp,"enemy damage applies without QA protection");g.Player.InvulnerableUntil=0;hp=g.Run.health;g.Player.Dash(Vector3.right);g.Player.TakeDamage(20);Check(g.Run.health==hp,"dash protects during invulnerability window");
            g.Player.InvulnerableUntil=0;g.Player.TakeDamage(10000);Check(g.State==VoyageState.Defeat&&SaveStore.Read<RunData>("voyage")==null,"permadeath clears active expedition");Finish();
        }
        void Finish(){Time.timeScale=1;File.WriteAllLines(Path.Combine(output,"results.txt"),results);Debug.Log((failed?"EXPEDITION_QA_FAILED":"EXPEDITION_QA_PASSED")+" in "+(Time.realtimeSinceStartup-startTime).ToString("F1")+" seconds");Application.Quit(failed?1:0);}
        void LogError(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert){failed=true;results.Add("FAIL runtime: "+message);File.WriteAllLines(Path.Combine(output,"results.txt"),results);}}
        void OnDestroy(){Application.logMessageReceived-=LogError;}
    }
}

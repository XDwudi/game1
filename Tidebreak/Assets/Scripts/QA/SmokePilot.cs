using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Tidebreak
{
    // Opt-in player integration runs use isolated saves. Normal launches never install this pilot.
    public class SmokePilot : MonoBehaviour
    {
        GameDirector g;readonly List<string> checks=new List<string>();string output;bool failed,visualOnly;
        void Check(bool value,string label){checks.Add((value?"PASS ":"FAIL ")+label);Debug.Log(checks[checks.Count-1]);if(!value)failed=true;File.WriteAllLines(Path.Combine(output,"island-results.txt"),checks);}
        void Capture(string name)
        {
            var cam=g.Player.View;var canvas=FindObjectOfType<Canvas>();var mode=canvas.renderMode;var camera=canvas.worldCamera;float plane=canvas.planeDistance;var previous=cam.targetTexture;var active=RenderTexture.active;
            var rt=RenderTexture.GetTemporary(1600,900,24,RenderTextureFormat.ARGB32);var image=new Texture2D(1600,900,TextureFormat.RGB24,false);
            try{canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=.2f;cam.targetTexture=rt;Canvas.ForceUpdateCanvases();cam.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());}
            finally{cam.targetTexture=previous;RenderTexture.active=active;canvas.renderMode=mode;canvas.worldCamera=camera;canvas.planeDistance=plane;RenderTexture.ReleaseTemporary(rt);Destroy(image);}
        }
        void At(Vector3 p,Vector3 look){g.Player.Teleport(p);g.Player.AimAt(look);Physics.SyncTransforms();}
        IEnumerator Fish(bool photograph=false)
        {
            At(new Vector3(0,3.1f,23),new Vector3(0,0,45));g.Player.SetRod(true);g.CastCharge=.45f;int wallet=g.Run.coins;g.Cast();Check(g.State==VoyageState.Fishing,"water cast accepted");
            float start=Time.realtimeSinceStartup;bool captured=false;
            while(g.State==VoyageState.Fishing&&Time.realtimeSinceStartup-start<35){g.TickFishing(!g.Surge,Time.deltaTime);if(photograph&&!captured&&g.ReelProgress>.27f){Capture("03-fishing");captured=true;}yield return null;}
            Check(g.State==VoyageState.Combat,"reeling launches a physical fish");if(g.State!=VoyageState.Combat)yield break;
            yield return new WaitForSeconds(.35f);if(photograph&&g.Enemies.Count>0){g.Player.AimAt(g.Enemies[0].transform.position);Capture("04-airborne-fish");}
            yield return Fight(photograph?"05-fish-combat":null);
            Check(g.Run.coins==wallet,"ordinary kill does not mint coins before selling");
            Check(g.Loot.Count>0,"dead fish remains as a physical catch");if(g.Loot.Count==0)yield break;
            var loot=g.Loot[g.Loot.Count-1];yield return new WaitForSeconds(.3f);
            var p=loot.transform.position+new Vector3(0,1.8f,-1.6f);p.y=Mathf.Max(g.World.GroundAt(p)+1.65f,2.5f);At(p,loot.transform.position);yield return null;g.Player.PickUp(loot);
            Check(g.Player.HeldFish==loot,"catch can be picked up");if(photograph){g.Player.AimAt(new Vector3(-8,3,-6));yield return null;Capture("06-held-catch");g.Player.ThrowHeld();yield return new WaitForSeconds(.2f);Check(!loot.Held&&loot.GetComponent<Rigidbody>().velocity.magnitude>1,"throw restores rigidbody velocity");At(loot.transform.position+new Vector3(0,1,-1),loot.transform.position);g.Player.PickUp(loot);}
            g.Player.StowHeld();yield return null;if(photograph){g.TogglePause();g.UI.ShowBag();yield return null;Capture("06-catch-bag");g.TogglePause();}Check(g.Run.bag.Count>0&&g.Player.HeldFish==null,"stow adds catch to persistent bag");
        }
        IEnumerator Fight(string screenshot=null)
        {
            float start=Time.realtimeSinceStartup;bool captured=false;float oldhp=g.Enemies.Count>0?g.Enemies[0].health:0;bool realHit=false;
            while(g.State==VoyageState.Combat&&Time.realtimeSinceStartup-start<95){
                if(g.Enemies.Count>0&&g.Enemies[0]){var e=g.Enemies[0];g.Player.AimAt(e.transform.position+(e.IsBoss?Vector3.up*.3f:Vector3.zero));g.Player.Fire();if(e.health<oldhp)realHit=true;}
                if(screenshot!=null&&!captured&&Time.realtimeSinceStartup-start>.6f){Capture(screenshot);captured=true;}
                yield return null;
            }
            Check(realHit,"camera raycast damages a live target");Check(g.State==VoyageState.Sailing||g.State==VoyageState.Victory,"combat can be completed with equipped weapon");
        }
        IEnumerator Start()
        {
            g=GameDirector.Instance;output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Artifacts/IslandQA"));Directory.CreateDirectory(output);Application.logMessageReceived+=OnLog;visualOnly=Array.IndexOf(Environment.GetCommandLineArgs(),"-tidebreakVisual")>=0;
            yield return new WaitForSecondsRealtime(1);Capture("01-harbor");g.StartVoyage();yield return new WaitForSeconds(.4f);
            Check(g.Player.Grounded,"player stands on island terrain");
            Vector3 start=g.Player.transform.position;float end=Time.time+2.5f;
            while(Time.time<end){g.Player.Motor.Move(Vector3.forward*5*Time.deltaTime);yield return null;}
            Check(g.Player.transform.position.z>start.z+10&&g.Player.transform.position.y>2.5f,"walk from island onto pier with ground collision");
            At(new Vector3(0,3.1f,22),new Vector3(-4,2.4f,65));yield return null;Capture("02-pier");
            At(new Vector3(0,3.3f,2),new Vector3(0,3,-11));yield return null;Capture("02-village");
            At(new Vector3(10,3.4f,-3),new Vector3(10,3,-10));end=Time.time+1.2f;while(Time.time<end){g.Player.Motor.Move(Vector3.back*5*Time.deltaTime);yield return null;}
            Check(g.Player.transform.position.z>-6.68f&&g.Player.transform.position.z<-6.2f,"workshop counter blocks the character at "+g.Player.transform.position.z.ToString("F2"));
            g.TogglePause();float elapsed=g.Run.elapsed;yield return new WaitForSecondsRealtime(.15f);Check(g.Run.elapsed==elapsed,"pause freezes voyage time");g.TogglePause();
            At(new Vector3(0,3.3f,0),new Vector3(0,2,-20));g.CastCharge=0;g.Cast();Check(g.State==VoyageState.Sailing,"land casts are rejected");
            At(new Vector3(0,3.1f,23),new Vector3(0,0,45));g.Cast();g.FishingAge=2;g.Tension=.99f;g.TickFishing(true,.7f);Check(g.State==VoyageState.Sailing,"excess line tension returns to retry state");
            Time.timeScale=2;g.Player.InvulnerableUntil=float.PositiveInfinity;
            yield return Fish(true);
            if(failed){Finish();yield break;}
            At(new Vector3(-10,3.3f,-3.8f),new Vector3(-10,3.3f,-9));yield return null;Capture("07-market");int before=g.Run.coins,value=g.Run.BagValue;int sale=g.SellCatch();Check(sale==value&&g.Run.coins==before+value&&g.Run.bag.Count==0,"sale pays exact appraised value and empties bag");
            At(new Vector3(10,3.3f,-4.5f),new Vector3(10,3.5f,-10));g.OpenShop();yield return null;Capture("08-workshop");
            int legitimateWallet=g.Run.coins;g.Run.coins=0;int level=g.Run.weaponLevel;Check(!g.Buy("weapon")&&g.Run.weaponLevel==level&&g.Run.coins==0,"insufficient coins cannot buy an upgrade");
            g.Run.coins=1000;int price=g.Price("weapon");Check(g.Buy("weapon")&&g.Run.coins==1000-price&&g.Run.weaponLevel==level+1,"gear purchase charges exact coin price");
            int cash=g.Run.coins;price=g.Price("relic0");Check(g.Buy("relic0")&&g.Run.coins==cash-price,"random perk requires payment");cash=g.Run.coins;Check(!g.Buy("relic0")&&g.Run.coins==cash,"sold out perk cannot be bought twice");
            g.Buy("shotgun");g.Buy("harpoon");g.CloseShop();g.Player.Equip(WeaponKind.Scattergun);g.Player.Refill();yield return new WaitForSeconds(.2f);Capture("09-shotgun");g.Player.Equip(WeaponKind.Harpoon);g.Player.Refill();yield return new WaitForSeconds(.2f);Capture("09-harpoon");
            // This fixture tests transactions; restore a fresh run for the actual campaign economy.
            g.StartVoyage();g.Player.InvulnerableUntil=float.PositiveInfinity;Time.timeScale=3;
            if(visualOnly){
                for(int region=1;region<=2;region++){
                    g.Run.stage=region*3;g.Run.bossCleared=true;g.OpenRoute();g.ChooseRoute(RouteKind.Shoal);yield return new WaitForSeconds(.25f);
                    Check(g.World.Region==region,"coast geometry switches to region "+region);
                    At(new Vector3(0,3.3f,2),new Vector3(0,3,-11));yield return null;Capture("region-"+region);
                }
                g.Run.stage=10;g.Run.weaponLevel=3;g.Run.harpoon=true;g.Player.Equip(WeaponKind.Harpoon);g.Player.Refill();At(new Vector3(0,3.1f,23),new Vector3(0,3,40));g.BeginCombat(true);yield return new WaitForSeconds(1.8f);Capture("10-kraken");Finish();yield break;
            }
            for(int stage=1;stage<=9&&!failed;stage++) {
                if(!g.Run.BossStage){while(!g.Run.RouteReady&&!failed)yield return Fish();}
                else {At(new Vector3(0,3.1f,23),new Vector3(0,3,40));g.BeginCombat(true);yield return new WaitForSeconds(1);g.Player.InvulnerableUntil=0;float hp=g.Run.health;g.Player.TakeDamage(10);Check(g.Run.health<hp,"enemy damage affects the player");g.Player.InvulnerableUntil=0;hp=g.Run.health;g.Warn(g.Player.transform.position,2,.1f,12);yield return new WaitForSeconds(.22f);Check(g.Run.health<hp,"telegraphed ground strike deals actual damage");g.Player.InvulnerableUntil=0;hp=g.Run.health;g.Player.Dash(Vector3.right);g.Player.TakeDamage(20);Check(g.Run.health==hp,"dash invulnerability prevents damage");g.Player.InvulnerableUntil=float.PositiveInfinity;At(new Vector3(0,3.1f,23),new Vector3(0,1,37));yield return Fight("boss-"+stage);}
                if(failed||stage==9)break;
                Check(g.Run.RouteReady,"stage "+stage+" commission unlocks travel");
                At(new Vector3(-10,3.3f,-3.8f),new Vector3(-10,3,-9));g.SellCatch();
                At(new Vector3(10,3.3f,-4.5f),new Vector3(10,3,-10));g.OpenShop();
                if(!g.Run.shotgun&&g.Run.coins>=g.Price("shotgun"))g.Buy("shotgun");else if(g.Run.weaponLevel<4)g.Buy("weapon");
                if(g.Run.stage>=5&&!g.Run.harpoon&&g.Run.coins>=g.Price("harpoon"))g.Buy("harpoon");
                if(g.Run.stage>=7&&g.Run.coins>=g.Price("bait"))g.Buy("bait");
                if(g.Run.health<g.Run.MaxHealth-30)g.Buy("heal");
                var saved=SaveStore.Read<RunData>("voyage");Check(saved!=null&&saved.coins==g.Run.coins&&saved.landed==g.Run.landed&&saved.bag.Count==g.Run.bag.Count,"coins, bag and commission persist");
                g.CloseShop();if(stage==1){g.ReturnHarbor();g.StartVoyage(true);Check(g.State==VoyageState.Sailing&&g.Run.RouteReady,"resume returns to island with completed commission");}
                g.OpenRoute();g.ChooseRoute(RouteKind.Shoal);g.Player.InvulnerableUntil=float.PositiveInfinity;
                if(g.Run.harpoon)g.Player.Equip(WeaponKind.Harpoon);else if(g.Run.shotgun)g.Player.Equip(WeaponKind.Scattergun);g.Player.Refill();
            }
            Check(g.State==VoyageState.Victory,"nine-stage island campaign completes");Capture("11-victory");
            if(g.State==VoyageState.Victory){g.Run.abyssBait=true;g.ChallengeKraken();At(new Vector3(0,3.1f,23),new Vector3(0,3,40));g.Player.Equip(g.Run.harpoon?WeaponKind.Harpoon:WeaponKind.Revolver);g.Player.Refill();g.BeginCombat(true);g.Player.InvulnerableUntil=float.PositiveInfinity;yield return Fight("10-kraken");Check(g.Run.krakenDefeated&&g.State==VoyageState.Victory,"optional Kraken encounter completes");}
            g.StartVoyage();g.Run.stage=9;g.Run.rareSignal=true;g.SetState(VoyageState.Victory);g.ChallengeWhiteWhale();g.Run.weaponLevel=4;g.Run.harpoon=true;g.Player.Equip(WeaponKind.Harpoon);g.Player.Refill();At(new Vector3(0,3.1f,23),new Vector3(0,3,40));g.BeginCombat(true);g.Player.InvulnerableUntil=float.PositiveInfinity;yield return Fight("12-white-whale");Check(g.Run.whaleDefeated,"rare white whale alternative completes");
            g.StartVoyage();g.BeginCombat(false);g.Player.InvulnerableUntil=0;g.Player.TakeDamage(10000);Check(g.State==VoyageState.Defeat&&SaveStore.Read<RunData>("voyage")==null,"permadeath clears current run");Finish();
        }
        void Finish(){Time.timeScale=1;File.WriteAllLines(Path.Combine(output,"island-results.txt"),checks);Debug.Log(failed?"ISLAND_QA_FAILED":"ISLAND_QA_PASSED");Application.Quit(failed?1:0);}
        void OnLog(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert){failed=true;checks.Add("FAIL runtime error: "+message);File.WriteAllLines(Path.Combine(output,"island-results.txt"),checks);}}
        void OnDestroy(){Application.logMessageReceived-=OnLog;}
    }
}

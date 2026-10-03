using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Tidebreak
{
    // Opt-in integration pilot. It writes only to a separate test save directory.
    // Normal launches never add this component.
    public class SmokePilot : MonoBehaviour
    {
        GameDirector g;
        List<string> checks=new List<string>();
        string output;
        bool failed;
        void Click(string text)
        {
            var button=Array.Find(FindObjectsOfType<UnityEngine.UI.Button>(),b=>b.GetComponentInChildren<TMPro.TextMeshProUGUI>().text.StartsWith(text));
            Check(button!=null&&button.interactable,"UI action: "+text);
            if(button)button.onClick.Invoke();
        }
        void Check(bool value,string label)
        {checks.Add((value?"PASS ":"FAIL ")+label);Debug.Log(checks[checks.Count-1]);if(!value)failed=true;}
        void Capture(string name)
        {
            // Render explicitly, so visual QA also works with a hidden test window.
            var cam=g.Player.View;var canvas=FindObjectOfType<Canvas>();
            var mode=canvas.renderMode;var camera=canvas.worldCamera;float plane=canvas.planeDistance;
            var previous=cam.targetTexture;var active=RenderTexture.active;
            var rt=RenderTexture.GetTemporary(1600,900,24,RenderTextureFormat.ARGB32);
            var image=new Texture2D(1600,900,TextureFormat.RGB24,false);
            try {
                canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=.2f;
                cam.targetTexture=rt;Canvas.ForceUpdateCanvases();cam.Render();RenderTexture.active=rt;
                image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());
            } finally {cam.targetTexture=previous;RenderTexture.active=active;canvas.renderMode=mode;canvas.worldCamera=camera;canvas.planeDistance=plane;RenderTexture.ReleaseTemporary(rt);Destroy(image);}
        }
        IEnumerator Start()
        {
            g=GameDirector.Instance;output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Artifacts/Smoke"));Directory.CreateDirectory(output);
            Application.logMessageReceived+=OnLog;
            yield return new WaitForSecondsRealtime(1);Capture("01-harbor");yield return new WaitForSecondsRealtime(.3f);
            Click("海洋图鉴");yield return null;Capture("journal");Click("返回港口");yield return null;
            Click("设置");yield return null;Capture("settings");Click("保存并返回");yield return null;
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-tidebreakVisual")>=0) {
                Click("船长手册");yield return null;Capture("manual");Click("返回");
                File.WriteAllLines(Path.Combine(output,"visual-results.txt"),checks);Application.Quit(failed?1:0);yield break;
            }
            Check(g.UI!=null&&g.Player.View!=null,"runtime bootstrap and camera");
            var startButton=Array.Find(FindObjectsOfType<UnityEngine.UI.Button>(),b=>b.GetComponentInChildren<TMPro.TextMeshProUGUI>().text.StartsWith("开始远征"));
            Check(startButton!=null&&startButton.interactable,"start button exists and is interactive");
            if(startButton)startButton.onClick.Invoke();else g.StartVoyage();g.Run.abyssBait=true;
            Check(g.State==VoyageState.Sailing,"new voyage starts at fishing ground");
            g.TogglePause();float old=g.Run.elapsed;yield return new WaitForSecondsRealtime(.2f);Check(g.Paused&&g.Run.elapsed==old,"pause freezes gameplay");g.TogglePause();
            g.Cast();g.FishingAge=2;g.Tension=.99f;g.TickFishing(true,.5f);Check(g.State==VoyageState.Sailing,"broken fishing line returns to retry state");
            Time.timeScale=3;
            int encounters=0;
            float firstHp=0;bool verifiedHit=false;
            while(encounters<10&&!failed) {
                int stage=g.Run.stage;g.Cast();Check(g.State==VoyageState.Fishing,"stage "+stage+" cast");
                float started=Time.realtimeSinceStartup;
                while(g.State==VoyageState.Fishing&&Time.realtimeSinceStartup-started<30) {
                    g.TickFishing(!g.Surge,Time.deltaTime);
                    if(stage==1&&g.ReelProgress>.25f&&g.ReelProgress<.29f)Capture("02-fishing");
                    yield return null;
                }
                Check(g.State==VoyageState.Combat,"stage "+stage+" fishing completes");
                if(g.State!=VoyageState.Combat)break;
                yield return null;Physics.SyncTransforms();
                Capture(stage==10?"07-kraken":stage==3?"05-boss":stage==1?"03-combat":"stage-"+stage);
                if(stage==1) {
                    g.Player.InvulnerableUntil=0;float hp=g.Run.health;g.Player.TakeDamage(10);Check(g.Run.health<hp,"hostile damage reduces health");
                    g.Player.InvulnerableUntil=0;hp=g.Run.health;g.Warn(new Vector3(0,.23f,0),2,.1f,10);yield return new WaitForSeconds(.2f);Check(g.Run.health<hp,"telegraphed strike damages a stationary player");
                    hp=g.Run.health;g.Player.InvulnerableUntil=0;g.Player.Dash(Vector3.forward);g.Warn(new Vector3(0,.23f,0),3,.05f,10);yield return new WaitForSeconds(.1f);Check(g.Run.health==hp,"dash invulnerability blocks an incoming strike");
                    g.Run.health=g.Run.MaxHealth;
                }
                g.Player.InvulnerableUntil=float.PositiveInfinity;
                started=Time.realtimeSinceStartup;float snap=0;
                while(g.State==VoyageState.Combat&&Time.realtimeSinceStartup-started<90) {
                    if(g.Enemies.Count>0&&g.Enemies[0]) {
                        var e=g.Enemies[0];g.Player.AimAt(e.transform.position);
                        if(!verifiedHit)firstHp=e.health;
                        g.Player.Fire();
                        if(!verifiedHit&&e.health<firstHp){verifiedHit=true;Check(true,"real camera raycast damages enemy");}
                    }
                    if(snap==0&&Time.realtimeSinceStartup-started>.5f){snap=1;Capture(stage==10?"07-kraken":stage==3?"05-boss":stage==1?"03-combat":"stage-"+stage);}
                    yield return null;
                }
                Check(g.State==VoyageState.Reward||g.State==VoyageState.Victory,"stage "+stage+" combat clear");encounters++;
                if(g.State==VoyageState.Reward) {
                    Check(g.Choices.Length==3&&g.Choices[0]!=g.Choices[1]&&g.Choices[0]!=g.Choices[2]&&g.Choices[1]!=g.Choices[2],"three distinct relic choices");
                    if(stage==1){Capture("04-reward");yield return new WaitForSecondsRealtime(.2f);}
                    g.ChooseRelic(0);Check(g.State==VoyageState.Shop,"reward enters shop");
                    int before=g.Run.coins;int cost=g.Price("weapon");bool bought=g.Buy("weapon");Check(!bought||g.Run.coins==before-cost,"purchase subtracts exact price");
                    int wallet=g.Run.coins;g.Run.coins=0;int oldLevel=g.Run.rodLevel;Check(!g.Buy("rod")&&g.Run.coins==0&&g.Run.rodLevel==oldLevel,"insufficient funds cannot buy gear");g.Run.coins=wallet;g.Checkpoint(true);
                    if(stage==2){g.Buy("shotgun");g.Player.Equip(WeaponKind.Scattergun);Check(!g.Run.shotgun||g.Player.weapon==WeaponKind.Scattergun,"shotgun equip");}
                    if(stage==5){g.Buy("harpoon");g.Player.Equip(WeaponKind.Harpoon);Check(!g.Run.harpoon||g.Player.weapon==WeaponKind.Harpoon,"harpoon equip");}
                    var saved=SaveStore.Read<RunData>("voyage");Check(saved!=null&&saved.stage==stage&&saved.coins==g.Run.coins,"checkpoint serialization");
                    if(stage==1){Capture("shop");yield return new WaitForSecondsRealtime(.2f);g.ReturnHarbor();g.StartVoyage(true);Check(g.State==VoyageState.Shop&&g.Run.stage==stage,"resume restores shop checkpoint");}
                    g.OpenRoute();g.ChooseRoute(RouteKind.Shoal);
                } else if(g.State==VoyageState.Victory&&stage==9) {
                    Capture("06-victory");yield return new WaitForSecondsRealtime(.3f);g.ChallengeKraken();Check(g.Run.stage==10&&g.State==VoyageState.Sailing,"rare bait unlocks Kraken encounter");
                } else break;
            }
            Check(g.Run.krakenDefeated&&g.State==VoyageState.Victory,"complete campaign and optional Kraken");
            Check(verifiedHit,"shooting integration observed");
            Capture("08-kraken-victory");yield return new WaitForSecondsRealtime(.3f);
            g.StartVoyage();g.Run.stage=9;g.Run.rareSignal=true;g.SetState(VoyageState.Victory);g.ChallengeWhiteWhale();
            Check(g.Run.stage==11&&g.State==VoyageState.Sailing,"rare signal unlocks white whale alternative");
            g.Run.weaponLevel=5;g.Run.damageRelics=3;g.BeginCombat();yield return null;Capture("09-white-whale");g.Player.InvulnerableUntil=float.PositiveInfinity;
            float whaleStart=Time.realtimeSinceStartup;
            while(g.State==VoyageState.Combat&&Time.realtimeSinceStartup-whaleStart<90){if(g.Enemies.Count>0){g.Player.AimAt(g.Enemies[0].transform.position);g.Player.Fire();}yield return null;}
            Check(g.Run.whaleDefeated&&g.State==VoyageState.Victory,"white whale combat and victory");
            g.StartVoyage();g.BeginCombat();g.Player.InvulnerableUntil=0;g.Player.TakeDamage(10000);Check(g.State==VoyageState.Defeat,"death enters defeat screen");
            Check(SaveStore.Read<RunData>("voyage")==null,"defeat clears current run save");
            File.WriteAllLines(Path.Combine(output,"smoke-results.txt"),checks);Time.timeScale=1;
            Debug.Log(failed?"TIDEBREAK_SMOKE_FAILED":"TIDEBREAK_SMOKE_PASSED");
            yield return new WaitForSecondsRealtime(.3f);Application.Quit(failed?1:0);
        }
        void OnLog(string condition,string trace,LogType type)
        {if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert){failed=true;checks.Add("FAIL runtime error: "+condition);}}
        void OnDestroy(){Application.logMessageReceived-=OnLog;}
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Tidebreak
{
    public partial class SmokePilot
    {
        [Serializable] sealed class DisplayRestartFixture {public int mode,width,height;}

        IEnumerator V08DisplayContracts()
        {
            Time.timeScale=1;
            string step=V05Arg("-v08DisplayStep","Roundtrip");
            File.AppendAllText(Path.Combine(output,"display-scope.txt"),"Actual Windows player Screen.SetResolution transitions, GraphicRaycaster and Button event dispatch, pixel captures, timed reversion and isolated QA CaptainLog persistence. Step: "+step+". These are scripted checks, not a human monitor inspection.\n");
            yield return V08DisplaySettled();
            Check(g.Display&&!g.Display.Busy,"v08 display controller finishes startup transition");
            if(step=="ResumeRestart"){
                var fixture=SaveStore.Read<DisplayRestartFixture>("v08-display-restart");
                Check(fixture!=null,"v08 restart fixture exists from previous independent process");
                if(fixture!=null){
                    Check(g.Display.CurrentMode==(CaptainDisplayMode)fixture.mode&&Screen.fullScreenMode==FullScreenMode.FullScreenWindow,"v08 independent restart applies persisted borderless mode before opening settings");
                    Check(Screen.width==g.Display.DesktopSize.x&&Screen.height==g.Display.DesktopSize.y,"v08 independent restart uses actual native desktop dimensions");
                    Check(g.Log.windowWidth==fixture.width&&g.Log.windowHeight==fixture.height,"v08 independent restart retains previous window dimensions");
                }
            }else Check(Screen.fullScreenMode==FullScreenMode.Windowed,"v08 regular automation starts windowed even when earlier suites saved another mode");
            g.ReturnHarbor();yield return null;
            Check(CampaignClick("设置"),"v08 opens settings through harbor button");yield return null;
            Check(CampaignClick("画面显示"),"v08 opens display page through settings tab");yield return null;
            V08DisplayLayout();

            if(step=="ResumeRestart"){
                yield return new WaitForEndOfFrame();CaptureV08DisplayScreen("display-after-independent-restart");
                yield return V08SelectDisplay(CaptainDisplayMode.Windowed,true);
                Check(Screen.width==g.Log.windowWidth&&Screen.height==g.Log.windowHeight,"v08 restart can return to the persisted window size");
                yield break;
            }
            if(step=="SeedRestart"){
                yield return V08SelectDisplay(CaptainDisplayMode.Borderless,true);
                SaveStore.Write("v08-display-restart",new DisplayRestartFixture{mode=(int)CaptainDisplayMode.Borderless,width=g.Log.windowWidth,height=g.Log.windowHeight});
                Check(SaveStore.LastError==null,"v08 restart fixture and confirmed display selection are written for next process");
                yield return new WaitForEndOfFrame();CaptureV08DisplayScreen("display-before-independent-restart");
                yield break;
            }
            Check(step=="Roundtrip","v08 display suite uses recognized test step");

            // Change the live window via the actual size selector before testing restoration.
            Vector2Int size=g.Display.SupportsWindow(1280,720)?new Vector2Int(1280,720):g.Display.WindowSize;
            if(new Vector2Int(Screen.width,Screen.height)==size&&g.Display.SupportsWindow(1600,900))size=new Vector2Int(1600,900);
            if(new Vector2Int(Screen.width,Screen.height)!=size){
                yield return V08DisplayButtonReady("Display size "+size.x+"x"+size.y);
                Check(V08DisplayClick("Display size "+size.x+"x"+size.y),"v08 raycast and click the window resolution selector");
                yield return V08DisplaySettled();
                Check(g.Display.AwaitingConfirmation&&Screen.width==size.x&&Screen.height==size.y,"v08 window size really changes before confirmation");
                yield return V08DisplayButtonReady("Display keep");
                Check(V08DisplayClick("Display keep"),"v08 confirms the actual window resize through UI");yield return null;
            }
            Vector2Int originalWindow=new Vector2Int(Screen.width,Screen.height);
            yield return new WaitForEndOfFrame();CaptureV08DisplayScreen("display-window-before-switch");V08DisplayLayout();
            yield return V08SelectDisplay(CaptainDisplayMode.Borderless,true);
            yield return V08SelectDisplay(CaptainDisplayMode.Exclusive,true);
            yield return V08SelectDisplay(CaptainDisplayMode.Windowed,true);
            Check(Screen.width==originalWindow.x&&Screen.height==originalWindow.y,"v08 returning from exclusive restores the original window dimensions");

            int savedMode=g.Log.displayMode,savedWidth=g.Log.windowWidth,savedHeight=g.Log.windowHeight;
            yield return V08SelectDisplay(CaptainDisplayMode.Borderless,false);
            Check(g.Log.displayMode==savedMode&&g.Log.windowWidth==savedWidth&&g.Log.windowHeight==savedHeight,"v08 unconfirmed preview does not overwrite saved display settings");
            float waitStarted=Time.realtimeSinceStartup;
            while((g.Display.AwaitingConfirmation||g.Display.Busy)&&Time.realtimeSinceStartup-waitStarted<DisplaySettings.ConfirmationDuration+16)yield return null;
            Check(!g.Display.AwaitingConfirmation&&!g.Display.Busy&&Screen.fullScreenMode==FullScreenMode.Windowed,"v08 unconfirmed display change actually reverts after countdown");
            Check(Screen.width==originalWindow.x&&Screen.height==originalWindow.y,"v08 timed reversion restores exact window dimensions");
            yield return new WaitForEndOfFrame();CaptureV08DisplayScreen("display-timeout-restored");

            yield return V08SelectDisplay(CaptainDisplayMode.Exclusive,false);
            yield return V08DisplayButtonReady("Display revert");
            Check(V08DisplayClick("Display revert"),"v08 clicks explicit restore control through the GraphicRaycaster");
            yield return V08DisplaySettled();
            Check(Screen.fullScreenMode==FullScreenMode.Windowed&&Screen.width==originalWindow.x&&Screen.height==originalWindow.y,"v08 explicit restore changes actual mode and dimensions");
            var saved=SaveStore.Read<CaptainLog>("captain");
            Check(saved!=null&&saved.displayVersion==1&&saved.displayMode==(int)CaptainDisplayMode.Windowed&&saved.windowWidth==originalWindow.x&&saved.windowHeight==originalWindow.y,"v08 disk CaptainLog reload retains last confirmed window setting");
            Check(!g.Display.RequestMode((CaptainDisplayMode)99)&&!g.Display.RequestWindowSize(0,0),"v08 invalid display mode and impossible window selection are rejected before changing the player");
            yield return null;
            Check(!g.Display.Busy&&!g.Display.AwaitingConfirmation&&Screen.fullScreenMode==FullScreenMode.Windowed&&Screen.width==originalWindow.x&&Screen.height==originalWindow.y,"v08 rejected selections leave actual mode, dimensions and confirmation state intact");
            V08DisplayLayout();
            yield return V08DisplayButtonReady("Display return");
            Check(V08DisplayClick("Display return"),"v08 returns to harbor through saved settings footer");yield return null;
            yield return V08DisplayPauseNavigation(originalWindow);
        }
        IEnumerator V08DisplayPauseNavigation(Vector2Int originalWindow)
        {
            // Exercise the production Escape handler used by GameDirector.Update;
            // this is deterministic input-handler dispatch, not an injected OS key.
            g.StartVoyage();g.SkipCinematic();g.BeginCombat(false);g.TogglePause();yield return null;
            Check(g.Paused&&g.State==VoyageState.Combat&&Time.timeScale==0,"v08 display navigation fixture is a genuinely paused active encounter");
            Check(V08DisplayClick("Action 设置"),"v08 opens settings with the actual paused encounter button");yield return null;
            Check(V08DisplayClick("Action 画面显示"),"v08 opens display tab while encounter remains paused");yield return null;
            yield return V08DisplayButtonReady("Display mode Borderless");
            Check(V08DisplayClick("Display mode Borderless"),"v08 starts real borderless switch from paused encounter");
            Check(g.Display.Busy&&!g.HandleEscapeNavigation()&&g.Paused&&Time.timeScale==0&&V08DisplayPageVisible(),"v08 Escape during mode transition keeps the settings page and paused encounter");
            yield return V08DisplaySettled();
            Check(g.Display.AwaitingConfirmation&&Screen.fullScreenMode==FullScreenMode.FullScreenWindow,"v08 paused encounter reaches real borderless preview");
            yield return V08DisplayButtonReady("Action 声音与操作");
            Check(V08DisplayClick("Action 声音与操作"),"v08 clicks actual audio tab during unconfirmed preview");
            Check(g.Paused&&Time.timeScale==0&&g.Display.Busy&&V08DisplayPageVisible(),"v08 tab navigation starts reversion without hiding confirmation page or resuming combat");
            yield return V08DisplaySettled();
            Check(!g.Display.AwaitingConfirmation&&g.Paused&&Time.timeScale==0&&Screen.fullScreenMode==FullScreenMode.Windowed&&Screen.width==originalWindow.x&&Screen.height==originalWindow.y,"v08 tab cancellation restores actual window while keeping combat paused");
            yield return new WaitForEndOfFrame();CaptureV08DisplayScreen("display-paused-tab-restored");

            yield return V08SelectDisplay(CaptainDisplayMode.Exclusive,false,"-paused-escape");
            int secondsBefore=g.Display.SecondsRemaining;yield return new WaitForSecondsRealtime(1.1f);
            Check(g.Display.AwaitingConfirmation&&g.Display.SecondsRemaining<secondsBefore&&g.Paused&&Time.timeScale==0,"v08 confirmation countdown advances in real time while encounter is paused");
            Check(!g.HandleEscapeNavigation()&&g.Display.Busy&&g.Paused&&Time.timeScale==0&&V08DisplayPageVisible(),"v08 production Escape handler cancels preview while retaining paused settings");
            Check(!g.HandleEscapeNavigation()&&g.Paused&&Time.timeScale==0,"v08 repeated Escape cannot resume combat while restoration is pending");
            yield return V08DisplaySettled();
            Check(Screen.fullScreenMode==FullScreenMode.Windowed&&Screen.width==originalWindow.x&&Screen.height==originalWindow.y&&g.Paused,"v08 Escape cancellation restores exact window without resuming combat");
            yield return V08DisplayButtonReady("Action 声音与操作");
            Check(V08DisplayClick("Action 声音与操作"),"v08 audio tab works normally after preview reversion");yield return null;
            Check(!V08DisplayPageVisible()&&g.Paused&&Time.timeScale==0,"v08 audio page navigation remains within paused encounter");
            Check(V08DisplayClick("Action 画面显示"),"v08 display page can be reopened after cancellation");yield return null;
            yield return V08DisplayButtonReady("Display return");
            Check(V08DisplayClick("Display return")&&g.Paused,"v08 settings footer returns to pause menu after cancellation");yield return null;
            Check(g.HandleEscapeNavigation()&&!g.Paused&&Time.timeScale==1,"v08 Escape resumes combat normally once display navigation is complete");
            g.ReturnHarbor();yield return null;
        }
        bool V08DisplayPageVisible(){return FindObjectsOfType<TMP_Text>().Any(t=>t.isActiveAndEnabled&&t.name=="Display current");}
        IEnumerator V08DisplaySettled()
        {
            float until=Time.realtimeSinceStartup+16;
            while(g.Display.Busy&&Time.realtimeSinceStartup<until)yield return null;
            yield return new WaitForSecondsRealtime(.15f);Canvas.ForceUpdateCanvases();
        }
        IEnumerator V08SelectDisplay(CaptainDisplayMode mode,bool confirm,string captureSuffix="")
        {
            yield return V08DisplayButtonReady("Display mode "+mode);
            Check(V08DisplayClick("Display mode "+mode),"v08 raycast and click actual display mode selector: "+mode);
            yield return V08DisplaySettled();
            Check(!g.Display.Busy&&g.Display.AwaitingConfirmation&&Screen.fullScreenMode==DisplaySettings.ToUnity(mode),"v08 actual Screen.fullScreenMode reaches "+mode+" and offers confirmation");
            var expected=mode==CaptainDisplayMode.Windowed?g.Display.WindowSize:g.Display.DesktopSize;
            Check(Screen.width==expected.x&&Screen.height==expected.y,"v08 actual render dimensions match "+mode+" target "+expected);
            V08DisplayLayout();
            yield return new WaitForEndOfFrame();CaptureV08DisplayScreen("display-"+mode+(confirm?"-confirm":"-preview")+captureSuffix);
            if(confirm){
                yield return V08DisplayButtonReady("Display keep");
                Check(V08DisplayClick("Display keep"),"v08 clicks keep for "+mode);yield return null;
                var saved=SaveStore.Read<CaptainLog>("captain");
                Check(!g.Display.AwaitingConfirmation&&saved!=null&&saved.displayMode==(int)mode,"v08 "+mode+" confirmation persists on disk");
            }
        }
        IEnumerator V08DisplayButtonReady(string name)
        {
            // A mode coroutine can clear Busy after SeaHUD.Update. EndOfFrame alone
            // does not run the UI again: wait for the actual next interactive frame.
            yield return null;
            float until=Time.realtimeSinceStartup+3;bool ready=false;
            while(Time.realtimeSinceStartup<until){
                ready=!g.Display.Busy&&FindObjectsOfType<UnityEngine.UI.Button>().Any(b=>b.isActiveAndEnabled&&b.interactable&&b.name==name);
                if(ready)break;yield return null;
            }
            Canvas.ForceUpdateCanvases();
            Check(ready,"v08 display control becomes interactive after asynchronous transition: "+name);
        }
        bool V08DisplayClick(string name)
        {
            var button=FindObjectsOfType<UnityEngine.UI.Button>().FirstOrDefault(b=>b.isActiveAndEnabled&&b.interactable&&b.name==name);
            if(!button){File.AppendAllText(Path.Combine(output,"display-ui-raycasts.txt"),name+" unavailable: busy="+g.Display.Busy+" confirmation="+g.Display.AwaitingConfirmation+" mode="+Screen.fullScreenMode+"\n");return false;}
            Canvas.ForceUpdateCanvases();var rect=(RectTransform)button.transform;
            var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center))};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            bool reached=hits.Count>0&&hits[0].gameObject.GetComponentInParent<UnityEngine.UI.Button>()==button;
            File.AppendAllText(Path.Combine(output,"display-ui-raycasts.txt"),name+" mode="+Screen.fullScreenMode+" resolution="+Screen.width+"x"+Screen.height+" point="+pointer.position+" top="+(hits.Count>0?hits[0].gameObject.name:"none")+"\n");
            if(!reached)return false;
            return ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
        }
        void V08DisplayLayout()
        {
            Canvas.ForceUpdateCanvases();
            foreach(var label in FindObjectsOfType<TMP_Text>().Where(t=>t.isActiveAndEnabled&&t.name.StartsWith("Display "))){
                label.ForceMeshUpdate();var corners=new Vector3[4];label.rectTransform.GetWorldCorners(corners);
                bool inside=corners.All(c=>c.x>=-1&&c.x<=Screen.width+1&&c.y>=-1&&c.y<=Screen.height+1);
                bool glyphs=label.textInfo.characterInfo.Take(label.textInfo.characterCount).Where(c=>!char.IsWhiteSpace(c.character)).All(c=>c.isVisible&&c.topRight.y>c.bottomLeft.y);
                Check(inside&&glyphs&&!label.isTextOverflowing,"v08 display label remains on screen with rendered glyphs: "+label.name+" / "+Screen.width+"x"+Screen.height);
            }
        }
        void CaptureV08DisplayScreen(string name)
        {
            var texture=ScreenCapture.CaptureScreenshotAsTexture();
            try{
                bool content=false;
                for(int y=16;y<texture.height;y+=32)for(int x=16;x<texture.width;x+=32)content|=texture.GetPixel(x,y).maxColorComponent>.08f;
                if(content){
                    File.WriteAllBytes(Path.Combine(output,name+".png"),texture.EncodeToPNG());
                    File.AppendAllText(Path.Combine(output,"display-captures.txt"),name+": native framebuffer; "+Screen.fullScreenMode+"; "+texture.width+"x"+texture.height+"\n");
                    Check(texture.width==Screen.width&&texture.height==Screen.height,"v08 "+name+" native framebuffer has actual screen dimensions");
                }else{
                    CapturePresentation(name,Screen.width,Screen.height);
                    File.AppendAllText(Path.Combine(output,"display-captures.txt"),name+": hidden-window black backbuffer; separate world/UI camera render; actual Screen mode="+Screen.fullScreenMode+"; "+Screen.width+"x"+Screen.height+"\n");
                }
            }finally{Destroy(texture);}
        }
    }
}

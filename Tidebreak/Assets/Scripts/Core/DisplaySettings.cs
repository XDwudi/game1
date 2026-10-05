using System;
using System.Collections;
using UnityEngine;

namespace Tidebreak
{
    // Persist our own values: Unity's FullScreenMode enum also contains macOS-only modes.
    public enum CaptainDisplayMode { Windowed, Borderless, Exclusive }

    public sealed class DisplaySettings : MonoBehaviour
    {
        public const float ConfirmationDuration=15;
        CaptainLog log;
        bool automation;
        int desktopWidth,desktopHeight;
        Vector2Int restoredWindow,previousSize;
        CaptainDisplayMode previousMode;
        Coroutine transition;
        float confirmationUntil,resizeSince;
        Vector2Int observedSize;
        public bool Busy {get;private set;}
        public bool AwaitingConfirmation {get;private set;}
        public bool LastTransitionSucceeded {get;private set;}
        public string Status {get;private set;}
        public CaptainDisplayMode CurrentMode {get{return FromUnity(Screen.fullScreenMode);}}
        public Vector2Int DesktopSize {get{return new Vector2Int(desktopWidth,desktopHeight);}}
        public Vector2Int WindowSize {get{return restoredWindow;}}
        public int SecondsRemaining {get{return Mathf.Max(0,Mathf.CeilToInt(confirmationUntil-Time.realtimeSinceStartup));}}

        public static string Label(CaptainDisplayMode mode)
        {return mode==CaptainDisplayMode.Exclusive?"全屏（独占）":mode==CaptainDisplayMode.Borderless?"窗口全屏（无边框）":"窗口";}
        public static FullScreenMode ToUnity(CaptainDisplayMode mode)
        {return mode==CaptainDisplayMode.Exclusive?FullScreenMode.ExclusiveFullScreen:mode==CaptainDisplayMode.Borderless?FullScreenMode.FullScreenWindow:FullScreenMode.Windowed;}
        public static CaptainDisplayMode FromUnity(FullScreenMode mode)
        {return mode==FullScreenMode.ExclusiveFullScreen?CaptainDisplayMode.Exclusive:mode==FullScreenMode.FullScreenWindow?CaptainDisplayMode.Borderless:CaptainDisplayMode.Windowed;}

        public void Init(CaptainLog saved,bool smoke)
        {
            log=saved;automation=smoke;
            desktopWidth=Mathf.Max(640,UnityEngine.Display.main.systemWidth);
            desktopHeight=Mathf.Max(360,UnityEngine.Display.main.systemHeight);
            if(desktopWidth<=640||desktopHeight<=360){desktopWidth=Mathf.Max(1280,Screen.currentResolution.width);desktopHeight=Mathf.Max(720,Screen.currentResolution.height);}
            if(log.displayVersion<1){log.displayMode=0;log.windowWidth=1600;log.windowHeight=900;log.displayVersion=1;}
            log.displayMode=Mathf.Clamp(log.displayMode,0,2);
            restoredWindow=FitWindow(log.windowWidth,log.windowHeight);
            log.windowWidth=restoredWindow.x;log.windowHeight=restoredWindow.y;
            observedSize=new Vector2Int(Screen.width,Screen.height);
            bool replay=smoke&&Array.IndexOf(Environment.GetCommandLineArgs(),"-v08DisplayResume")>=0;
            var mode=smoke&&!replay?CaptainDisplayMode.Windowed:(CaptainDisplayMode)log.displayMode;
            Vector2Int size=mode==CaptainDisplayMode.Windowed?(smoke&&!replay?FitWindow(Screen.width,Screen.height):restoredWindow):DesktopSize;
            transition=StartCoroutine(Apply(mode,size,false));
        }

        public Vector2Int FitWindow(int width,int height)
        {
            if(width<640||height<360){width=1600;height=900;}
            int maxWidth=Mathf.Max(640,desktopWidth-48),maxHeight=Mathf.Max(360,desktopHeight-72);
            float scale=Mathf.Min(1,Mathf.Min(maxWidth/(float)width,maxHeight/(float)height));
            return new Vector2Int(Mathf.Max(640,Mathf.RoundToInt(width*scale)),Mathf.Max(360,Mathf.RoundToInt(height*scale)));
        }
        public bool SupportsWindow(int width,int height){return FitWindow(width,height)==new Vector2Int(width,height);}

        public bool RequestMode(CaptainDisplayMode mode)
        {
            if(Busy||AwaitingConfirmation||!Enum.IsDefined(typeof(CaptainDisplayMode),mode))return false;
            if(CurrentMode==mode)return false;
            SavePrevious();
            transition=StartCoroutine(Apply(mode,mode==CaptainDisplayMode.Windowed?restoredWindow:DesktopSize,true));return true;
        }
        public bool RequestWindowSize(int width,int height)
        {
            if(Busy||AwaitingConfirmation||CurrentMode!=CaptainDisplayMode.Windowed||!SupportsWindow(width,height))return false;
            var size=new Vector2Int(width,height);if(size==new Vector2Int(Screen.width,Screen.height))return false;
            SavePrevious();transition=StartCoroutine(Apply(CaptainDisplayMode.Windowed,size,true));return true;
        }
        void SavePrevious()
        {
            previousMode=CurrentMode;previousSize=new Vector2Int(Screen.width,Screen.height);
            if(previousMode==CaptainDisplayMode.Windowed)restoredWindow=FitWindow(previousSize.x,previousSize.y);
        }
        IEnumerator Apply(CaptainDisplayMode mode,Vector2Int size,bool preview)
        {
            Busy=true;LastTransitionSucceeded=false;Status="正在切换显示模式…";
            Screen.SetResolution(size.x,size.y,ToUnity(mode));
            int stable=0;
            // Screen.SetResolution is deferred until the end of the frame. Check the actual result.
            yield return new WaitForSecondsRealtime(.25f);
            // Awake still builds the island after Init starts this coroutine. Begin
            // the timeout after yielding so a slow first load is not a rejection.
            float until=Time.realtimeSinceStartup+7;
            while(Time.realtimeSinceStartup<until&&stable<4){
                stable=Screen.fullScreenMode==ToUnity(mode)&&Screen.width==size.x&&Screen.height==size.y?stable+1:0;
                yield return null;
            }
            if(stable<4){
                Status="显示器未接受此设置，正在恢复窗口…";AwaitingConfirmation=false;
                var safe=FitWindow(log.windowWidth,log.windowHeight);Screen.SetResolution(safe.x,safe.y,FullScreenMode.Windowed);
                stable=0;
                yield return new WaitForSecondsRealtime(.25f);
                until=Time.realtimeSinceStartup+7;
                while(Time.realtimeSinceStartup<until&&stable<4){
                    stable=Screen.fullScreenMode==FullScreenMode.Windowed&&Screen.width==safe.x&&Screen.height==safe.y?stable+1:0;
                    yield return null;
                }
                if(stable>=4){
                    log.displayMode=0;restoredWindow=safe;log.windowWidth=safe.x;log.windowHeight=safe.y;
                    if(!automation)SaveStore.Write("captain",log);
                    Status="显示器未接受此设置，已恢复窗口。";
                }else Status="显示切换未完成；保留了此前的保存设置，请重启游戏恢复。";
            }else{
                LastTransitionSucceeded=true;
                if(preview){AwaitingConfirmation=true;confirmationUntil=Time.realtimeSinceStartup+ConfirmationDuration;Status="画面清晰且完整时，请保留更改。";}
                else Status="显示设置已生效。";
            }
            observedSize=new Vector2Int(Screen.width,Screen.height);resizeSince=Time.realtimeSinceStartup;Busy=false;transition=null;
        }
        public bool Confirm()
        {
            if(Busy||!AwaitingConfirmation)return false;
            AwaitingConfirmation=false;
            if(CurrentMode==CaptainDisplayMode.Windowed)restoredWindow=FitWindow(Screen.width,Screen.height);
            log.displayVersion=1;log.displayMode=(int)CurrentMode;log.windowWidth=restoredWindow.x;log.windowHeight=restoredWindow.y;
            SaveStore.Write("captain",log);
            bool saved=SaveStore.LastError==null;
            Status=saved?"显示设置已保存，下次启动自动使用。":"显示已应用，但保存失败；请检查存档目录是否可写。";
            return saved;
        }
        public bool TryLeavePreview()
        {
            // Escape, tabs and menu navigation share the same guard. Reverting
            // keeps the current panel and pause state until the screen is stable.
            if(Busy)return false;
            if(AwaitingConfirmation){Revert();return false;}
            return true;
        }
        public bool Revert()
        {
            if(Busy||!AwaitingConfirmation)return false;
            // This nested coroutine may not enter Apply until the next frame.
            // Keep the controller busy throughout, including that scheduling gap.
            AwaitingConfirmation=false;Busy=true;transition=StartCoroutine(RestorePrevious());return true;
        }
        IEnumerator RestorePrevious()
        {
            yield return Apply(previousMode,previousSize,false);
            if(LastTransitionSucceeded)Status="已恢复切换前的显示设置。";
        }
        void Update()
        {
            if(AwaitingConfirmation&&!Busy&&Time.realtimeSinceStartup>=confirmationUntil){Revert();return;}
            if(automation||Busy||AwaitingConfirmation||CurrentMode!=CaptainDisplayMode.Windowed)return;
            var size=new Vector2Int(Screen.width,Screen.height);
            if(size!=observedSize){observedSize=size;resizeSince=Time.realtimeSinceStartup;return;}
            if(Time.realtimeSinceStartup-resizeSince<1||size==restoredWindow)return;
            restoredWindow=FitWindow(size.x,size.y);log.windowWidth=restoredWindow.x;log.windowHeight=restoredWindow.y;
            SaveStore.Write("captain",log);resizeSince=float.PositiveInfinity;
        }
        void OnDestroy(){if(transition!=null)StopCoroutine(transition);}
    }
}

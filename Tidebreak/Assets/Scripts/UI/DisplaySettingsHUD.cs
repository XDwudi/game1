using System;
using TMPro;
using UnityEngine;

namespace Tidebreak
{
    public partial class SeaHUD
    {
        TextMeshProUGUI displayCurrent,displayMessage,displayWindowHint;
        UnityEngine.UI.Button[] displayModes,displaySizes;
        TextMeshProUGUI[] displayModeLabels;
        UnityEngine.UI.Button displayKeep,displayRevert,displayReturn;
        readonly CaptainDisplayMode[] displayOrder={CaptainDisplayMode.Exclusive,CaptainDisplayMode.Borderless,CaptainDisplayMode.Windowed};
        readonly Vector2Int[] windowChoices={new Vector2Int(1280,720),new Vector2Int(1600,900),new Vector2Int(1920,1080)};

        UnityEngine.UI.Button DisplayButton(Transform parent,float x,float y,float w,float h,string name,string label,Action action,bool brass=false)
        {
            var plate=Plate(parent,x,y,w,h,brass?new Color(.79f,.66f,.43f):new Color(.085f,.185f,.215f),brass,name);plate.raycastTarget=true;
            var button=plate.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=plate;
            var colors=button.colors;colors.highlightedColor=new Color(1.12f,1.12f,1.12f);colors.pressedColor=new Color(.72f,.86f,.8f);colors.disabledColor=new Color(.58f,.62f,.63f,.65f);button.colors=colors;
            var title=Text(plate.transform,15,2,w-30,h-4,label,18,brass?Ink:Cream,TextAlignmentOptions.Midline);title.name=name+" label";title.enableWordWrapping=false;
            button.onClick.AddListener(()=>{game.Audio.Cue("select");action();});return button;
        }
        void BuildDisplaySettings(RectTransform p,bool paused)
        {
            displayModes=new UnityEngine.UI.Button[3];displayModeLabels=new TextMeshProUGUI[3];
            string[] descriptions={"独占屏幕，使用显示器原生分辨率。\n适合专注战斗；切换应用时需稍等。","无边框铺满桌面，使用原生分辨率。\n便于快速切换应用和查看攻略。","保留标题栏，可以拖动和调整窗口。\n切回窗口时恢复上次使用的大小。"};
            for(int i=0;i<3;i++){
                int choice=i;float x=80+i*489;
                Plate(p,x,346,462,194,new Color(.045f,.105f,.13f,.96f));
                Text(p,x+22,359,418,38,DisplaySettings.Label(displayOrder[i]),24,Cream).name="Display title "+displayOrder[i];
                Text(p,x+22,407,418,66,descriptions[i],17,Muted);
                displayModes[i]=DisplayButton(p,x+20,482,422,42,"Display mode "+displayOrder[i],"使用此模式",()=>game.Display.RequestMode(displayOrder[choice]));
                displayModeLabels[i]=displayModes[i].GetComponentInChildren<TextMeshProUGUI>();
            }
            Icon(p,82,564,27,NauticalMark.Compass,Gold);
            displayCurrent=Text(p,122,560,1398,38,"",23,Gold);displayCurrent.name="Display current";
            displayWindowHint=Text(p,80,611,1440,28,"",17,Muted);displayWindowHint.name="Display window hint";
            displaySizes=new UnityEngine.UI.Button[3];
            for(int i=0;i<3;i++){
                int choice=i;var size=windowChoices[i];
                displaySizes[i]=DisplayButton(p,80+i*267,651,250,43,"Display size "+size.x+"x"+size.y,size.x+" × "+size.y,()=>game.Display.RequestWindowSize(windowChoices[choice].x,windowChoices[choice].y));
            }
            displayMessage=Text(p,80,716,950,41,"",18,Cream);displayMessage.name="Display status";
            displayKeep=DisplayButton(p,1050,712,225,47,"Display keep","保留更改",()=>game.Display.Confirm(),true);
            displayRevert=DisplayButton(p,1294,712,226,47,"Display revert","恢复原设置",()=>game.Display.Revert());
            displayReturn=DisplayButton(p,80,787,420,56,"Display return","保存并返回",()=>{SaveStore.Write("captain",game.Log);settings=false;if(paused)ShowPause();else ShowHarbor();},true);
            Text(p,860,802,660,30,"显示方式不改变画质、视野或战斗速度。",17,Muted,TextAlignmentOptions.Right);
            UpdateDisplaySettings();
        }
        void UpdateDisplaySettings()
        {
            if(!displayCurrent||!displayCurrent.gameObject.activeInHierarchy||!game.Display)return;
            var display=game.Display;bool waiting=display.AwaitingConfirmation,available=!display.Busy&&!waiting;
            displayCurrent.text="当前  /  "+DisplaySettings.Label(display.CurrentMode)+"    ·    "+Screen.width+" × "+Screen.height;
            displayWindowHint.text=display.CurrentMode==CaptainDisplayMode.Windowed?"窗口分辨率  /  可拖动窗口边缘自由调整；当前尺寸会自动记住。":"窗口分辨率  /  切回窗口时恢复 "+display.WindowSize.x+" × "+display.WindowSize.y+"；全屏使用显示器原生尺寸。";
            for(int i=0;i<3;i++){
                bool selected=display.CurrentMode==displayOrder[i];displayModes[i].interactable=available&&!selected;
                displayModeLabels[i].text=selected?"●  当前模式":"使用此模式";displayModeLabels[i].color=selected?Gold:Cream;
                displaySizes[i].interactable=available&&display.CurrentMode==CaptainDisplayMode.Windowed&&display.SupportsWindow(windowChoices[i].x,windowChoices[i].y)&&new Vector2Int(Screen.width,Screen.height)!=windowChoices[i];
            }
            displayMessage.text=waiting?"请确认画面正常  ·  "+display.SecondsRemaining+" 秒后自动恢复  ·  ESC 恢复原设置":display.Status;
            displayMessage.color=waiting?Gold:Muted;
            displayKeep.gameObject.SetActive(waiting);displayRevert.gameObject.SetActive(waiting);
            displayKeep.interactable=displayRevert.interactable=!display.Busy;displayReturn.interactable=available;
        }
    }
}

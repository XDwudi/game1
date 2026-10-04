using TMPro;
using UnityEngine;
namespace Tidebreak
{
    public partial class SeaHUD
    {
        RectTransform reloadPanel,mechanicPanel;
        TextMeshProUGUI reloadLabel,mechanicLabel,buildStatus;
        UnityEngine.UI.Image reloadFill;
        void UpdateCombatReadability()
        {
            if(!reloadPanel){
                reloadPanel=Rect("Tactical reload",hud,640,555,320,54);Box(reloadPanel,0,0,320,54,Panel);reloadLabel=Text(reloadPanel,8,4,304,25,"",15,Cream,TextAlignmentOptions.Center);
                reloadFill=Bar(reloadPanel,18,37,284,6,Gold);Box(reloadPanel,18+284*.55f,35,284*.17f,10,new Color(.3f,1,.7f,.45f));
                mechanicPanel=Rect("Encounter instructions",hud,478,135,720,72);Box(mechanicPanel,0,0,720,72,new Color(Ink.r,Ink.g,Ink.b,.88f));mechanicLabel=Text(mechanicPanel,16,8,688,58,"",18,Gold,TextAlignmentOptions.Center);
                buildStatus=Text(hud,400,706,800,32,"",17,Mint,TextAlignmentOptions.Center);
            }
            reloadPanel.gameObject.SetActive(game.Player.Reloading);reloadFill.fillAmount=game.Player.ReloadProgress;
            reloadLabel.text=!game.Player.QuickReloadAvailable?"速装已尝试 · 等待上膛":game.Player.ReloadProgress>=.55f&&game.Player.ReloadProgress<=.72f?"现在按 R · 精准装填":"装填中 · 绿色区间再次按 R";
            buildStatus.text=game.Player.Synergy?game.Player.Synergy.Status:"";
            Enemy boss=null;foreach(var e in game.Enemies)if(e&&e.IsBoss){boss=e;break;}
            mechanicPanel.gameObject.SetActive(boss&&game.State==VoyageState.Combat);
            if(boss&&boss.Encounter){var brain=boss.Encounter;mechanicLabel.text=brain.Targets.Count>0?"潮核封锁 · 先击碎岸上的发光机关  "+brain.Targets.Count+" 个\n"+brain.Targets[0].Label:brain.Windup>0?brain.Cue:brain.Recovering?"破招窗口 · 瞄准核心，同时留意反扑预警":brain.Cue;}
            RaycastHit hitInfo;if(game.IsPlaying&&Physics.Raycast(game.Player.View.transform.position,game.Player.View.transform.forward,out hitInfo,100,~((1<<9)|(1<<30)))){
                var target=hitInfo.collider.GetComponentInParent<EncounterTarget>();if(target)targetName.text="破招机关 · "+target.Label+"\n"+Mathf.CeilToInt(target.Health)+" / "+Mathf.CeilToInt(target.Maximum)+"  ·  射击摧毁";
            }
        }
        void BuildKeystoneStock(RectTransform p)
        {
            var r=game.Run;int act=Mathf.Clamp((r.maxIsland-2)/3,0,2);
            for(int a=0;a<3;a++){int selected=a;Button(p,80+a*300,324,280,37,"第 "+(a+1)+" 幕 · 岛屿 "+VoyageBuilds.Island(a)+" 解锁",()=>{keystoneAct=selected;BuildShopPage();},keystoneAct==a);}
            act=keystoneAct;
            for(int i=0;i<3;i++){int index=act*3+i;float x=80+i*486;bool own=r.HasKeystone(index),can=VoyageBuilds.CanBuy(r,index);
                Box(p,x,382,464,316,Panel);Text(p,x+22,398,420,43,VoyageBuilds.Names[index],29,own?Mint:Gold);Text(p,x+22,462,420,141,VoyageBuilds.Descriptions[index],20,Cream);
                string label=own?"已装配":r.HasActKeystone(act)?"本幕已选择另一专精":r.maxIsland<VoyageBuilds.Island(act)?"抵达第 "+VoyageBuilds.Island(act)+" 岛解锁":VoyageBuilds.Price(act)+" 金币 · "+(can?"购买专精":"金币不足");
                Button(p,x+22,627,420,49,label,()=>game.BuyKeystone(index),can,can);
            }
            Text(p,80,723,1430,61,"每幕三选一，三个专精可以跨幕组合。先想好打法，再用金币购买。\n普通配件补足属性；专精决定你如何输出、躲避和生存。",19,Muted);
        }
        int keystoneAct;
        public void ShowBuilds(){shopTab=4;BuildShopPage();}
    }
}

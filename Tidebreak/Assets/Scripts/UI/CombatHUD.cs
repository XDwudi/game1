using TMPro;
using UnityEngine;
namespace Tidebreak
{
    public partial class SeaHUD
    {
        RectTransform reloadPanel,mechanicPanel,mechanicMeterPanel;
        TextMeshProUGUI reloadLabel,mechanicLabel,buildStatus,mechanicTitle,mechanicMeterLabel,mechanicDetail;
        UnityEngine.UI.Image reloadFill,mechanicProgress,mechanicMeterA,mechanicMeterB;
        static readonly string[] MechanismTitles={"引钳撞桩","音贝应答","净水造岸","折光破甲","拉钟截潮","护火破冰","导雷接地","双阀淬甲","逆行归忆","钓竿牵腕","声呐追鲸"};
        static readonly string[] MechanismStrategies={"借重钳撞断系船桩。\n蓝线追踪，橙线锁定后侧闪。","先听完整乐谱，再依序回应。\n数字与贝壳亮灯给出同样信息。","把清水带到污染环。\n净化的绿圈也是安全地面。","让三面镜连续接通光路。\n沿光束寻找下一面反光镜。","用钟声打断浪潮蓄势。\n提前拉绳无效，成功拍次保留。","热炉需要有人伴行。\n补热时停船，落冰时暂离。","将电荷引入指定接地圈。\n先看路线，再领取电荷。","两阀分别控制温度与压力。\n安全区内才积累淬火进度。","你走过的路会成为战场。\n记录后逆行，取回足迹就离开。","用钓线牵制登陆触腕。\n平潮收线，红潮放线。","先发声呐，再二次定位。\n双环是真回波，单环是假象。"};
        string MechanismInput(BossMechanism m)
        {
            switch(m.BossIndex){
                case 0:return m.TargetLocked?"SHIFT · 现在侧闪":"WASD · 移至亮桩";
                case 1:return m.Listening?"观察亮灯 · 聆听乐谱":"E · 回应音贝";
                case 2:return m.Carrying?"按住 E · 净化":"E · 取清水";
                case 3:return "E · 转动反光镜";
                case 4:return m.TimingWindowOpen?"E · 现在拉绳":"等待钟拍 · 绿灯时按 E";
                case 5:return "按住 E · 停车补热    松开 · 伴行";
                case 6:return m.Carrying?"WASD / SHIFT · 赶往接地圈":"E · 领取电荷";
                case 7:return "E · 开启阀门";
                case 8:return m.StateStep==0?"E · 开始记录":m.StateStep==1?"WASD · 走出新足迹":"WASD · 沿原路逆行";
                case 9:return !game.Player.RodEquipped?"1 · 切换鱼竿":!m.Carrying?"E · 瞄准触腕挂钩":m.Surge?m.Phase>=2?"松开 E · 换到新金圈":"松开 E · 降低张力":m.Braced?"按住 E · 收线":"WASD · 移入金色牵引圈";
                default:return m.StateStep==0?"E · 发射声呐":m.StateStep==2?"锁定变橙后横移 · SHIFT 侧闪":"E · 观测点二次定位";
            }
        }
        void UpdateCombatReadability()
        {
            if(!reloadPanel){
                reloadPanel=Rect("Tactical reload",hud,640,555,320,54);Box(reloadPanel,0,0,320,54,Panel);reloadLabel=Text(reloadPanel,8,4,304,25,"",15,Cream,TextAlignmentOptions.Center);
                reloadFill=Bar(reloadPanel,18,37,284,6,Gold);Box(reloadPanel,18+284*.55f,35,284*.17f,10,new Color(.3f,1,.7f,.45f));
                mechanicPanel=Rect("Encounter action instrument",hud,457,130,686,139);
                Box(mechanicPanel,0,0,686,139,new Color(Ink.r,Ink.g,Ink.b,.91f));Box(mechanicPanel,0,0,4,139,Gold);
                mechanicTitle=Text(mechanicPanel,17,9,475,25,"",18,Gold);mechanicDetail=Text(mechanicPanel,486,9,181,25,"",15,Mint,TextAlignmentOptions.Right);
                mechanicLabel=Text(mechanicPanel,17,41,650,53,"",18,Cream);mechanicLabel.overflowMode=TextOverflowModes.Ellipsis;
                mechanicProgress=Bar(mechanicPanel,17,99,650,5,Mint);
                mechanicMeterPanel=Rect("Mechanism meters",mechanicPanel,17,110,650,26);
                mechanicMeterA=Bar(mechanicMeterPanel,0,19,312,4,Gold);mechanicMeterB=Bar(mechanicMeterPanel,338,19,312,4,Mint);
                mechanicMeterLabel=Text(mechanicMeterPanel,0,-1,650,21,"",12,Muted);
                buildStatus=Text(hud,400,706,800,32,"",17,Mint,TextAlignmentOptions.Center);
            }
            reloadPanel.gameObject.SetActive(game.Player.Reloading);reloadFill.fillAmount=game.Player.ReloadProgress;
            reloadLabel.text=!game.Player.QuickReloadAvailable?"速装已尝试 · 等待上膛":game.Player.ReloadProgress>=.55f&&game.Player.ReloadProgress<=.72f?"现在按 R · 精准装填":"装填中 · 绿色区间再次按 R";
            buildStatus.text=game.Player.Synergy?game.Player.Synergy.Status:"";
            Enemy boss=null;foreach(var e in game.Enemies)if(e&&e.IsBoss){boss=e;break;}
            mechanicPanel.gameObject.SetActive(boss&&game.State==VoyageState.Combat&&!game.Paused);
            if(boss&&boss.Encounter){
                var brain=boss.Encounter;var m=brain.Mechanism;
                if(m){
                    mechanicTitle.text=(m.Active?"行动 / ":"破绽 / ")+MechanismTitles[m.BossIndex];
                    mechanicDetail.text=m.Active?"第 "+m.Phase+" 阶段 · "+Mathf.RoundToInt(m.Progress*100)+"%":"核心可攻击";
                    mechanicLabel.text=m.Active?m.Cue:brain.Windup>0?brain.Cue:"已完成反制 · 瞄准核心输出，留意地面反扑预警";
                    mechanicProgress.fillAmount=m.Active?m.Progress:1;
                    mechanicProgress.color=m.Active?Gold:Mint;
                    UpdateMechanismMeters(m);
                }
            }
            RaycastHit hitInfo;if(game.IsPlaying&&Physics.Raycast(game.Player.View.transform.position,game.Player.View.transform.forward,out hitInfo,100,~((1<<9)|(1<<30)))){
                var target=hitInfo.collider.GetComponentInParent<EncounterTarget>();if(target)targetName.text="可破坏物 · "+target.Label+"\n"+Mathf.CeilToInt(target.Health)+" / "+Mathf.CeilToInt(target.Maximum)+"  ·  射击摧毁";
            }
        }
        void UpdateMechanismMeters(BossMechanism m)
        {
            mechanicMeterPanel.gameObject.SetActive(m.Active);if(!m.Active)return;
            float a=m.Progress,b=m.CycleProgress;string label="行动进度                                              当前动作";
            switch(m.BossIndex){
                case 0:a=m.CycleProgress;b=m.TargetLocked?1:0;label=m.TargetLocked?"◆ 已锁定 · 立即侧闪离开橙线":"◇ 正在追踪 · 站到亮桩附近引导";break;
                case 1:a=(float)m.CompletedActions/Mathf.Max(1,m.RequiredActions);b=m.Listening?0:1;label=m.Listening?"♪ 正在播放 · 数字、形状与音高同时提示":"♪ 乐谱留在上方 · 走近音贝按 E 依序回应";break;
                case 2:a=m.Progress;b=m.Carrying?1:0;label="净化进度                                                清水："+(m.Carrying?"已携带":"待领取");break;
                case 3:a=m.Progress;b=m.Progress;label="连续光路 · 金线表示接通，沿光束找到下一面镜";break;
                case 4:a=m.CycleProgress;b=m.TimingWindowOpen?1:0;label=m.TimingWindowOpen?"◆ 现在按 E · 钟摆处于绿色有效时窗":"◇ 等待钟摆 · 进度 63%–84% 是有效时窗";break;
                case 5:a=m.Heat/100;b=m.Progress;label="热量 "+Mathf.CeilToInt(m.Heat)+" / 100 · 低于 20 停航                 护送进度";break;
                case 6:a=m.CycleProgress;b=m.Progress;label=m.Carrying?"电荷剩余 "+m.ActionSeconds.ToString("F1")+" 秒 · 奔向闪亮接地圈":"前往蓄电台领取电荷 · 成功接地轮次保留";break;
                case 7:a=m.Heat/100;b=m.Pressure/100;label="温度 "+Mathf.CeilToInt(m.Heat)+"  [目标 25–45]                      压力 "+Mathf.CeilToInt(m.Pressure)+"  [低于 65]";break;
                case 8:a=m.StateStep==1?(float)m.RecordedRoute.Count/Mathf.Max(1,m.RequiredActions):m.Progress;b=m.StateStep/2f;label=m.StateStep==1?"记录路线 · 每步间隔至少三米":"逆行回收 · 取回足迹后立即移动，避开追忆落点";break;
                case 9:a=m.Carrying?m.CycleProgress:0;b=m.Carrying?m.Tension:0;label=m.Carrying?"收线 "+Mathf.RoundToInt(m.CycleProgress*100)+"%                                        张力 "+Mathf.RoundToInt(m.Tension*100)+"%":"收线 —                                                 张力 — · 等待挂钩";break;
                case 10:a=m.StateStep==1?Mathf.Clamp01(1-m.ActionSeconds/12):m.StateStep==2?1:0;b=m.Progress;label=m.StateStep==2?"已引出破冰 · 蓝线变橙后横移，成功避开才保存观测":m.StateStep==1?"回波剩余 "+Mathf.Max(0,12-m.ActionSeconds).ToString("F1")+" 秒 · 识别双环":"观测 "+m.CompletedActions+" / "+m.RequiredActions+" · 免费声呐不消耗随身道具";break;
            }
            mechanicMeterLabel.text=label;mechanicMeterA.fillAmount=Mathf.Clamp01(a);mechanicMeterB.fillAmount=Mathf.Clamp01(b);
            mechanicMeterA.color=m.BossIndex==5&&m.Heat<=20?new Color(1,.38f,.24f):Gold;
            mechanicMeterB.color=(m.BossIndex==9&&m.Tension>.7f)||(m.BossIndex==7&&m.Pressure>=65)?new Color(1,.38f,.24f):Mint;
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

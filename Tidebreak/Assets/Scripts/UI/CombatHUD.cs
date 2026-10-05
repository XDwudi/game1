using TMPro;
using UnityEngine;
namespace Tidebreak
{
    public partial class SeaHUD
    {
        string BossThreatLegend(Enemy boss)
        {
            switch(boss.Spec.id){
                case 108:return "浪沫低环：跳跃   /   重钳落点：离开橙圈";
                case 109:return "珊瑚音浪：连续跳跃   /   音刃：穿过弹道间隙";
                case 110:return "毒根浊池：离开边界   /   已净化绿岸：安全";
                case 111:return "盐晶光带：横移   /   晶簇落点：离开橙圈";
                case 112:return "幽焰锁链：横移   /   吞岸旋流：向外冲刺";
                case 113:return "冰脊猎线：横移   /   冰雨落点：离开橙圈";
                case 114:return "雷柱落点：离开橙圈   /   导电光带：横移";
                case 115:return "熔火喷口：离开橙圈   /   低矮熔潮：跳跃";
                case 116:return "镜刃宽带：横移   /   回忆落点：取回后离开";
                case 117:return "触腕拍岸：离开橙圈   /   墨浪低环：跳跃";
                default:return "蓝线追踪 → 橙线锁定后横移   /   鲸歌低环：跳跃";
            }
        }
        RectTransform reloadPanel,mechanicPanel,mechanicMeterPanel;
        TextMeshProUGUI reloadLabel,mechanicLabel,buildStatus,mechanicTitle,mechanicMeterLabel,mechanicDetail;
        UnityEngine.UI.Image reloadWindow,reloadFill,mechanicProgress,mechanicMeterA,mechanicMeterB;
        static readonly string[] MechanismTitles={"引钳撞桩","音贝应答","净水造岸","折光破甲","拉钟截潮","护火破冰","导雷接地","双阀淬甲","逆行归忆","钓竿牵腕","声呐追鲸"};
        static readonly string[] MechanismStrategies={"借重钳撞断系船桩。\n蓝线追踪，橙线锁定后侧闪。","先听完整乐谱，再依序回应。\n数字与贝壳亮灯给出同样信息。","把清水带到污染环。\n净化的绿圈也是安全地面。","让三面镜连续接通光路。\n沿光束寻找下一面反光镜。","用钟声打断浪潮蓄势。\n提前拉绳无效，成功拍次保留。","护送热炉，补温融开封锁。\n换枪击碎冰锁；风雪回炉边。","先接电，再反相，最后接地。\n指定中继与接地台都有导线。","调节双阀，淬出脆甲。\n保持温压平衡，再射击破甲。","记录路线后逆行回收。\n双晶为真，单晶为镜面诱饵。","借平潮牵腕，红潮松线换侧。\n腕结上岸后换枪击碎。","辨认双环，交叉声呐确认。\n诱出破冰，锁定后横移。"};
        string MechanismInput(BossMechanism m)
        {
            if(!string.IsNullOrEmpty(m.LateInput))return m.LateInput;
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
                reloadPanel=Rect("Tactical reload",hud,640,555,320,54);Plate(reloadPanel,0,0,320,54,Panel);reloadLabel=Text(reloadPanel,8,3,304,28,"",15,Cream,TextAlignmentOptions.Center);
                reloadFill=Bar(reloadPanel,18,37,284,6,Gold);reloadWindow=Box(reloadPanel,18+284*.55f,35,284*.17f,10,new Color(.3f,1,.7f,.45f));
                mechanicPanel=Rect("Encounter action instrument",hud,1176,118,396,196);
                Plate(mechanicPanel,0,0,396,196,new Color(Ink.r,Ink.g,Ink.b,.94f));
                Stitch(mechanicPanel,17,43,360,new Color(Brass.r,Brass.g,Brass.b,.48f));
                mechanicTitle=Text(mechanicPanel,17,13,255,25,"",18,Gold);mechanicDetail=Text(mechanicPanel,280,14,99,25,"",14,Mint,TextAlignmentOptions.Right);
                mechanicLabel=Text(mechanicPanel,17,47,362,70,"",17,Cream);mechanicLabel.overflowMode=TextOverflowModes.Ellipsis;
                mechanicProgress=Bar(mechanicPanel,17,123,362,4,Mint);
                mechanicMeterPanel=Rect("Mechanism meters",mechanicPanel,17,140,362,43);
                mechanicMeterA=Bar(mechanicMeterPanel,0,35,172,4,Gold);mechanicMeterB=Bar(mechanicMeterPanel,190,35,172,4,Mint);
                mechanicMeterLabel=Text(mechanicMeterPanel,0,-1,362,33,"",12,Muted);
                buildStatus=Text(hud,585,742,430,25,"",14,Mint,TextAlignmentOptions.Center);
            }
            reloadPanel.gameObject.SetActive(game.Player.Reloading);reloadFill.fillAmount=game.Player.ReloadProgress;
            reloadWindow.rectTransform.anchoredPosition=new Vector2(18+284*game.Player.ReloadWindowStart,-35);reloadWindow.rectTransform.sizeDelta=new Vector2(284*(game.Player.ReloadWindowEnd-game.Player.ReloadWindowStart),10);
            reloadLabel.text=!game.Player.QuickReloadAvailable?"速装已尝试 · 等待上膛":game.Player.ReloadProgress>=game.Player.ReloadWindowStart&&game.Player.ReloadProgress<=game.Player.ReloadWindowEnd?"现在按 R · 精准装填":"装填中 · 绿色区间再次按 R";
            buildStatus.text=game.Player.Synergy?game.Player.Synergy.Status:"";
            Enemy boss=null;foreach(var e in game.Enemies)if(e&&e.IsBoss){boss=e;break;}
            Enemy elite=null;if(!boss)foreach(var e in game.Enemies)if(e&&e.elite&&e.Elite&&!e.dead&&(!elite||e.Elite.Windup>elite.Elite.Windup||e.Elite.Windup==elite.Elite.Windup&&(e.transform.position-game.Player.transform.position).sqrMagnitude<(elite.transform.position-game.Player.transform.position).sqrMagnitude))elite=e;
            mechanicPanel.gameObject.SetActive((boss||elite)&&game.State==VoyageState.Combat&&!game.Paused);
            if(elite){
                mechanicTitle.text="精英 / "+EliteTactics.Names[(int)elite.Spec.body];mechanicDetail.text=elite.Elite.Windup>0?"正在蓄势":"警戒";
                mechanicLabel.text=elite.Regional&&elite.Regional.Active?elite.Regional.Cue:elite.Elite.Windup>0?elite.Elite.Cue:EliteTactics.Counters[(int)elite.Spec.body];
                mechanicProgress.fillAmount=elite.Elite.Windup>0?elite.Elite.Windup:elite.health/elite.maxHealth;
                mechanicProgress.color=elite.Elite.Windup>0?Gold:Mint;mechanicMeterPanel.gameObject.SetActive(false);
            }
            if(boss&&boss.Encounter){
                var brain=boss.Encounter;var m=brain.Mechanism;
                if(m){
                    mechanicTitle.text=(m.Active?"行动 / ":"破绽 / ")+MechanismTitles[m.BossIndex];
                    mechanicDetail.text=m.Active?Mathf.RoundToInt(m.Progress*100)+"%":"可攻击";
                    mechanicLabel.text=m.Active?CompactMechanicCue(m):brain.Windup>0?brain.Cue:"核心已暴露。\n瞄准发光弱点，留意地面反扑。";
                    mechanicProgress.fillAmount=m.Active?m.Progress:1;
                    mechanicProgress.color=m.Active?Gold:Mint;
                    UpdateMechanismMeters(m);
                }
            }
            RaycastHit hitInfo;if(game.IsPlaying&&Physics.Raycast(game.Player.View.transform.position,game.Player.View.transform.forward,out hitInfo,100,~((1<<9)|(1<<30)))){
                var target=hitInfo.collider.GetComponentInParent<EncounterTarget>();if(target)targetName.text="可破坏物 · "+target.Label+"\n"+Mathf.CeilToInt(target.Health)+" / "+Mathf.CeilToInt(target.Maximum)+"  ·  射击摧毁";
            }
        }
        string CompactMechanicCue(BossMechanism m)
        {
            if(!string.IsNullOrEmpty(m.LateBrief))return m.LateBrief;
            switch(m.BossIndex){
                case 0:return m.TargetLocked?"重钳已锁定！\n立即横移，借撞击破桩。":"前往亮起的系船桩。\n蓝线追踪，变橙后侧闪。";
                case 1:return m.Listening?m.Cue:"按乐谱次序回应音贝。\n"+m.Cue;
                case 2:return m.Carrying?"把清水带到污染环。\n按住 E 净化，绿圈可避毒。":"从蓝色水泵取清水。\n已净化的岸线不会失去。";
                case 3:return "按 E 转动镜面，连接三面镜。\n金色光路表示方向正确。";
                case 4:return m.TimingWindowOpen?"钟摆变绿 · 现在按 E！\n成功拍次会保留。":"等钟摆变绿再按 E。\n提前拉绳不能打断浪潮。";
                case 5:return m.Heat<20?"热炉即将熄灭！\n靠近并按住 E 补热。":"在热炉旁伴行。\n按住 E 停车补热，落冰时暂离。";
                case 6:return m.Carrying?"电荷不稳定 · 前往指定接地圈！\n剩余 "+m.ActionSeconds.ToString("F1")+" 秒。":"先观察接地圈，再领取电荷。\n按 E 领取后立即动身。";
                case 7:return "冷却阀降温，泄压阀减压。\n温度 25–45、压力低于 65。";
                case 8:return m.StateStep==0?"按 E 记录自己的路线。\n每步至少相隔三米。":m.StateStep==1?"走出 "+m.RequiredActions+" 个分散足迹。\n返回时需要逆序经过。":"沿原路线逆行回收足迹。\n取回后立即离开爆炸落点。";
                case 9:return !m.Carrying?"拿起鱼竿，瞄准触腕按 E。\n金圈是安全的牵引位置。":m.Surge?m.Phase>=2?"红潮来了 · 松开 E！\n横移到新的金色牵引圈。":"红潮来了 · 松开 E 降张力。":m.Braced?"平潮窗口 · 按住 E 收线。\n红潮时放线，注意脚下。":"移入金色牵引圈后才能收线。";
                default:return m.StateStep==0?"在声呐站按 E 发射回波。\n辨认双环，再到观测点定位。":m.StateStep==1?"双环是真回波，单环是假象。\n前往相应观测点按 E。":m.Phase>=3?"破冰有两段！\n每段蓝线变橙后分别横移。":"蓝线追踪，橙线锁定。\n横移避开破冰后才记录观测。";
            }
        }
        void UpdateMechanismMeters(BossMechanism m)
        {
            mechanicMeterPanel.gameObject.SetActive(m.Active);if(!m.Active)return;
            float a=m.Progress,b=m.CycleProgress;string label="行动进度  ·  当前动作";
            switch(m.BossIndex){
                case 0:a=m.CycleProgress;b=m.TargetLocked?1:0;label=m.TargetLocked?"◆ 已锁定 · 立即侧闪离开橙线":"◇ 正在追踪 · 站到亮桩附近引导";break;
                case 1:a=(float)m.CompletedActions/Mathf.Max(1,m.RequiredActions);b=m.Listening?0:1;label=m.Listening?"♪ 正在播放 · 数字、形状与音高同时提示":"♪ 乐谱保留在卡片内 · 按 E 依序回应";break;
                case 2:a=m.Progress;b=m.Carrying?1:0;label="净化进度  ·    清水："+(m.Carrying?"已携带":"待领取");break;
                case 3:a=m.Progress;b=m.Progress;label="连续光路 · 金线表示接通，沿光束找到下一面镜";break;
                case 4:a=m.CycleProgress;b=m.TimingWindowOpen?1:0;label=m.TimingWindowOpen?"◆ 现在按 E · 钟摆处于绿色有效时窗":"◇ 等待钟摆 · 进度 63%–84% 是有效时窗";break;
                case 5:a=m.Heat/100;b=m.Progress;label="热量 "+Mathf.CeilToInt(m.Heat)+" / 100 · 低于 20 停航  ·  护送进度";break;
                case 6:a=m.CycleProgress;b=m.Progress;label=m.Carrying?"电荷剩余 "+m.ActionSeconds.ToString("F1")+" 秒 · 奔向闪亮接地圈":"前往蓄电台领取电荷 · 成功接地轮次保留";break;
                case 7:a=m.Heat/100;b=m.Pressure/100;label="温度 "+Mathf.CeilToInt(m.Heat)+"  [目标 25–45]  ·       压力 "+Mathf.CeilToInt(m.Pressure)+"  [低于 65]";break;
                case 8:a=m.StateStep==1?(float)m.RecordedRoute.Count/Mathf.Max(1,m.RequiredActions):m.Progress;b=m.StateStep/2f;label=m.StateStep==1?"记录路线 · 每步间隔至少三米":"逆行回收 · 取回足迹后立即移动，避开追忆落点";break;
                case 9:a=m.Carrying?m.CycleProgress:0;b=m.Carrying?m.Tension:0;label=m.Carrying?"收线 "+Mathf.RoundToInt(m.CycleProgress*100)+"% · 张力 "+Mathf.RoundToInt(m.Tension*100)+"%":"等待挂钩 · 瞄准触腕按 E";break;
                case 10:a=m.StateStep==1?Mathf.Clamp01(1-m.ActionSeconds/12):m.StateStep==2?1:0;b=m.Progress;label=m.StateStep==2?"已引出破冰 · 蓝线变橙后横移，成功避开才保存观测":m.StateStep==1?"回波剩余 "+Mathf.Max(0,12-m.ActionSeconds).ToString("F1")+" 秒 · 识别双环":"观测 "+m.CompletedActions+" / "+m.RequiredActions+" · 免费声呐不消耗随身道具";break;
            }
            mechanicMeterLabel.text=string.IsNullOrEmpty(m.LateMeter)?label:m.LateMeter;mechanicMeterA.fillAmount=Mathf.Clamp01(a);mechanicMeterB.fillAmount=Mathf.Clamp01(b);
            mechanicMeterA.color=m.BossIndex==5&&m.Heat<=20?new Color(1,.38f,.24f):Gold;
            mechanicMeterB.color=(m.BossIndex==9&&m.Tension>.7f)||(m.BossIndex==7&&m.Pressure>=65)?new Color(1,.38f,.24f):Mint;
        }
        void BuildKeystoneStock(RectTransform p)
        {
            var r=game.Run;int act=Mathf.Clamp((r.maxIsland-2)/3,0,2);
            for(int a=0;a<3;a++){int selected=a;Button(p,80+a*300,324,280,37,"第 "+(a+1)+" 幕 · 岛屿 "+VoyageBuilds.Island(a)+" 解锁",()=>{keystoneAct=selected;BuildShopPage();},keystoneAct==a);}
            act=keystoneAct;
            for(int i=0;i<3;i++){int index=act*3+i;float x=80+i*486;bool own=r.HasKeystone(index),can=VoyageBuilds.CanBuy(r,index);
                Plate(p,x,382,464,326,Paper,true,"Keystone field sheet "+index);
                Icon(p,x+367,393,69,i==0?NauticalMark.Harpoon:i==1?NauticalMark.Coat:NauticalMark.Sonar,new Color(PaperInk.r,PaperInk.g,PaperInk.b,.67f));
                Text(p,x+22,399,343,43,VoyageBuilds.Names[index],28,PaperInk).fontStyle=FontStyles.Bold;
                Stitch(p,x+22,456,420,new Color(PaperInk.r,PaperInk.g,PaperInk.b,.25f));
                Text(p,x+22,475,420,116,VoyageBuilds.Descriptions[index],20,PaperInk);
                string locked=r.HasActKeystone(act)?"本幕已选择另一专精":r.maxIsland<VoyageBuilds.Island(act)?"抵达第 "+VoyageBuilds.Island(act)+" 岛解锁":"";
                var requirement=Text(p,x+22,603,420,28,own?"已装配 · 当前流派核心":locked!=""?locked:"本幕三选一 · 请按自己的打法选择",15,PaperMuted);requirement.gameObject.name="Shop requirement keystone"+index;
                PurchaseStrip(p,x+22,642,420,46,"keystone"+index,own?-1:VoyageBuilds.Price(act),own?"已装配":locked!=""?"待解锁":can?"购买专精":"金币不足",()=>game.BuyKeystone(index),can);
            }
            Text(p,80,723,1430,61,"每幕三选一，三个专精可以跨幕组合。先想好打法，再用金币购买。\n普通配件补足属性；专精决定你如何输出、躲避和生存。",19,Muted);
        }
        int keystoneAct;
        public void ShowBuilds(){shopTab=4;BuildShopPage();}
    }
}

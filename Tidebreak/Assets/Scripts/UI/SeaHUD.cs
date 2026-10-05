using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Tidebreak
{
    public partial class SeaHUD : MonoBehaviour
    {
        static readonly Color Ink=new Color(.025f,.065f,.085f), Panel=new Color(.055f,.115f,.135f,.97f), Cream=new Color(.96f,.93f,.82f), Muted=new Color(.58f,.73f,.72f), Mint=new Color(.39f,.87f,.77f), Gold=new Color(.98f,.72f,.39f);
        GameDirector game;
        RectTransform root,hud,modal,fishPanel,bossPanel,crosshair,dangerPanel,noticePanel,compassPanel,targetPanel;
        TMP_FontAsset font;
        Sprite whiteSprite;
        TextMeshProUGUI inventory,compass,seaTitle,objective,money,healthText,weaponText,ammoText,notice,hint,bossName,bossHealth,bossInstruction,fishingTitle,fishingHint,damageNumber,status,targetName,dangerText;
        UnityEngine.UI.Image healthFill,tensionFill,progressFill,bossFill,dashFill,damageOverlay,hit,voyageInfoBackground,targetBackdrop;
        float hitUntil,damageUntil;
        bool settings;
        public void Init(GameDirector director)
        {
            game=director;font=Resources.Load<TMP_FontAsset>("Fonts/SeaFont");
            whiteSprite=Sprite.Create(Texture2D.whiteTexture,new UnityEngine.Rect(0,0,Texture2D.whiteTexture.width,Texture2D.whiteTexture.height),Vector2.one*.5f);
            var c=new GameObject("Tidebreak UI",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));
            c.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;c.GetComponent<Canvas>().sortingOrder=20;
            var scaler=c.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=.5f;
            scaler.screenMatchMode=UnityEngine.UI.CanvasScaler.ScreenMatchMode.Expand;
            root=Rect("Safe canvas",c.transform,0,0,1600,900);root.anchorMin=root.anchorMax=new Vector2(.5f,.5f);root.pivot=new Vector2(.5f,.5f);root.anchoredPosition=Vector2.zero;
            if(!FindObjectOfType<EventSystem>())new GameObject("UI input",typeof(EventSystem),typeof(StandaloneInputModule));
            hud=Full("Voyage HUD",root);
            voyageInfoBackground=Plate(hud,26,23,348,148,new Color(Ink.r,Ink.g,Ink.b,.86f));
            Stitch(hud,44,132,307,new Color(Brass.r,Brass.g,Brass.b,.42f));
            seaTitle=Text(hud,44,31,317,31,"",23,Cream);
            objective=Text(hud,44,69,312,65,"",15,Mint);
            objective.overflowMode=TextOverflowModes.Ellipsis;
            inventory=Text(hud,44,140,314,27,"",14,Cream);
            compassPanel=Rect("Bearing panel",hud,525,16,550,35);Plate(compassPanel,0,0,550,35,new Color(Ink.r,Ink.g,Ink.b,.76f));
            Icon(compassPanel,7,5,25,NauticalMark.Compass,Gold);Icon(compassPanel,518,5,25,NauticalMark.Compass,Gold);
            compass=Text(compassPanel,0,2,550,30,"",14,Cream,TextAlignmentOptions.Center);
            Plate(hud,1350,25,222,62,new Color(Ink.r,Ink.g,Ink.b,.90f));
            Icon(hud,1361,37,35,NauticalMark.Coin,Gold);
            money=Text(hud,1399,38,153,34,"",23,Gold,TextAlignmentOptions.Right);
            noticePanel=Rect("Voyage notification",hud,440,118,720,58);
            Plate(noticePanel,0,0,720,58,new Color(Ink.r,Ink.g,Ink.b,.94f));
            notice=Text(noticePanel,16,6,688,46,"",17,Cream,TextAlignmentOptions.Center);
            notice.fontStyle=FontStyles.Bold;
            Plate(hud,26,749,295,120,new Color(Ink.r,Ink.g,Ink.b,.90f));
            Icon(hud,41,757,29,NauticalMark.Coat,Gold);
            Text(hud,80,761,105,25,"船长生命",15,Muted);healthText=Text(hud,171,757,129,30,"",22,Cream,TextAlignmentOptions.Right);
            healthFill=Bar(hud,44,797,256,7,Mint);
            Text(hud,44,821,185,28,"SHIFT  /  冲刺",14,Muted);dashFill=Bar(hud,201,831,99,4,Gold);
            Plate(hud,1240,749,331,120,new Color(Ink.r,Ink.g,Ink.b,.90f));
            weaponText=Text(hud,1258,762,190,28,"",18,Cream);
            ammoText=Text(hud,1437,755,112,44,"",29,Gold,TextAlignmentOptions.Right);
            Text(hud,1258,801,291,24,"1 鱼竿 · 2–7 武器 · B 拟饵",13,Muted,TextAlignmentOptions.Right);
            Text(hud,1258,831,291,24,"R 装填   ·   右键瞄准",14,Muted,TextAlignmentOptions.Right);
            Plate(hud,464,778,672,43,new Color(.035f,.09f,.13f,.90f));
            hint=Text(hud,478,786,644,29,"",16,Cream,TextAlignmentOptions.Center);
            explorationKeys=Text(hud,370,848,860,24,"WASD 移动  ·  SPACE 跳跃  ·  E 交互  ·  F 收纳  ·  TAB 鱼篓",12,Muted,TextAlignmentOptions.Center);
            status=Text(hud,470,867,660,21,"",11,Muted,TextAlignmentOptions.Center);
            crosshair=Rect("Crosshair",hud,780,430,40,40);
            Box(crosshair,17,1,3,10,Cream);Box(crosshair,17,27,3,10,Cream);Box(crosshair,1,17,10,3,Cream);Box(crosshair,27,17,10,3,Cream);Box(crosshair,17,17,3,3,Mint);
            hit=Box(hud,793,443,14,14,Color.clear);hit.rectTransform.localRotation=Quaternion.Euler(0,0,45);
            damageNumber=Text(hud,816,418,130,40,"",23,Gold);
            targetPanel=Rect("Target identity",hud,495,487,610,58);targetBackdrop=Plate(targetPanel,0,0,610,58,new Color(Ink.r,Ink.g,Ink.b,.86f));
            targetName=Text(targetPanel,10,4,590,50,"",17,Cream,TextAlignmentOptions.Center);
            dangerPanel=Rect("Incoming strike warning",hud,530,667,540,43);
            dangerBack=Plate(dangerPanel,0,0,540,43,new Color(.53f,.1f,.11f,.9f));dangerText=Text(dangerPanel,12,7,516,31,"",17,Cream,TextAlignmentOptions.Center);
            fishPanel=Rect("Fishing instrument",hud,495,610,610,126);Plate(fishPanel,0,0,610,126,Panel);
            Icon(fishPanel,17,12,29,NauticalMark.Reel,Gold);
            fishingTitle=Text(fishPanel,56,11,530,33,"",19,Cream);
            Text(fishPanel,22,52,85,20,"收线进度",13,Muted);progressFill=Bar(fishPanel,113,58,472,7,Mint);
            Text(fishPanel,22,81,85,20,"鱼线张力",13,Muted);tensionFill=Bar(fishPanel,113,87,472,7,Gold);
            fishingHint=Text(fishPanel,22,103,565,21,"",11,Muted);
            bossPanel=Rect("Boss life",hud,460,25,680,75);
            Plate(bossPanel,0,0,680,75,new Color(Ink.r,Ink.g,Ink.b,.92f));
            bossName=Text(bossPanel,18,10,490,30,"",21,Cream);bossHealth=Text(bossPanel,515,12,147,26,"",17,Gold,TextAlignmentOptions.Right);
            bossFill=Bar(bossPanel,18,48,644,6,new Color(.94f,.42f,.35f));
            bossInstruction=Text(bossPanel,18,59,644,14,"",11,Muted,TextAlignmentOptions.Center);
            damageOverlay=Box(root,0,0,1600,900,Color.clear);damageOverlay.raycastTarget=false;InitImpactPresentation();
        }
        public void ShowState()
        {
            settings=false;ClearModal();hud.gameObject.SetActive(game.IsPlaying);if(game.State!=VoyageState.Combat){hitUntil=damageUntil=0;hit.color=Color.clear;damageNumber.text="";}
            switch(game.State){case VoyageState.Harbor:ShowHarbor();break;case VoyageState.Shop:ShowShop();break;case VoyageState.Route:ShowRoute();break;case VoyageState.Victory:ShowResult(true);break;case VoyageState.Defeat:ShowResult(false);break;}
        }
        void ClearModal(){ClearSpecimen();if(modal){modal.gameObject.SetActive(false);Destroy(modal.gameObject);}modal=null;}
        RectTransform NewModal(bool shade=true)
        {ClearModal();hud.gameObject.SetActive(false);modal=Full("Menu",root);if(shade){Box(modal,-1600,-1000,4800,2900,new Color(.015f,.06f,.085f,.985f));DecorateMenu(modal);}return modal;}
        public void ShowHarbor()
        {
            settings=false;hud.gameObject.SetActive(false);var p=NewModal(false);
            Plate(p,-10,-10,645,920,new Color(.025f,.09f,.13f,.98f));Box(p,631,0,2,900,new Color(Brass.r,Brass.g,Brass.b,.65f));
            Icon(p,345,49,250,NauticalMark.Compass,new Color(Brass.r,Brass.g,Brass.b,.10f));Stitch(p,60,546,505,new Color(Brass.r,Brass.g,Brass.b,.44f));
            Text(p,58,47,510,28,"SINGLE PLAYER  /  FISHING ROGUELIKE",14,Mint);
            Text(p,53,137,550,127,"TIDE",112,Cream).characterSpacing=8;
            Text(p,53,246,550,127,"BREAK",106,Cream).characterSpacing=4;
            Box(p,61,395,58,4,Gold);
            Text(p,140,381,409,42,"潮 汐 猎 手",29,Gold);
            Text(p,60,455,514,72,"深海从不白送宝藏。\n抛竿、开火，把传说带回港口。",21,Muted);
            Button(p,60,571,505,61,"开始远征    →",()=>game.StartVoyage(),true);
            var save=SaveStore.Read<RunData>("voyage");
            if(save!=null)Button(p,60,646,245,50,"继续 · 钓点 "+save.stage,()=>game.StartVoyage(true));
            else Button(p,60,646,245,50,"船长手册",ShowManual);
            Button(p,320,646,245,50,"海洋图鉴",ShowJournal);
            Button(p,60,710,245,50,"设置",()=>ShowSettings(false));Button(p,320,710,245,50,"退出游戏",game.Quit);
            Text(p,60,807,510,44,"九岛异境 · 岛屿专研 · 十一种巨物反制\nWIN 64   /   v0.8.0",13,Muted);
            Text(p,1075,741,452,38,"PINEHAVEN",27,Cream,TextAlignmentOptions.Right).characterSpacing=4;
            Text(p,1075,786,452,43,"松风港  /  钓猎远征\n完成 "+game.Log.victories+" 次远征  ·  克拉肯 "+game.Log.krakens+" 次",15,Cream,TextAlignmentOptions.Right);
        }
        void Header(RectTransform p,string eyebrow,string title,string sub)
        {Icon(p,80,49,28,NauticalMark.Anchor,Gold);Text(p,121,53,1399,25,eyebrow,14,Gold).characterSpacing=.9f;Text(p,76,99,1448,69,title,48,Cream);Text(p,80,185,1420,53,sub,20,Muted);Stitch(p,80,250,1440,new Color(Brass.r,Brass.g,Brass.b,.46f));}
        public void ShowShop(){BuildShopPage();}
        void ShowRoute(){BuildIslandMap();}
        void ShowResult(bool victory)
        {
            var p=NewModal();Header(p,victory?"VOYAGE COMPLETE   /   远征记录":"THE SEA REMEMBERS   /   远征记录",victory?(game.Run.krakenDefeated?"古神陨落，传说归航":game.Run.whaleDefeated?"幽海传说，收入囊中":"破浪归来，船长"):"海潮会记住这次冒险",victory?"你的战利品已写入船长日志。下一趟海域，会有新的选择。":"失去的是这趟装备。留下的是经验、图鉴，和下一次机会。");
            string[] labels={"到达钓点","猎获怪物","累计金币","远征用时"};
            string[] values={game.Run.stage.ToString(),game.Run.kills.ToString(),game.Run.earned.ToString(),TimeSpan.FromSeconds(game.Run.elapsed).ToString(@"mm\:ss")};
            for(int i=0;i<4;i++){float x=80+i*366;Box(p,x,302,342,154,Panel);Text(p,x+24,324,296,32,labels[i],17,Muted);Text(p,x+22,368,296,66,values[i],47,Cream);}
            bool kraken=victory&&game.StoryComplete&&(game.Run.abyssBait||game.Run.rareSignal);
            if(kraken){Text(p,80,521,1440,71,"深渊传来回应。稀有 BOSS「克拉肯」可被唤醒。\n挑战前恢复 50 点生命；战败仍会保留本次主线通关纪录。",23,Gold);Button(p,80,642,680,66,"继续：挑战克拉肯    →",game.ChallengeKraken,true);}
            else Text(p,80,521,1440,80,victory?"稀有传闻：购买「禁忌鱼饵」，或解开岛上的隐藏宝箱。\n继续探索后，可随时在航图中进入传说海域。":"船长建议：锁定后再闪避，攻击正面青绿核心，\n不要在鱼线发红时一直按住收线。",23,Muted);
            if(victory&&game.StoryComplete&&game.Run.rareSignal)Button(p,800,642,720,66,"罕见遭遇：幽海白鲸    →",game.ChallengeWhiteWhale);
            if(victory)Button(p,1020,758,500,61,"继续探索已解锁的群岛",game.ExploreAfterEnding);
            Button(p,80,758,450,61,"返回港口",game.ReturnHarbor,true);Button(p,550,758,450,61,"再次远征",()=>game.StartVoyage());
        }
        public void ShowPause()
        {
            var p=NewModal();Header(p,"ANCHOR DOWN   /   暂停","风浪可以等一等","远征已暂停。已收纳鱼获、金币、购买记录和委托进度会自动保存。");
            Button(p,80,319,568,65,"继续游戏",game.TogglePause,true);Button(p,80,405,568,59,"设置",()=>ShowSettings(true));
            Button(p,80,485,568,59,"船长手册",ShowManual);Button(p,80,565,568,59,"保存进度并返回港口",game.ReturnHarbor);
            Text(p,800,331,680,209,"W A S D   岛上移动  /  SPACE 跳跃\n鼠标左键   抛竿 / 收线 / 射击\n鼠标右键   精确瞄准\nSHIFT   短距离冲刺，短暂无敌\n1 鱼竿  2 左轮  3 霰弹  4 鱼叉\nE 交互  /  F 收纳  /  Q 抛出  /  ESC 暂停",23,Muted);
        }
        void ShowManual()
        {
            var p=NewModal();Header(p,"CAPTAIN'S FIELD GUIDE","船长手册","从松风港出发，独立调查与机关 → 破招首领战 → 金币选择专精 → 新航路。");
            string[] titles={"01  抛竿与控线","02  射击与生存","03  成长与巨物"};
            string[] desc={"WASD 行走，SPACE 跳跃。\n按 1 拿起鱼竿，面向开阔海面。\n按住左键蓄力，松开抛出浮漂。\n\n咬钩后，绿灯按住左键收线；\n红灯松手降低张力。\n收线完成，怪鱼会跃出海面。", "按 2 拔枪，左键射击，R 装填。\n右键瞄准，SHIFT 冲刺闪避。\n凌空击杀与弱点击杀提高售价。\n\n击倒鱼后按 E 拿起；\nF 收进鱼篓，Q 可以把鱼抛出。\n鱼篓可在工坊扩容。", "港内鱼获收购柜台按 E 出售鱼获。\n老船长工坊用金币购买所有升级。\n随机配件也需要金币。\n\n修复、护送、解谜、追寻失踪船员。\n各岛完成调查后，在码头摇钟。\n首领潮核必须交给向导解锁航线。"};
            for(int i=0;i<3;i++){float x=80+i*489;Box(p,x,295,463,401,Panel);Text(p,x+26,321,415,45,titles[i],28,Gold);Text(p,x+26,389,413,281,desc[i],21,Muted);}
            Button(p,80,772,400,58,"返回",()=>{if(game.Paused)ShowPause();else ShowHarbor();},true);
        }
        public void ShowJournal(){BuildCodex();}
        public void ShowBag(){BuildBagPage();}
        void ShowSettings(bool paused,bool display=false)
        {
            if(settings&&game.Display&&!game.Display.TryLeavePreview())return;
            settings=true;var p=NewModal();Header(p,"SHIP'S INSTRUMENTS   /   设置","按你的节奏航行",display?"三种显示方式，自由切换。试用后保留更改；15 秒内未确认会自动恢复。":"声音与镜头设置即时生效。所有机制同时提供文字和视觉提示。");
            Button(p,80,273,330,44,"声音与操作",()=>ShowSettings(paused),!display);
            Button(p,427,273,330,44,"画面显示",()=>ShowSettings(paused,true),display);
            if(display){BuildDisplaySettings(p,paused);return;}
            SettingSlider(p,80,343,"总音量",game.Log.volume,0,1,v=>{game.Log.volume=v;AudioListener.volume=v;},true);
            SettingSlider(p,80,434,"音乐",game.Log.musicVolume,0,1,v=>game.Log.musicVolume=v,true);
            SettingSlider(p,80,525,"动作与提示音",game.Log.effectsVolume,0,1,v=>game.Log.effectsVolume=v,true);
            SettingSlider(p,80,616,"海浪与环境",game.Log.ambienceVolume,0,1,v=>game.Log.ambienceVolume=v,true);
            SettingSlider(p,860,343,"鼠标灵敏度",game.Log.sensitivity,.3f,2,v=>game.Log.sensitivity=v,false);
            SettingSlider(p,860,434,"视野角度",game.Log.fieldOfView,65,100,v=>game.Log.fieldOfView=v,false);
            Button(p,860,531,316,51,"镜头震动："+(game.Log.shake?"开":"关"),()=>{game.Log.shake=!game.Log.shake;ShowSettings(paused);});
            Button(p,1192,531,328,51,"步行起伏："+(game.Log.headBob?"开":"关"),()=>{game.Log.headBob=!game.Log.headBob;ShowSettings(paused);});
            Button(p,860,598,660,51,"下次新远征："+(game.Log.easy?"轻松模式":"标准模式"),()=>{game.Log.easy=!game.Log.easy;ShowSettings(paused);});
            Text(p,860,670,660,78,"轻松模式：受伤降低 35%，攻击预警延长，奖励不变。\n当前远征的难度保持原选择。\n降低视野起伏可改善长时间探索的舒适度。",17,Muted);
            Button(p,80,775,420,60,"保存并返回",()=>{SaveStore.Write("captain",game.Log);settings=false;if(paused)ShowPause();else ShowHarbor();},true);
        }
        void SettingSlider(RectTransform p,float x,float y,string label,float value,float min,float max,Action<float> changed,bool percent)
        {
            Text(p,x,y,500,31,label,22,Cream);
            var number=Text(p,x+525,y,135,31,percent?Mathf.RoundToInt(value*100)+"%":value.ToString("F1"),21,Gold,TextAlignmentOptions.Right);
            Slider(p,x,y+48,660,value,min,max,v=>{changed(v);number.text=percent?Mathf.RoundToInt(v*100)+"%":v.ToString("F1");});
        }
        void Update()
        {
            if(game==null)return;
            UpdateDisplaySettings();
            var r=game.Run;
            seaTitle.text=game.Island.name+"   /   "+r.stage.ToString("00");
            objective.text=game.QuestObjective;
            inventory.text="鱼篓 "+r.bag.Count+" / "+r.BagCapacity+"   ·   待售 "+r.BagValue+" 金币";
            float bearing=game.Player.transform.eulerAngles.y;compass.text="海湾  N   ·   "+Mathf.RoundToInt(bearing).ToString("000")+"°   ·   港口  S";
            money.text=r.coins.ToString("N0")+" 金币";healthText.text=Mathf.CeilToInt(r.health)+" / "+r.MaxHealth;healthFill.fillAmount=r.health/r.MaxHealth;dashFill.fillAmount=game.Player.DashReady;
            weaponText.text=game.Player.HeldFish?game.Player.HeldFish.Data.Label:game.Player.RodEquipped?"海钓竿 Lv."+r.rodLevel:Balance.Weapons[(int)game.Player.weapon].name;ammoText.text=game.Player.HeldFish?game.Player.HeldFish.Data.value+" G":game.Player.RodEquipped?(game.Charging?Mathf.RoundToInt(game.CastCharge*100)+"%":"READY"):game.Player.Reloading?"装填中":game.Player.Ammo+" / "+game.Player.Capacity;
            notice.text=Time.unscaledTime<game.MessageUntil?game.Message:"";
            noticePanel.gameObject.SetActive(!string.IsNullOrEmpty(notice.text));
            UpdateExpeditionHUD();
            var activeMechanism=game.ActiveBossMechanism;
            hint.text=activeMechanism&&activeMechanism.Active?MechanismInput(activeMechanism):game.Interaction!=""?game.Interaction:game.State==VoyageState.Fishing?"按住左键收线  /  红灯松手  /  Q 收回":game.State==VoyageState.Combat?"左键 射击 · 右键 瞄准 · R 装填":game.Player.RodEquipped?game.AimedHabitat+"\n按住左键蓄力，松开抛竿":"1 切换鱼竿   ·   鱼市与工坊在码头后方";
            status.text="SEED "+r.seed+"   /   "+(r.easy?"轻松":"标准")+"   /   J 任务 · I 图鉴 · M 航图";
            fishPanel.gameObject.SetActive(game.State==VoyageState.Fishing);
            if(game.State==VoyageState.Fishing) {
                fishingTitle.text=!game.FishBiting?"等待咬钩…":game.Surge?"鱼群挣扎！松开左键降张力":"平稳窗口 · 按住左键收线";
                fishingTitle.color=game.Surge&&game.FishBiting?Gold:Mint;progressFill.fillAmount=game.ReelProgress;tensionFill.fillAmount=game.Tension;
                tensionFill.color=game.Tension>.7f?new Color(1,.36f,.26f):Gold;
                fishingHint.text="钓具等级 "+r.rodLevel+"   ·   张力满格会断线   ·   收线进度不会因短暂松手大幅下降";
            }
            Enemy boss=null;foreach(var e in game.Enemies)if(e&&e.IsBoss){boss=e;break;}compassPanel.gameObject.SetActive(boss==null);
            bossPanel.gameObject.SetActive(boss!=null&&game.State==VoyageState.Combat);
            noticePanel.anchoredPosition=new Vector2(440,boss?-115:-118);
            if(boss&&(game.Message.StartsWith("远航手记")||game.Message.StartsWith("第 ")||game.Message.StartsWith("破招成功")||game.Message.StartsWith(boss.DisplayName+" 苏醒")))noticePanel.gameObject.SetActive(false);
            inventory.gameObject.SetActive(!boss);objective.gameObject.SetActive(!boss);explorationKeys.gameObject.SetActive(game.State!=VoyageState.Combat);voyageInfoBackground.rectTransform.sizeDelta=new Vector2(348,boss?47:148);
            if(boss){seaTitle.text=game.Island.name+"  /  首领战";bossName.text=boss.DisplayName+"  /  阶段 "+boss.phase+" · "+(activeMechanism&&activeMechanism.Active?"反制":"破绽");bossFill.fillAmount=boss.health/boss.maxHealth;bossHealth.text=Mathf.CeilToInt(boss.health)+" / "+Mathf.CeilToInt(boss.maxHealth);objective.text=activeMechanism&&activeMechanism.Active?MechanismStrategies[activeMechanism.BossIndex]:"反制完成，核心可以攻击。\n保留闪避，应对反扑。";bossInstruction.text=BossThreatLegend(boss);}
            crosshair.gameObject.SetActive(game.IsPlaying&&!game.Paused);
            float danger=-1;string dangerResponse="";
            if(game.State==VoyageState.Combat)for(int i=0;i<game.Hazards.childCount;i++) {
                var child=game.Hazards.GetChild(i);var warning=child.GetComponent<DeckWarning>();
                if(warning&&GameDirector.FlatDistance(game.Player.transform.position,warning.transform.position)<warning.radius&&(danger<0||warning.Remaining<danger)){danger=warning.Remaining;dangerResponse="落点已锁定 · SHIFT 离开脚下光圈";}
                var threat=child.GetComponent<ThreatField>();string response;
                if(threat&&threat.Threatens(game.Player.transform.position,out response)&&(danger<0||threat.Remaining<danger)){danger=threat.Remaining;dangerResponse=response;}
                var breach=child.GetComponent<TrackingBreach>();
                if(breach&&breach.Threatens(game.Player.transform.position)&&(danger<0||breach.Remaining<danger)){danger=breach.Remaining;dangerResponse=breach.Locked?"破冰线已锁定 · 横向闪出橙色宽带":"蓝线正在追踪 · 准备等锁定后侧闪";}
            }
            dangerPanel.gameObject.SetActive(danger>=0&&!game.Paused);
            if(danger>=0)dangerText.text="! "+dangerResponse+(danger>.05f?" · "+danger.ToString("F1")+" 秒":"");
            targetName.text="";
            if(game.State==VoyageState.Combat&&!game.Paused) {
                RaycastHit rayHit;
                if(Physics.Raycast(game.Player.View.transform.position,game.Player.View.transform.forward,out rayHit,100)) {
                    var enemy=rayHit.collider.GetComponentInParent<Enemy>();
                    if(enemy)targetName.text=enemy.IsBoss?(enemy.Encounter&&enemy.Encounter.DamageFactor<=0?"◆ 核心封锁":rayHit.collider.GetComponent<HitRegion>()?"◇ 弱点":"装甲部位"):(rayHit.collider.GetComponent<HitRegion>()?"<color=#8CFFD1>弱点 · </color>":"")+(enemy.elite?"精英 · ":"")+(enemy.Spec.endemic?"专属 · ":"")+enemy.DisplayName+"   "+Mathf.CeilToInt(enemy.health)+" / "+Mathf.CeilToInt(enemy.maxHealth);
                }
            }
            // Mechanism targets use two lines; resolve their label before choosing
            // the compact boss-anatomy layout, otherwise the second line is clipped.
            UpdateCombatReadability();
            bool compactTarget=boss&&targetName.text.IndexOf('\n')<0;
            targetPanel.anchoredPosition=new Vector2(compactTarget?690:495,-487);
            targetPanel.sizeDelta=new Vector2(compactTarget?220:610,compactTarget?31:58);
            targetBackdrop.rectTransform.sizeDelta=targetPanel.sizeDelta;
            targetName.rectTransform.sizeDelta=new Vector2(compactTarget?200:590,compactTarget?25:50);targetName.fontSize=compactTarget?15:17;
            if(Time.unscaledTime>hitUntil){hit.color=Color.clear;damageNumber.text="";}
            damageOverlay.color=Color.clear;UpdateImpactPresentation(danger);
            targetPanel.gameObject.SetActive(!string.IsNullOrEmpty(targetName.text)&&game.IsPlaying);
            if(SaveStore.LastError!=null&&game.Paused&&!settings)notice.text="保存失败：请确保存档目录可写";
        }
        public void HitMarker(bool critical,float damage,bool killed=false){ShowImpactMarker(critical,damage,killed);}
        public void DamageFlash(){damageUntil=Time.unscaledTime+.55f;damageDirectionKnown=false;}
        public void DamageFlash(Vector3 source){DamageFlash();damageDirection=source;damageDirectionKnown=true;}
        RectTransform Full(string name,Transform parent){var g=new GameObject(name,typeof(RectTransform));var r=g.GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;return r;}
        RectTransform Rect(string name,Transform parent,float x,float y,float w,float h)
        {var g=new GameObject(name,typeof(RectTransform));var r=g.GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
        UnityEngine.UI.Image Box(Transform p,float x,float y,float w,float h,Color c)
        {var r=Rect("Panel",p,x,y,w,h);var im=r.gameObject.AddComponent<UnityEngine.UI.Image>();im.sprite=whiteSprite;im.color=c;im.raycastTarget=false;return im;}
        TextMeshProUGUI Text(Transform p,float x,float y,float w,float h,string str,float size,Color color,TextAlignmentOptions align=TextAlignmentOptions.TopLeft)
        {var r=Rect("Text",p,x,y,w,h);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.fontSize=size;t.color=color;t.text=str;t.alignment=align;t.raycastTarget=false;t.enableWordWrapping=true;t.overflowMode=TextOverflowModes.Overflow;return t;}
        void Button(Transform p,float x,float y,float w,float h,string label,Action action,bool accent=false,bool enabled=true)
        {
            var im=Plate(p,x,y,w,h,accent?new Color(.79f,.66f,.43f):new Color(.085f,.185f,.215f),accent,"Action "+label);im.raycastTarget=true;
            var b=im.gameObject.AddComponent<UnityEngine.UI.Button>();b.targetGraphic=im;b.interactable=enabled;
            var colors=b.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.12f,1.12f,1.12f);colors.pressedColor=new Color(.72f,.86f,.8f);colors.disabledColor=new Color(.6f,.6f,.6f,.5f);b.colors=colors;
            float size=Mathf.Min(w<180?16:20,(h-6)*.64f);
            var title=Text(im.transform,15,2,w-30,h-4,label,size,accent?Ink:Cream,TextAlignmentOptions.Midline);title.enableWordWrapping=false;title.overflowMode=TextOverflowModes.Ellipsis;
            b.onClick.AddListener(()=>{game.Audio.Cue("select");action();});
        }
        UnityEngine.UI.Image Bar(Transform p,float x,float y,float w,float h,Color color)
        {var bg=Box(p,x,y,w,h,new Color(.17f,.29f,.31f));var fill=Box(bg.transform,0,0,w,h,color);fill.type=UnityEngine.UI.Image.Type.Filled;fill.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;fill.fillOrigin=0;return fill;}
        void Slider(Transform p,float x,float y,float w,float value,float min,float max,Action<float> changed)
        {
            var bg=Box(p,x,y,w,20,new Color(.16f,.3f,.33f));bg.raycastTarget=true;
            var fill=Box(bg.transform,0,0,w,20,Mint);var handle=Box(bg.transform,0,-6,20,32,Cream);handle.raycastTarget=true;
            fill.rectTransform.anchorMin=Vector2.zero;fill.rectTransform.anchorMax=Vector2.one;fill.rectTransform.pivot=Vector2.one*.5f;fill.rectTransform.sizeDelta=Vector2.zero;fill.rectTransform.anchoredPosition=Vector2.zero;
            handle.rectTransform.pivot=Vector2.one*.5f;handle.rectTransform.sizeDelta=new Vector2(20,12);handle.rectTransform.anchoredPosition=Vector2.zero;
            var slider=bg.gameObject.AddComponent<UnityEngine.UI.Slider>();slider.fillRect=fill.rectTransform;slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;
            slider.minValue=min;slider.maxValue=max;slider.value=value;slider.onValueChanged.AddListener(v=>changed(v));
        }
    }
}

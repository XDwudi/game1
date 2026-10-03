using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Tidebreak
{
    public class SeaHUD : MonoBehaviour
    {
        static readonly Color Ink=new Color(.035f,.09f,.13f), Panel=new Color(.045f,.13f,.17f,.97f), Cream=new Color(.94f,.92f,.82f), Muted=new Color(.55f,.72f,.72f), Mint=new Color(.37f,.9f,.75f), Gold=new Color(1,.71f,.35f);
        GameDirector game;
        RectTransform root,hud,modal,fishPanel,bossPanel,crosshair,dangerPanel,noticePanel;
        TMP_FontAsset font;
        Sprite whiteSprite;
        TextMeshProUGUI inventory,compass,seaTitle,objective,money,healthText,weaponText,ammoText,notice,hint,bossName,bossHealth,fishingTitle,fishingHint,damageNumber,status,targetName,dangerText;
        UnityEngine.UI.Image healthFill,tensionFill,progressFill,bossFill,dashFill,damageOverlay,hit;
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
            Box(hud,26,23,410,116,new Color(Ink.r,Ink.g,Ink.b,.78f));
            seaTitle=Text(hud,44,31,450,31,"",23,Cream);
            objective=Text(hud,44,66,375,53,"",15,Mint);
            inventory=Text(hud,44,119,400,28,"",14,Cream);
            Box(hud,525,16,550,35,new Color(Ink.r,Ink.g,Ink.b,.7f));
            compass=Text(hud,490,18,620,30,"",14,Cream,TextAlignmentOptions.Center);
            Box(hud,1350,25,222,62,new Color(Ink.r,Ink.g,Ink.b,.84f));
            money=Text(hud,1370,37,180,34,"",25,Gold,TextAlignmentOptions.Right);
            noticePanel=Rect("Voyage notification",hud,370,135,860,52);
            Box(noticePanel,0,0,860,52,new Color(Ink.r,Ink.g,Ink.b,.88f));
            notice=Text(noticePanel,10,0,840,52,"",23,Cream,TextAlignmentOptions.Center);
            notice.fontStyle=FontStyles.Bold;
            Box(hud,26,749,295,120,new Color(Ink.r,Ink.g,Ink.b,.82f));
            Text(hud,44,761,140,25,"船长生命",15,Muted);healthText=Text(hud,171,757,129,30,"",22,Cream,TextAlignmentOptions.Right);
            healthFill=Bar(hud,44,797,256,7,Mint);
            Text(hud,44,821,185,28,"SHIFT  /  冲刺",14,Muted);dashFill=Bar(hud,201,831,99,4,Gold);
            Box(hud,1240,749,331,120,new Color(Ink.r,Ink.g,Ink.b,.82f));
            weaponText=Text(hud,1258,762,190,28,"",18,Cream);
            ammoText=Text(hud,1437,755,112,44,"",29,Gold,TextAlignmentOptions.Right);
            Text(hud,1258,801,291,24,"1 鱼竿  2 左轮  3 霰弹  4 鱼叉",13,Muted,TextAlignmentOptions.Right);
            Text(hud,1258,831,291,24,"R 装填   ·   右键瞄准",14,Muted,TextAlignmentOptions.Right);
            Box(hud,350,750,860,59,new Color(.035f,.09f,.13f,.82f));
            hint=Text(hud,363,762,834,45,"",18,Cream,TextAlignmentOptions.Center);
            Text(hud,357,846,820,27,"WASD 移动    SPACE 跳跃    E 交互    F 收纳    Q 抛出    TAB 鱼篓",14,Muted,TextAlignmentOptions.Center);
            status=Text(hud,470,867,660,21,"",11,Muted,TextAlignmentOptions.Center);
            crosshair=Rect("Crosshair",hud,780,430,40,40);
            Box(crosshair,17,1,3,10,Cream);Box(crosshair,17,27,3,10,Cream);Box(crosshair,1,17,10,3,Cream);Box(crosshair,27,17,10,3,Cream);Box(crosshair,17,17,3,3,Mint);
            hit=Box(hud,793,443,14,14,Color.clear);hit.rectTransform.localRotation=Quaternion.Euler(0,0,45);
            damageNumber=Text(hud,816,418,130,40,"",23,Gold);
            targetName=Text(hud,510,507,580,50,"",17,Cream,TextAlignmentOptions.Center);
            dangerPanel=Rect("Incoming strike warning",hud,505,591,590,57);
            Box(dangerPanel,0,0,590,57,new Color(.53f,.1f,.11f,.94f));dangerText=Text(dangerPanel,12,10,566,38,"",21,Cream,TextAlignmentOptions.Center);
            fishPanel=Rect("Fishing instrument",hud,495,654,610,126);Box(fishPanel,0,0,610,126,Panel);
            fishingTitle=Text(fishPanel,22,13,565,29,"",20,Cream);
            Text(fishPanel,22,52,85,20,"收线进度",13,Muted);progressFill=Bar(fishPanel,113,58,472,7,Mint);
            Text(fishPanel,22,81,85,20,"鱼线张力",13,Muted);tensionFill=Bar(fishPanel,113,87,472,7,Gold);
            fishingHint=Text(fishPanel,22,103,565,21,"",11,Muted);
            bossPanel=Rect("Boss life",hud,442,30,716,90);
            Box(bossPanel,0,0,716,90,new Color(Ink.r,Ink.g,Ink.b,.87f));
            bossName=Text(bossPanel,20,10,535,30,"",24,Cream);bossHealth=Text(bossPanel,546,11,149,28,"",17,Gold,TextAlignmentOptions.Right);
            bossFill=Bar(bossPanel,20,54,675,8,new Color(.94f,.42f,.35f));
            Text(bossPanel,20,69,675,18,"红圈预警 → 冲刺躲避 → 攻击发光弱点",11,Muted,TextAlignmentOptions.Center);
            damageOverlay=Box(root,0,0,1600,900,Color.clear);damageOverlay.raycastTarget=false;
        }
        public void ShowState()
        {
            settings=false;ClearModal();hud.gameObject.SetActive(game.IsPlaying);
            switch(game.State){case VoyageState.Harbor:ShowHarbor();break;case VoyageState.Shop:ShowShop();break;case VoyageState.Route:ShowRoute();break;case VoyageState.Victory:ShowResult(true);break;case VoyageState.Defeat:ShowResult(false);break;}
        }
        void ClearModal(){if(modal){modal.gameObject.SetActive(false);Destroy(modal.gameObject);}modal=null;}
        RectTransform NewModal(bool shade=true)
        {ClearModal();modal=Full("Menu",root);if(shade)Box(modal,0,0,1600,900,new Color(.015f,.06f,.085f,.82f));return modal;}
        public void ShowHarbor()
        {
            settings=false;hud.gameObject.SetActive(false);var p=NewModal(false);
            Box(p,0,0,631,900,new Color(.025f,.09f,.13f,.96f));Box(p,631,0,2,900,new Color(Mint.r,Mint.g,Mint.b,.3f));
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
            Text(p,60,807,510,44,"海岛钓猎  ·  金币工坊  ·  深渊巨物\nWIN 64   /   v0.2.0",13,Muted);
            Text(p,1075,741,452,38,"PINEHAVEN",27,Cream,TextAlignmentOptions.Right).characterSpacing=4;
            Text(p,1075,786,452,43,"松风港  /  钓猎远征\n完成 "+game.Log.victories+" 次远征  ·  克拉肯 "+game.Log.krakens+" 次",15,Cream,TextAlignmentOptions.Right);
        }
        void Header(RectTransform p,string eyebrow,string title,string sub)
        {Text(p,80,53,1440,25,eyebrow,14,Mint);Text(p,76,99,1448,69,title,48,Cream);Text(p,80,185,1420,53,sub,20,Muted);Box(p,80,250,1440,1,new Color(.2f,.4f,.42f));}
        public void ShowShop()
        {
            var p=NewModal();Header(p,"ROWAN'S WORKSHOP   /   老船长工坊","每一枚金币，都来自海洋","持有 "+game.Run.coins+" 金币     ·     生命 "+Mathf.CeilToInt(game.Run.health)+" / "+game.Run.MaxHealth+"     ·     所有升级均需金币购买");
            string[] ids={"weapon","rod","hull","heal","shotgun","harpoon","bait","relic0","relic1","relic2"};
            string[] names={"枪械改装  "+game.Run.weaponLevel+" / 5","钓具改装  "+game.Run.rodLevel+" / 3","防护背心  "+game.Run.hullLevel+" / 3","热鱼汤","礁石霰弹枪","雷鸣鱼叉","禁忌鱼饵",game.Choices[0].name,game.Choices[1].name,game.Choices[2].name};
            string[] desc={"基础伤害倍率 +18%\n所有已购武器共同受益","碳素竿身与强化鱼线\n更快收线，更稳控线","生命上限 +25\n购买时立即恢复 25 生命","恢复 40 生命\n生命满时不可购买","七发散射，近距离高伤害\n按 3 切换；远距离伤害衰减","远距离精确穿刺\n按 4 切换；弹匣 3 发","完成主线后开启克拉肯\n消耗金币购买，本局有效",game.Choices[0].description,game.Choices[1].description,game.Choices[2].description};
            for(int i=0;i<10;i++) {
                string id=ids[i];float x=80+(i%5)*292;float y=280+(i/5)*215;
                Box(p,x,y,274,197,Panel);Box(p,x,y,274,3,i>=7?Gold:Mint);
                Text(p,x+17,y+17,240,32,names[i],21,Cream);Text(p,x+17,y+65,240,65,desc[i],15,Muted);
                int price=game.Price(id);bool can=price>=0&&game.Run.coins>=price;
                Button(p,x+17,y+145,240,38,price<0?(i>=7?"本港已购":id=="heal"?"生命已满":"已拥有 / 已满级"):price+" 金币 · "+(can?"购买":"金币不足"),()=>game.Buy(id),can,can);
            }
            Text(p,80,740,1000,62,"右下三件配件为本航段随机库存，每件限购一次。\n买不起可以返回码头继续钓鱼；击杀不会自动卖鱼，请到鱼获收购柜台交易。",16,Muted);
            Button(p,1130,772,390,59,"返回海岛    ESC",game.CloseShop,true);
        }
        void ShowRoute()
        {
            var p=NewModal();Header(p,"CHART A COURSE   /   航线抉择","下一竿，钓向何处？","下一航段 "+(game.Run.stage+1)+" / 9   ·   每三处航段需要击败守关 BOSS。");
            string[] titles={"丰饶浅滩","猎手暗礁","幽光航道"};string[] english={"THE SHOALS","THE HUNT","THE ABYSS"};
            string[] desc={"战利品金币 +15%\n稳定积累，适合完善装备。","怪物生命 +12%\n精英鱼出现概率 +28%。\n危险更高，精英奖励也更丰厚。","立即恢复 12 生命\n额外 20% 概率发现古神信号。\n通关后或可挑战稀有巨物。"};
            for(int i=0;i<3;i++) {
                int index=i;float x=80+i*489;Color c=i==0?Mint:i==1?Gold:new Color(.75f,.57f,1);
                Box(p,x,298,463,389,Panel);Text(p,x+30,329,400,30,english[i],16,c);Text(p,x+30,386,400,48,titles[i],35,Cream);
                Text(p,x+30,457,400,126,desc[i],19,Muted);Button(p,x+30,603,403,55,"驶向这里    →",()=>game.ChooseRoute((RouteKind)index),true);
            }
        }
        void ShowResult(bool victory)
        {
            var p=NewModal();Header(p,victory?"VOYAGE COMPLETE   /   远征记录":"THE SEA REMEMBERS   /   远征记录",victory?(game.Run.krakenDefeated?"古神陨落，传说归航":game.Run.whaleDefeated?"幽海传说，收入囊中":"破浪归来，船长"):"海潮会记住这次冒险",victory?"你的战利品已写入船长日志。下一趟海域，会有新的选择。":"失去的是这趟装备。留下的是经验、图鉴，和下一次机会。");
            string[] labels={"到达钓点","猎获怪物","累计金币","远征用时"};
            string[] values={game.Run.stage.ToString(),game.Run.kills.ToString(),game.Run.earned.ToString(),TimeSpan.FromSeconds(game.Run.elapsed).ToString(@"mm\:ss")};
            for(int i=0;i<4;i++){float x=80+i*366;Box(p,x,302,342,154,Panel);Text(p,x+24,324,296,32,labels[i],17,Muted);Text(p,x+22,368,296,66,values[i],47,Cream);}
            bool kraken=victory&&game.Run.stage==9&&(game.Run.abyssBait||game.Run.rareSignal);
            if(kraken){Text(p,80,521,1440,71,"深渊传来回应。稀有 BOSS「克拉肯」可被唤醒。\n挑战前恢复 50 点生命；战败仍会保留本次主线通关纪录。",23,Gold);Button(p,80,642,680,66,"继续：挑战克拉肯    →",game.ChallengeKraken,true);}
            else Text(p,80,521,1440,80,victory?"稀有传闻：购买「禁忌鱼饵」，或沿幽光航道发现古神信号，\n可在下一次通关后挑战克拉肯。":"船长建议：红圈出现后再冲刺，注意发光弱点，\n不要在鱼线发红时一直按住收线。",23,Muted);
            if(victory&&game.Run.stage==9&&game.Run.rareSignal)Button(p,800,642,720,66,"罕见遭遇：幽海白鲸    →",game.ChallengeWhiteWhale);
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
            var p=NewModal();Header(p,"CAPTAIN'S FIELD GUIDE","船长手册","从松风港出发，完成 9 段海域委托。买装备、挑战首领；可以随时保存离开。");
            string[] titles={"01  抛竿与控线","02  射击与生存","03  成长与巨物"};
            string[] desc={"WASD 行走，SPACE 跳跃。\n按 1 拿起鱼竿，面向开阔海面。\n按住左键蓄力，松开抛出浮漂。\n\n咬钩后，绿灯按住左键收线；\n红灯松手降低张力。\n收线完成，怪鱼会跃出海面。", "按 2 拔枪，左键射击，R 装填。\n右键瞄准，SHIFT 冲刺闪避。\n凌空击杀与弱点击杀提高售价。\n\n击倒鱼后按 E 拿起；\nF 收进鱼篓，Q 可以把鱼抛出。\n鱼篓最多容纳 12 条鱼。", "港内鱼获收购柜台按 E 出售鱼获。\n老船长工坊用金币购买所有升级。\n随机配件也需要金币。\n\n完成委托，到港内航图按 E 前进。\n第 3 / 6 / 9 航段在码头按 E\n召唤首领；鱼饵可解锁克拉肯。"};
            for(int i=0;i<3;i++){float x=80+i*489;Box(p,x,295,463,401,Panel);Text(p,x+26,321,415,45,titles[i],28,Gold);Text(p,x+26,389,413,281,desc[i],21,Muted);}
            Button(p,80,772,400,58,"返回",()=>{if(game.Paused)ShowPause();else ShowHarbor();},true);
        }
        void ShowJournal()
        {
            var p=NewModal();Header(p,"THE BESTIARY   /   海洋图鉴","有些传说，长着牙齿","击败生物即可永久记录。精英个体有紫色鳞片，体型与战利品均更强。");
            string[] desc={"普通 · 快速水弹","普通 · 范围毒刺","普通 · 高速追猎","守关 · 双钳重击 / 半血双重砸击","守关 · 扇形弹幕 / 半血追加范围攻击","守关 · 风暴轰击 / 狂暴连击","稀有 · 触腕连击 / 墨汁弹幕 / 深渊狂潮","稀有 · 霜潮齐射 / 甲板冰爆 / 半血封锁两翼"};
            for(int i=0;i<8;i++){float y=275+i*58;bool seen=game.Log.discovered[i];Box(p,80,y,1440,51,Panel);Text(p,101,y+8,435,32,(seen?"◆  ":"◇  ")+Balance.CreatureName((CreatureKind)i),23,seen?Gold:Cream);Text(p,565,y+11,700,28,(seen?"已收录   /   ":"未发现   /   ")+desc[i],16,Muted);Text(p,1270,y+12,230,28,(game.Log.goldDiscovered[i]?"金鳞收录  ":"")+(game.Log.heaviest[i]>0?game.Log.heaviest[i].ToString("F1")+" kg":""),15,Gold,TextAlignmentOptions.Right);}
            Button(p,80,778,400,58,"返回港口",ShowHarbor,true);
        }
        public void ShowBag()
        {
            var p=NewModal();Header(p,"CATCH MANIFEST   /   鱼篓","今天的收获","已收纳 "+game.Run.bag.Count+" / 12 条鱼     ·     合计估价 "+game.Run.BagValue+" 金币     ·     到鱼获收购柜台按 E 出售");
            for(int i=0;i<12;i++) {
                float x=80+(i%4)*366,y=278+(i/4)*147;Box(p,x,y,346,130,Panel);
                if(i>=game.Run.bag.Count){Text(p,x+20,y+44,306,40,"空 鱼 篓",19,Muted,TextAlignmentOptions.Center);continue;}
                var f=game.Run.bag[i];Text(p,x+19,y+15,308,33,f.Label,21,f.quality==2?Gold:Cream);
                Text(p,x+19,y+59,308,29,f.weight.ToString("F1")+" kg     "+f.value+" 金币",18,Mint);
                Text(p,x+19,y+99,308,22,(f.airshot?"凌空 +25%  ":"")+(f.weakshot?"弱点 +15%":""),12,Muted);
            }
            Button(p,1120,772,400,59,"继续钓猎    ESC",game.TogglePause,true);
            Text(p,80,782,960,45,"鱼获已随航程保存；拿在手里的鱼按 F 收纳。珍稀金鳞与最大体重会留在永久图鉴。",16,Muted);
        }
        void ShowSettings(bool paused)
        {
            settings=true;var p=NewModal();Header(p,"SHIP'S INSTRUMENTS   /   设置","按你的节奏航行","音量与鼠标灵敏度即时生效；轻松模式在下一次新远征时生效。");
            Text(p,80,300,680,38,"总音量",24,Cream);Slider(p,80,357,660,game.Log.volume,0,1,v=>{game.Log.volume=v;AudioListener.volume=v;});
            Text(p,80,430,680,38,"鼠标灵敏度",24,Cream);Slider(p,80,487,660,game.Log.sensitivity,.3f,2,v=>game.Log.sensitivity=v);
            Button(p,860,305,660,63,"镜头震动："+(game.Log.shake?"开启":"关闭"),()=>{game.Log.shake=!game.Log.shake;ShowSettings(paused);});
            Button(p,860,400,660,63,"下次远征："+(game.Log.easy?"轻松模式":"标准模式"),()=>{game.Log.easy=!game.Log.easy;ShowSettings(paused);});
            Text(p,860,498,660,110,"轻松模式：受到的伤害降低 35%，\n攻击预警延长，奖励保持一致。\n当前远征的难度不会改变。",21,Muted);
            Button(p,80,775,420,60,"保存并返回",()=>{SaveStore.Write("captain",game.Log);settings=false;if(paused)ShowPause();else ShowHarbor();},true);
        }
        void Update()
        {
            if(game==null)return;
            var r=game.Run;
            seaTitle.text=Balance.Seas[r.Act]+"   /   "+r.stage.ToString("00");
            objective.text=r.RouteReady?"委托完成 · 港内航图可前往下一站":r.BossStage?"委托：击败首领\n码头尽头 E 摇钟；可先钓鱼攒钱":"委托：收集鱼获 "+r.landed+" / "+r.Quota+"\n钓鱼 → 击倒 → 拿起 → 鱼市出售";
            inventory.text="鱼篓 "+r.bag.Count+" / 12   ·   待售 "+r.BagValue+" 金币";
            float bearing=game.Player.transform.eulerAngles.y;compass.text="海湾  N   ·   "+Mathf.RoundToInt(bearing).ToString("000")+"°   ·   港口  S";
            money.text=r.coins+"  金币";healthText.text=Mathf.CeilToInt(r.health)+" / "+r.MaxHealth;healthFill.fillAmount=r.health/r.MaxHealth;dashFill.fillAmount=game.Player.DashReady;
            weaponText.text=game.Player.HeldFish?game.Player.HeldFish.Data.Label:game.Player.RodEquipped?"碳素海钓竿  Lv."+r.rodLevel:Balance.Weapons[(int)game.Player.weapon].name;ammoText.text=game.Player.HeldFish?game.Player.HeldFish.Data.value+" G":game.Player.RodEquipped?(game.Charging?Mathf.RoundToInt(game.CastCharge*100)+"%":"READY"):game.Player.Reloading?"装填中":game.Player.Ammo+" / "+game.Player.Capacity;
            notice.text=Time.unscaledTime<game.MessageUntil?game.Message:"";
            noticePanel.gameObject.SetActive(!string.IsNullOrEmpty(notice.text));
            hint.text=game.Interaction!=""?game.Interaction:game.State==VoyageState.Fishing?"按住左键收线  /  红灯松手  /  Q 收回":game.Player.RodEquipped?"面向海面 · 按住左键蓄力，松开抛竿":game.State==VoyageState.Combat?(game.Enemies.Count>0&&game.Enemies[0].IsBoss?"避开红圈和水弹   ·   等待破绽，集中攻击发光弱点":"凌空击杀 +25% 售价   ·   瞄准头部弱点"):"1 切换鱼竿   ·   鱼市与工坊在码头后方";
            status.text="SEED "+r.seed+"   /   "+(r.easy?"轻松":"标准")+"   /   ESC 暂停";
            fishPanel.gameObject.SetActive(game.State==VoyageState.Fishing);
            if(game.State==VoyageState.Fishing) {
                fishingTitle.text=!game.FishBiting?"等待咬钩…":game.Surge?"鱼群挣扎！松开左键降张力":"平稳窗口 · 按住左键收线";
                fishingTitle.color=game.Surge&&game.FishBiting?Gold:Mint;progressFill.fillAmount=game.ReelProgress;tensionFill.fillAmount=game.Tension;
                tensionFill.color=game.Tension>.7f?new Color(1,.36f,.26f):Gold;
                fishingHint.text="钓具等级 "+r.rodLevel+"   ·   张力满格会断线   ·   收线进度不会因短暂松手大幅下降";
            }
            Enemy boss=null;foreach(var e in game.Enemies)if(e&&e.IsBoss){boss=e;break;}
            bossPanel.gameObject.SetActive(boss!=null&&game.State==VoyageState.Combat);
            if(boss){bossName.text=Balance.CreatureName(boss.kind)+"  /  "+(boss.phase==2?"狂暴":boss.Exposed?"弱点暴露":"蓄势");bossFill.fillAmount=boss.health/boss.maxHealth;bossHealth.text=Mathf.CeilToInt(boss.health)+" / "+boss.maxHealth;}
            crosshair.gameObject.SetActive(game.IsPlaying&&!game.Paused);
            float danger=-1;
            if(game.State==VoyageState.Combat)for(int i=0;i<game.Hazards.childCount;i++) {
                var warning=game.Hazards.GetChild(i).GetComponent<DeckWarning>();if(!warning)continue;
                Vector3 delta=game.Player.transform.position-warning.transform.position;delta.y=0;
                if(delta.magnitude<warning.radius&&(danger<0||warning.Remaining<danger))danger=warning.Remaining;
            }
            dangerPanel.gameObject.SetActive(danger>=0&&!game.Paused);
            if(danger>=0)dangerText.text="脚下危险 · "+danger.ToString("F1")+" 秒  /  SHIFT 冲刺";
            targetName.text="";
            if(game.State==VoyageState.Combat&&!game.Paused) {
                RaycastHit rayHit;
                if(Physics.Raycast(game.Player.View.transform.position,game.Player.View.transform.forward,out rayHit,100)) {
                    var enemy=rayHit.collider.GetComponentInParent<Enemy>();
                    if(enemy)targetName.text=(enemy.elite?"精英 · ":"")+Balance.CreatureName(enemy.kind)+"   "+Mathf.CeilToInt(enemy.health)+" / "+Mathf.CeilToInt(enemy.maxHealth);
                }
            }
            if(Time.unscaledTime>hitUntil){hit.color=Color.clear;damageNumber.text="";}
            damageOverlay.color=new Color(.8f,.12f,.08f,Mathf.Clamp01((damageUntil-Time.unscaledTime)/.4f)*.25f);
            if(SaveStore.LastError!=null&&game.Paused&&!settings)notice.text="保存失败：请确保存档目录可写";
        }
        public void HitMarker(bool critical,float damage){hit.color=critical?Gold:Mint;hitUntil=Time.unscaledTime+.13f;damageNumber.text=Mathf.RoundToInt(damage)+(critical?"!":"");}
        public void DamageFlash(){damageUntil=Time.unscaledTime+.4f;}
        RectTransform Full(string name,Transform parent){var g=new GameObject(name,typeof(RectTransform));var r=g.GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;return r;}
        RectTransform Rect(string name,Transform parent,float x,float y,float w,float h)
        {var g=new GameObject(name,typeof(RectTransform));var r=g.GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
        UnityEngine.UI.Image Box(Transform p,float x,float y,float w,float h,Color c)
        {var r=Rect("Panel",p,x,y,w,h);var im=r.gameObject.AddComponent<UnityEngine.UI.Image>();im.sprite=whiteSprite;im.color=c;im.raycastTarget=false;return im;}
        TextMeshProUGUI Text(Transform p,float x,float y,float w,float h,string str,float size,Color color,TextAlignmentOptions align=TextAlignmentOptions.TopLeft)
        {var r=Rect("Text",p,x,y,w,h);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.fontSize=size;t.color=color;t.text=str;t.alignment=align;t.raycastTarget=false;t.enableWordWrapping=true;t.overflowMode=TextOverflowModes.Overflow;return t;}
        void Button(Transform p,float x,float y,float w,float h,string label,Action action,bool accent=false,bool enabled=true)
        {
            var im=Box(p,x,y,w,h,accent?Mint:new Color(.1f,.22f,.26f));im.raycastTarget=true;
            var b=im.gameObject.AddComponent<UnityEngine.UI.Button>();b.targetGraphic=im;b.interactable=enabled;
            var colors=b.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.12f,1.12f,1.12f);colors.pressedColor=new Color(.72f,.86f,.8f);colors.disabledColor=new Color(.6f,.6f,.6f,.5f);b.colors=colors;
            Text(im.transform,15,3,w-30,h-6,label,20,accent?Ink:Cream,TextAlignmentOptions.Midline);
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

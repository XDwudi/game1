using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Tidebreak
{
    public partial class SeaHUD : MonoBehaviour
    {
        static readonly Color Ink=new Color(.035f,.09f,.13f), Panel=new Color(.045f,.13f,.17f,.97f), Cream=new Color(.94f,.92f,.82f), Muted=new Color(.55f,.72f,.72f), Mint=new Color(.37f,.9f,.75f), Gold=new Color(1,.71f,.35f);
        GameDirector game;
        RectTransform root,hud,modal,fishPanel,bossPanel,crosshair,dangerPanel,noticePanel,compassPanel;
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
            Box(hud,26,23,410,155,new Color(Ink.r,Ink.g,Ink.b,.78f));
            seaTitle=Text(hud,44,31,450,31,"",23,Cream);
            objective=Text(hud,44,66,375,90,"",15,Mint);
            inventory=Text(hud,44,149,400,28,"",14,Cream);
            compassPanel=Rect("Bearing panel",hud,525,16,550,35);Box(compassPanel,0,0,550,35,new Color(Ink.r,Ink.g,Ink.b,.7f));
            compass=Text(compassPanel,0,2,550,30,"",14,Cream,TextAlignmentOptions.Center);
            Box(hud,1350,25,222,62,new Color(Ink.r,Ink.g,Ink.b,.84f));
            money=Text(hud,1370,37,180,34,"",25,Gold,TextAlignmentOptions.Right);
            noticePanel=Rect("Voyage notification",hud,460,135,800,66);
            Box(noticePanel,0,0,800,66,new Color(Ink.r,Ink.g,Ink.b,.88f));
            notice=Text(noticePanel,16,6,768,54,"",19,Cream,TextAlignmentOptions.Center);
            notice.fontStyle=FontStyles.Bold;
            Box(hud,26,749,295,120,new Color(Ink.r,Ink.g,Ink.b,.82f));
            Text(hud,44,761,140,25,"船长生命",15,Muted);healthText=Text(hud,171,757,129,30,"",22,Cream,TextAlignmentOptions.Right);
            healthFill=Bar(hud,44,797,256,7,Mint);
            Text(hud,44,821,185,28,"SHIFT  /  冲刺",14,Muted);dashFill=Bar(hud,201,831,99,4,Gold);
            Box(hud,1240,749,331,120,new Color(Ink.r,Ink.g,Ink.b,.82f));
            weaponText=Text(hud,1258,762,190,28,"",18,Cream);
            ammoText=Text(hud,1437,755,112,44,"",29,Gold,TextAlignmentOptions.Right);
            Text(hud,1258,801,291,24,"1 鱼竿 · 2–7 武器 · B 拟饵",13,Muted,TextAlignmentOptions.Right);
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
            fishPanel=Rect("Fishing instrument",hud,495,610,610,126);Box(fishPanel,0,0,610,126,Panel);
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
            settings=false;ClearModal();hud.gameObject.SetActive(game.IsPlaying);if(game.State!=VoyageState.Combat){hitUntil=damageUntil=0;hit.color=Color.clear;damageNumber.text="";}
            switch(game.State){case VoyageState.Harbor:ShowHarbor();break;case VoyageState.Shop:ShowShop();break;case VoyageState.Route:ShowRoute();break;case VoyageState.Victory:ShowResult(true);break;case VoyageState.Defeat:ShowResult(false);break;}
        }
        void ClearModal(){ClearSpecimen();if(modal){modal.gameObject.SetActive(false);Destroy(modal.gameObject);}modal=null;}
        RectTransform NewModal(bool shade=true)
        {ClearModal();modal=Full("Menu",root);if(shade)Box(modal,0,0,1600,900,new Color(.015f,.06f,.085f,.97f));return modal;}
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
            Text(p,60,807,510,44,"九岛剧情  ·  108 种生物  ·  逐步解锁\nWIN 64   /   v0.3.0",13,Muted);
            Text(p,1075,741,452,38,"PINEHAVEN",27,Cream,TextAlignmentOptions.Right).characterSpacing=4;
            Text(p,1075,786,452,43,"松风港  /  钓猎远征\n完成 "+game.Log.victories+" 次远征  ·  克拉肯 "+game.Log.krakens+" 次",15,Cream,TextAlignmentOptions.Right);
        }
        void Header(RectTransform p,string eyebrow,string title,string sub)
        {Text(p,80,53,1440,25,eyebrow,14,Mint);Text(p,76,99,1448,69,title,48,Cream);Text(p,80,185,1420,53,sub,20,Muted);Box(p,80,250,1440,1,new Color(.2f,.4f,.42f));}
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
            else Text(p,80,521,1440,80,victory?"稀有传闻：购买「禁忌鱼饵」，或解开岛上的隐藏宝箱。\n继续探索后，可随时在航图中进入传说海域。":"船长建议：红圈出现后再冲刺，注意发光弱点，\n不要在鱼线发红时一直按住收线。",23,Muted);
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
            var p=NewModal();Header(p,"CAPTAIN'S FIELD GUIDE","船长手册","从松风港出发，与向导交谈 → 收集不同样本 → 探索遗迹 → 首领潮核 → 解锁新岛。");
            string[] titles={"01  抛竿与控线","02  射击与生存","03  成长与巨物"};
            string[] desc={"WASD 行走，SPACE 跳跃。\n按 1 拿起鱼竿，面向开阔海面。\n按住左键蓄力，松开抛出浮漂。\n\n咬钩后，绿灯按住左键收线；\n红灯松手降低张力。\n收线完成，怪鱼会跃出海面。", "按 2 拔枪，左键射击，R 装填。\n右键瞄准，SHIFT 冲刺闪避。\n凌空击杀与弱点击杀提高售价。\n\n击倒鱼后按 E 拿起；\nF 收进鱼篓，Q 可以把鱼抛出。\n鱼篓可在工坊扩容。", "港内鱼获收购柜台按 E 出售鱼获。\n老船长工坊用金币购买所有升级。\n随机配件也需要金币。\n\n完成委托，到港内航图按 E 选择岛屿。\n各岛完成调查后，在码头摇钟。\n首领潮核必须交给向导解锁航线。"};
            for(int i=0;i<3;i++){float x=80+i*489;Box(p,x,295,463,401,Panel);Text(p,x+26,321,415,45,titles[i],28,Gold);Text(p,x+26,389,413,281,desc[i],21,Muted);}
            Button(p,80,772,400,58,"返回",()=>{if(game.Paused)ShowPause();else ShowHarbor();},true);
        }
        public void ShowJournal(){BuildCodex();}
        public void ShowBag(){BuildBagPage();}
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
            seaTitle.text=game.Island.name+"   /   "+r.stage.ToString("00");
            objective.text=game.QuestObjective;
            inventory.text="鱼篓 "+r.bag.Count+" / "+r.BagCapacity+"   ·   待售 "+r.BagValue+" 金币";
            float bearing=game.Player.transform.eulerAngles.y;compass.text="海湾  N   ·   "+Mathf.RoundToInt(bearing).ToString("000")+"°   ·   港口  S";
            money.text=r.coins+"  金币";healthText.text=Mathf.CeilToInt(r.health)+" / "+r.MaxHealth;healthFill.fillAmount=r.health/r.MaxHealth;dashFill.fillAmount=game.Player.DashReady;
            weaponText.text=game.Player.HeldFish?game.Player.HeldFish.Data.Label:game.Player.RodEquipped?"海钓竿 Lv."+r.rodLevel:Balance.Weapons[(int)game.Player.weapon].name;ammoText.text=game.Player.HeldFish?game.Player.HeldFish.Data.value+" G":game.Player.RodEquipped?(game.Charging?Mathf.RoundToInt(game.CastCharge*100)+"%":"READY"):game.Player.Reloading?"装填中":game.Player.Ammo+" / "+game.Player.Capacity;
            notice.text=Time.unscaledTime<game.MessageUntil?game.Message:"";
            noticePanel.gameObject.SetActive(!string.IsNullOrEmpty(notice.text));
            UpdateExpeditionHUD();
            hint.text=game.Interaction!=""?game.Interaction:game.State==VoyageState.Fishing?"按住左键收线  /  红灯松手  /  Q 收回":game.Player.RodEquipped?"面向海面 · 按住左键蓄力，松开抛竿":game.State==VoyageState.Combat?(game.Enemies.Count>0&&game.Enemies[0].IsBoss?"避开红圈和水弹   ·   等待破绽，集中攻击发光弱点":"凌空击杀 +25% 售价   ·   瞄准头部弱点"):"1 切换鱼竿   ·   鱼市与工坊在码头后方";
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
            if(boss){bossName.text=boss.DisplayName+"  /  "+(boss.phase==2?"狂暴":boss.Exposed?"弱点暴露":"蓄势");bossFill.fillAmount=boss.health/boss.maxHealth;bossHealth.text=Mathf.CeilToInt(boss.health)+" / "+boss.maxHealth;}
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
                    if(enemy)targetName.text=(enemy.elite?"精英 · ":"")+enemy.DisplayName+"   "+Mathf.CeilToInt(enemy.health)+" / "+Mathf.CeilToInt(enemy.maxHealth)+"\n<color=#FFC06A>"+enemy.Telegraph+"</color>";
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

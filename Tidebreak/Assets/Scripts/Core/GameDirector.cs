using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tidebreak
{
    public class GameDirector : MonoBehaviour
    {
        public static GameDirector Instance;
        public RunData Run=new RunData();
        public CaptainLog Log;
        public VoyageState State=VoyageState.Harbor;
        public bool Paused,Automation;
        public System.Random Rng;
        public SeaWorld World;
        public AnglerController Player;
        public SeaHUD UI;
        public SeaAudio Audio;
        public readonly List<Enemy> Enemies=new List<Enemy>();
        public readonly List<FishLoot> Loot=new List<FishLoot>();
        public Relic[] Choices;
        public string Message="",Interaction="";
        public float MessageUntil;
        public float ReelProgress,Tension,FishingAge,CastCharge;
        public bool Charging;
        public bool Surge {get{return Mathf.Sin((FishingAge-1.9f)*2)>.28f;}}
        public bool FishBiting {get{return FishingAge>1.9f;}}
        public int LastReward;
        public Transform Hazards;
        public bool IsPlaying {get{return State==VoyageState.Sailing||State==VoyageState.Fishing||State==VoyageState.Combat;}}
        public Vector3 CastPoint {get{return castPoint;}}
        LineRenderer fishingLine;
        GameObject bobber;
        Vector3 castPoint,castStart;
        float nextLineSound;
        FishLoot targetLoot;
        int interactionKind;
        void Awake()
        {
            Instance=this;Automation=Array.IndexOf(Environment.GetCommandLineArgs(),"-tidebreakSmoke")>=0;
            if(Automation)SaveStore.DirectoryOverride=System.IO.Path.Combine(Application.temporaryCachePath,"TidebreakIslandQA");
            Application.targetFrameRate=90;QualitySettings.vSyncCount=0;
            Log=SaveStore.Read<CaptainLog>("captain")??new CaptainLog();
            if(Log.discovered==null)Log.discovered=new bool[8];else if(Log.discovered.Length!=8)Array.Resize(ref Log.discovered,8);
            if(Log.goldDiscovered==null)Log.goldDiscovered=new bool[8];else if(Log.goldDiscovered.Length!=8)Array.Resize(ref Log.goldDiscovered,8);
            if(Log.heaviest==null)Log.heaviest=new float[8];else if(Log.heaviest.Length!=8)Array.Resize(ref Log.heaviest,8);
            AudioListener.volume=Log.volume;Rng=new System.Random();
            World=new GameObject("Coastal world").AddComponent<SeaWorld>();World.Build();
            Hazards=new GameObject("Encounter effects").transform;
            Audio=gameObject.AddComponent<SeaAudio>();Audio.Init();
            Player=new GameObject("Captain").AddComponent<AnglerController>();Player.Init(this);
            UI=gameObject.AddComponent<SeaHUD>();UI.Init(this);SetState(VoyageState.Harbor);
            if(Automation)gameObject.AddComponent<SmokePilot>();
        }
        void Update()
        {
            if(Input.GetKeyDown(KeyCode.Escape)) {
                if(State==VoyageState.Harbor)UI.ShowHarbor();
                else if(State==VoyageState.Shop||State==VoyageState.Route)CloseShop();
                else if(State!=VoyageState.Victory&&State!=VoyageState.Defeat)TogglePause();
            }
            if(Paused)return;
            if(IsPlaying&&Input.GetKeyDown(KeyCode.Tab)){TogglePause();UI.ShowBag();return;}
            if(State==VoyageState.Harbor) {
                float a=Time.unscaledTime*.012f;
                Player.Teleport(new Vector3(28+Mathf.Sin(a)*2,14,28));Player.transform.LookAt(new Vector3(0,1,-7));Player.View.transform.localRotation=Quaternion.identity;return;
            }
            if(IsPlaying)Run.elapsed+=Time.deltaTime;
            UpdateInteraction();
            if(IsPlaying&&Input.GetKeyDown(KeyCode.E)&&!Automation)Interact();
            if(State==VoyageState.Sailing&&Player.RodEquipped&&!Player.HeldFish&&!Automation) {
                if(Input.GetMouseButtonDown(0)){Charging=true;CastCharge=0;}
                if(Charging)CastCharge=Mathf.Min(1,CastCharge+Time.deltaTime*.8f);
                if(Charging&&Input.GetMouseButtonUp(0)){Charging=false;Cast();}
            }
            if(State==VoyageState.Fishing&&!Automation)TickFishing(Input.GetMouseButton(0),Time.deltaTime);
            if(State==VoyageState.Fishing&&Input.GetKeyDown(KeyCode.Q))CancelFishing();
        }
        void SetRegion(int region)
        {
            if(World.Region==region){World.SetAct(region);return;}
            World.gameObject.SetActive(false);Destroy(World.gameObject);
            World=new GameObject("Coastal region "+region).AddComponent<SeaWorld>();World.Region=region;World.Build();
        }
        public void StartVoyage(bool resume=false)
        {
            ClearEncounter();
            if(resume)Run=SaveStore.Read<RunData>("voyage")??NewRun();else {Run=NewRun();SaveStore.ClearRun();Log.voyages++;SaveStore.Write("captain",Log);}
            Run.stage=Mathf.Clamp(Run.stage,1,11);Run.health=Mathf.Clamp(Run.health,1,Run.MaxHealth);if(Run.bag==null)Run.bag=new List<CatchData>();
            Rng=new System.Random(Run.seed+Run.stage*997);RollShop();SetRegion(Run.Act);Player.ResetForEncounter();
            SetState(VoyageState.Sailing);Checkpoint(false);Notice("欢迎来到松风港 · 走上码头，按住左键蓄力抛竿",5);
        }
        RunData NewRun(){int seed=Environment.TickCount&int.MaxValue;return new RunData{seed=seed,easy=Log.easy,rareSignal=new System.Random(seed).NextDouble()<.18};}
        void RollShop(){Choices=Relic.Roll(new System.Random(Run.seed+Run.stage*881));}
        public void Cast()
        {
            if(State!=VoyageState.Sailing||Paused||Player.HeldFish)return;
            var direction=Player.View.transform.forward;direction.y=0;if(direction.sqrMagnitude<.1f)direction=Player.transform.forward;direction.Normalize();
            castPoint=Player.transform.position+direction*Mathf.Lerp(13,31,CastCharge);castPoint.y=-.4f;
            if(!World.IsWater(castPoint)||World.GroundAt(castPoint)>-.3f){Notice("请面向开阔海面抛竿；走到码头前端可以抛得更远",3);return;}
            // Check the same ballistic arc that is rendered, rather than a straight line through the pier.
            Vector3 previous=Player.RodTip;
            for(int i=1;i<=20;i++){float t=i/20f;var next=Vector3.Lerp(Player.RodTip,castPoint,t)+Vector3.up*Mathf.Sin(t*Mathf.PI)*3;if(Physics.Linecast(previous,next,SeaWorld.GroundMask)){Notice("抛竿路线被遮挡，请移到开阔岸边",3);return;}previous=next;}
            castStart=Player.RodTip;
            bobber=Shape.Part("Red and white fishing float",PrimitiveType.Sphere,null,castStart,new Vector3(.19f,.25f,.19f),new Color(.95f,.31f,.14f));
            Shape.Part("Float cap",PrimitiveType.Sphere,bobber.transform,new Vector3(0,.25f,0),new Vector3(.84f,.7f,.84f),new Color(.95f,.92f,.8f));
            fishingLine=new GameObject("Fishing line").AddComponent<LineRenderer>();fishingLine.positionCount=25;fishingLine.material=Shape.Mat(new Color(.75f,.85f,.76f));fishingLine.startWidth=.014f;fishingLine.endWidth=.008f;
            ReelProgress=0;Tension=.1f;FishingAge=0;SetState(VoyageState.Fishing);Audio.Cue("cast");Player.CastAnimation();
        }
        public void TickFishing(bool holding,float dt)
        {
            if(State!=VoyageState.Fishing||Paused)return;
            float old=FishingAge;FishingAge+=dt;
            if(old<.75f&&FishingAge>=.75f){Splash(castPoint);Audio.Cue("splash");}
            if(old<1.9f&&FishBiting){Audio.Cue("bite");Notice("咬钩！稳住竿尖，绿灯收线，红灯松手",2.5f);}
            if(FishBiting) {
                Tension=Mathf.Max(0,Tension+Balance.TensionGain(holding,Surge,Run.rodLevel)*dt);
                ReelProgress=Mathf.Max(0,ReelProgress+Balance.ReelGain(holding,Surge,Run.rodLevel)*dt);
                if(Tension>=1){Notice("鱼线断了 · 不损失鱼饵，红灯时放松鱼线",3);CancelFishing();return;}
                if(FishingAge>40){Notice("鱼挣脱了 · 尝试在平稳窗口收线",3);CancelFishing();return;}
                if(ReelProgress>=1){Run.catches++;ClearFishing();BeginCombat(false);return;}
                if(holding&&Time.time>nextLineSound){nextLineSound=Time.time+.48f;Audio.Cue("reel");}
            }
            Vector3 end=castPoint;
            if(FishingAge<.75f){float t=FishingAge/.75f;end=Vector3.Lerp(castStart,castPoint,t)+Vector3.up*Mathf.Sin(t*Mathf.PI)*3;}
            else {end=Vector3.Lerp(castPoint,Player.transform.position+Player.transform.forward*5,ReelProgress*.63f);end.y=-.36f+Mathf.Sin(FishingAge*7)*.07f;end.x+=FishBiting?Mathf.Sin(FishingAge*2.8f)*(.2f+Tension*.5f):0;}
            if(bobber)bobber.transform.position=end;
            if(fishingLine)for(int i=0;i<25;i++){float f=i/24f;Vector3 p=Vector3.Lerp(Player.RodTip,end,f);p.y-=Mathf.Sin(f*Mathf.PI)*(holding?.13f:.7f);fishingLine.SetPosition(i,p);}
        }
        public void CancelFishing(){Charging=false;ClearFishing();SetState(VoyageState.Sailing);}
        void ClearFishing(){if(bobber)Destroy(bobber);if(fishingLine)Destroy(fishingLine.gameObject);}
        public void BeginCombat(bool boss=false)
        {
            if(State!=VoyageState.Sailing&&State!=VoyageState.Fishing)return;
            ClearFishing();Charging=false;SetState(VoyageState.Combat);Audio.SetCombat(true);Player.SetRod(false);
            if(boss) {
                CreatureKind kind=Run.stage==3?CreatureKind.Crab:Run.stage==6?CreatureKind.Angler:Run.stage==9?CreatureKind.Leviathan:Run.stage==10?CreatureKind.Kraken:CreatureKind.WhiteWhale;
                Spawn(kind,false,new Vector3(0,kind==CreatureKind.Kraken?.8f:.3f,37));Splash(new Vector3(0,-.35f,37));Audio.Cue("boss");Notice("海中巨物苏醒 · 留意蓄力动作，攻击后的弱点会暴露",4);
            } else {
                CreatureKind kind=(CreatureKind)Rng.Next(0,Run.Act==0?2:3);
                bool elite=Rng.NextDouble()<.1+Run.Act*.12+(Run.route==RouteKind.Hunt?.28:0);
                var e=Spawn(kind,elite,castPoint);e.LaunchToward(Player.transform.position+Player.transform.forward*3.5f);
                Notice("鱼跃出水面！凌空击杀 +25% 售价 · E 拿起鱼获",3.2f);
            }
        }
        Enemy Spawn(CreatureKind kind,bool elite,Vector3 pos)
        {var e=new GameObject(Balance.CreatureName(kind)).AddComponent<Enemy>();e.Init(this,kind,elite,pos);return e;}
        public void EnemyKilled(Enemy enemy)
        {
            if(State!=VoyageState.Combat)return;
            var item=new CatchData{kind=enemy.kind,elite=enemy.elite,quality=enemy.quality,weight=enemy.weight,airshot=enemy.Airborne,weakshot=enemy.lastWeak};
            float bonus=(item.quality==2?2.6f:item.quality==1?1.6f:1)*(item.airshot?1.25f:1)*(item.weakshot?1.15f:1);
            item.value=Mathf.RoundToInt(Balance.Bounty(enemy.kind,Run.stage,enemy.elite)*bonus*(1+Run.fortuneRelics*.15f)*(Run.route==RouteKind.Shoal?1.15f:1));
            Run.kills++;Run.health=Mathf.Min(Run.MaxHealth,Run.health+Run.leechRelics*3);Log.discovered[(int)enemy.kind]=true;Log.totalKills++;
            if(enemy.IsBoss) {
                Run.bossKills++;Run.bossCleared=true;Run.landed++;Run.coins+=item.value;Run.earned+=item.value;Run.health=Mathf.Min(Run.MaxHealth,Run.health+25);LastReward=item.value;
                if(enemy.kind==CreatureKind.Kraken){Run.krakenDefeated=true;Log.krakens++;}if(enemy.kind==CreatureKind.WhiteWhale)Run.whaleDefeated=true;
                Notice("巨物悬赏 +"+item.value+" 金币 · 航路已解锁",4);
            } else {
                var g=new GameObject("Catch: "+item.Label);g.transform.position=enemy.transform.position;var loot=g.AddComponent<FishLoot>();loot.Init(this,item,enemy.transform.rotation);Loot.Add(loot);
                Notice((item.airshot?"凌空击杀！ ":"")+"鱼获价值 "+item.value+" 金币 · E 拿起，F 收入鱼篓",4);
            }
            Enemies.Remove(enemy);Audio.Cue("catch");
            if(Enemies.Count==0){Audio.SetCombat(false);Log.bestStage=Mathf.Max(Log.bestStage,Run.stage);SaveStore.Write("captain",Log);if(enemy.IsBoss&&Run.stage>=9)EndVoyage(true);else{SetState(VoyageState.Sailing);Checkpoint(false);}}
        }
        public void RegisterCatch(FishLoot fish)
        {if(fish.Registered)return;fish.Registered=true;Run.landed++;int kind=(int)fish.Data.kind;Log.heaviest[kind]=Mathf.Max(Log.heaviest[kind],fish.Data.weight);if(fish.Data.quality==2)Log.goldDiscovered[kind]=true;SaveStore.Write("captain",Log);if(Run.RouteReady)Notice("委托完成 · 可继续钓鱼赚金币，或前往港内航图开启下一站",4);}
        public bool Stow(FishLoot fish)
        {
            if(!fish||Run.bag.Count>=12){Notice("鱼篓已满，先到鱼获收购柜台出售",3);return false;}
            RegisterCatch(fish);Run.bag.Add(fish.Data);Loot.Remove(fish);if(Player.HeldFish==fish)Player.HeldFish=null;Destroy(fish.gameObject);Checkpoint(false);Audio.Cue("ready");return true;
        }
        public int SellCatch()
        {
            if(State!=VoyageState.Sailing||FlatDistance(Player.transform.position,World.SellPoint)>4.8f)return 0;
            int total=Run.BagValue,count=Run.bag.Count;
            if(Player.HeldFish){var f=Player.HeldFish;RegisterCatch(f);total+=f.Data.value;count++;Loot.Remove(f);Player.HeldFish=null;Destroy(f.gameObject);}
            if(total==0){Notice("鱼篓还是空的。把钓到的鱼带给米罗吧。",3);return 0;}
            Run.coins+=total;Run.earned+=total;Run.sold+=count;Run.bag.Clear();Notice("米罗收下了 "+count+" 条鱼 · +"+total+" 金币",3);Audio.Cue("coin");Checkpoint(false);return total;
        }
        void UpdateInteraction()
        {
            Interaction="";interactionKind=0;targetLoot=null;if(!IsPlaying||State==VoyageState.Fishing)return;
            Vector3 p=Player.transform.position;
            if(State==VoyageState.Sailing) {
                if(FlatDistance(p,World.SellPoint)<4.8f){Interaction="E  出售鱼获   /   鱼篓 "+Run.BagValue+" 金币";interactionKind=1;return;}
                if(FlatDistance(p,World.ShopPoint)<4.8f){Interaction="E  老船长工坊   /   金币购买装备与配件";interactionKind=2;return;}
                if(FlatDistance(p,World.ChartPoint)<3.4f){Interaction=Run.RouteReady?"E  查看航图 · 下一站已解锁":"航路委托   /   "+(Run.BossStage?"击败海域首领":"收集鱼获 "+Run.landed+" / "+Run.Quota);interactionKind=3;return;}

            }
            if(Player.HeldFish){Interaction="F  收入鱼篓   /   Q  抛出鱼获   /   带到鱼市出售";return;}
            float best=3.6f;
            foreach(var f in Loot)if(f&&!f.Held){float d=Vector3.Distance(f.transform.position,p);if(d<best){best=d;targetLoot=f;}}
            if(targetLoot){interactionKind=5;Interaction="E  拿起 "+targetLoot.Data.Label+"  ·  "+targetLoot.Data.weight.ToString("F1")+" kg  ·  "+targetLoot.Data.value+" 金币";}
            else if(State==VoyageState.Sailing&&Run.BossStage&&!Run.bossCleared&&p.z>21&&Mathf.Abs(p.x)<4){Interaction="E  摇响猎潮钟 · 挑战 "+Balance.CreatureName(Run.stage==3?CreatureKind.Crab:Run.stage==6?CreatureKind.Angler:Run.stage==9?CreatureKind.Leviathan:Run.stage==10?CreatureKind.Kraken:CreatureKind.WhiteWhale);interactionKind=4;return;}
        }
        public void Interact()
        {
            UpdateInteraction();
            if(interactionKind==1)SellCatch();else if(interactionKind==2)OpenShop();else if(interactionKind==3)OpenRoute();else if(interactionKind==4)BeginCombat(true);else if(interactionKind==5)Player.PickUp(targetLoot);
        }
        public static float FlatDistance(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        public void OpenShop(){if(State!=VoyageState.Sailing||FlatDistance(Player.transform.position,World.ShopPoint)>4.8f)return;Player.StowHeld();Checkpoint(false);SetState(VoyageState.Shop);}
        public void CloseShop(){if(State!=VoyageState.Shop&&State!=VoyageState.Route)return;SetState(VoyageState.Sailing);}
        public void ChooseRelic(int i){Buy("relic"+i);}
        public bool Buy(string id)
        {
            if(State!=VoyageState.Shop)return false;
            int price=Price(id);if(price<0||Run.coins<price){Notice("金币不足 · 去码头钓鱼，交给鱼市换取金币",3);return false;}
            if(id.StartsWith("relic")){int i;if(!int.TryParse(id.Substring(5),out i)||i<0||i>2)return false;Choices[i].apply(Run);Run.shopMask|=1<<i;}
            else switch(id){case "weapon":Run.weaponLevel++;break;case "rod":Run.rodLevel++;break;case "hull":Run.hullLevel++;Run.health+=25;break;case "heal":Run.health=Mathf.Min(Run.MaxHealth,Run.health+40);break;case "shotgun":Run.shotgun=true;break;case "harpoon":Run.harpoon=true;break;case "bait":Run.abyssBait=true;break;default:return false;}
            Run.coins-=price;Player.Refill();Checkpoint(false);Notice("购买完成 · 花费 "+price+" 金币",2);Audio.Cue("coin");UI.ShowShop();return true;
        }
        public int Price(string id)
        {
            if(id.StartsWith("relic")){int i;if(!int.TryParse(id.Substring(5),out i)||i<0||i>2||Choices==null||(Run.shopMask&(1<<i))!=0)return -1;return 80+Run.Act*30;}
            switch(id){case "weapon":return Run.weaponLevel>=5?-1:Balance.UpgradePrice(85,Run.weaponLevel);case "rod":return Run.rodLevel>=3?-1:Balance.UpgradePrice(65,Run.rodLevel);case "hull":return Run.hullLevel>=3?-1:Balance.UpgradePrice(90,Run.hullLevel);case "heal":return Run.health>=Run.MaxHealth?-1:25;case "shotgun":return Run.shotgun?-1:150;case "harpoon":return Run.harpoon?-1:260;case "bait":return Run.abyssBait?-1:180;default:return -1;}
        }
        public void OpenRoute(){if((State!=VoyageState.Sailing&&State!=VoyageState.Shop)||!Run.RouteReady){Notice("先完成本海域委托，再来规划航线",3);return;}SetState(VoyageState.Route);}
        public void ChooseRoute(RouteKind route)
        {
            if(State!=VoyageState.Route)return;
            Player.StowHeld();Run.route=route;Run.stage++;Run.landed=0;Run.bossCleared=false;Run.shopMask=0;
            if(route==RouteKind.Abyss){Run.rareSignal|=Rng.NextDouble()<.2;Run.health=Mathf.Min(Run.MaxHealth,Run.health+12);}
            ClearEncounter();RollShop();SetRegion(Run.Act);Player.ResetForEncounter();Checkpoint(false);SetState(VoyageState.Sailing);
            Notice(Run.BossStage?"巨物海域 · 准备好后，在码头尽头按 E 摇响猎潮钟":"抵达新海域 · 鱼群与工坊配件已刷新",4);
        }
        public void ChallengeKraken(){ChallengeRare(10);}
        public void ChallengeWhiteWhale(){ChallengeRare(11);}
        void ChallengeRare(int stage)
        {
            if(State!=VoyageState.Victory||Run.stage!=9||(stage==10?!(Run.abyssBait||Run.rareSignal):!Run.rareSignal))return;
            Run.stage=stage;Run.bossCleared=false;Run.landed=0;Run.health=Mathf.Min(Run.MaxHealth,Run.health+50);ClearEncounter();SetRegion(2);Player.ResetForEncounter();Checkpoint(false);SetState(VoyageState.Sailing);Notice("稀有巨物潜伏海湾 · 在码头尽头按 E 摇响猎潮钟",5);
        }
        public void EndVoyage(bool victory)
        {
            if(State==VoyageState.Defeat||State==VoyageState.Victory)return;
            ClearHazards();ClearFishing();Audio.SetCombat(false);Paused=false;Time.timeScale=1;if(victory&&Run.stage==9)Log.victories++;
            SaveStore.ClearRun();SaveStore.Write("captain",Log);SetState(victory?VoyageState.Victory:VoyageState.Defeat);Audio.Cue(victory?"win":"lose");
        }
        public void ReturnHarbor(){if(IsPlaying||State==VoyageState.Shop||State==VoyageState.Route){Player.StowHeld();Checkpoint(false);}Paused=false;Time.timeScale=1;ClearEncounter();SetRegion(0);SetState(VoyageState.Harbor);}
        public void Checkpoint(bool between){Run.betweenEncounters=false;SaveStore.Write("voyage",Run);}
        public void TogglePause(){Paused=!Paused;Time.timeScale=Paused?0:1;Cursor.lockState=Paused?CursorLockMode.None:(IsPlaying&&!Automation?CursorLockMode.Locked:CursorLockMode.None);Cursor.visible=Paused||!IsPlaying||Automation;if(Paused)UI.ShowPause();else UI.ShowState();}
        public void SetState(VoyageState state){State=state;Cursor.lockState=IsPlaying&&!Automation?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=!IsPlaying||Automation;UI.ShowState();}
        public void Notice(string text,float seconds){Message=text;MessageUntil=Time.unscaledTime+seconds;}
        public void Warn(Vector3 pos,float radius,float delay,float damage){pos.y=World.GroundAt(pos)+.035f;var g=new GameObject("Telegraphed strike");g.transform.SetParent(Hazards);g.transform.position=pos;var w=g.AddComponent<DeckWarning>();w.game=this;w.radius=radius;w.delay=delay;w.damage=damage;w.Init();}
        public void Projectile(Vector3 from,Vector3 to,float speed,float damage,Color color){var g=Shape.Part("Hostile projectile",PrimitiveType.Sphere,Hazards,from,Vector3.one*.4f,color,false,true);var p=g.AddComponent<SeaProjectile>();p.game=this;p.velocity=(to-from).normalized*speed;p.damage=damage;}
        public void Splash(Vector3 point){Effect(point,new Color(.66f,.87f,.83f),20,.1f);var r=new GameObject("Water ripple").AddComponent<SurfaceRipple>();r.transform.position=new Vector3(point.x,-.35f,point.z);}
        public void Effect(Vector3 point,Color color,int count,float size){for(int i=0;i<count;i++){var g=Shape.Part("Spray",PrimitiveType.Cube,Hazards,point,Vector3.one*size,color);var f=g.AddComponent<Fleck>();f.velocity=UnityEngine.Random.insideUnitSphere*3+Vector3.up*1.5f;f.life=UnityEngine.Random.Range(.3f,.7f);}}
        public void Tracer(Vector3 a,Vector3 b,Color color){var g=new GameObject("Tracer");g.transform.SetParent(Hazards);var l=g.AddComponent<LineRenderer>();l.positionCount=2;l.SetPosition(0,a);l.SetPosition(1,b);l.startWidth=.016f;l.endWidth=.006f;l.material=Shape.Mat(color,true);Destroy(g,.07f);}
        void ClearHazards(){if(Hazards)for(int i=Hazards.childCount-1;i>=0;i--)Destroy(Hazards.GetChild(i).gameObject);}
        void ClearEncounter(){Charging=false;ClearFishing();foreach(var e in Enemies)if(e)Destroy(e.gameObject);Enemies.Clear();foreach(var f in Loot)if(f)Destroy(f.gameObject);Loot.Clear();if(Player)Player.HeldFish=null;ClearHazards();}
        void OnApplicationQuit(){Time.timeScale=1;if(Player&&(IsPlaying||State==VoyageState.Shop||State==VoyageState.Route)){Player.StowHeld();Checkpoint(false);}SaveStore.Write("captain",Log);}
        public void Quit(){SaveStore.Write("captain",Log);Application.Quit();}
    }
}

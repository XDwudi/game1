using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tidebreak
{
    public partial class GameDirector : MonoBehaviour
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
            EnsureExpedition();AudioListener.volume=Log.volume;Rng=new System.Random();
            World=new GameObject("Coastal world").AddComponent<SeaWorld>();World.Build();
            Hazards=new GameObject("Encounter effects").transform;
            Audio=gameObject.AddComponent<SeaAudio>();Audio.Init();
            Player=new GameObject("Captain").AddComponent<AnglerController>();Player.Init(this);
            UI=gameObject.AddComponent<SeaHUD>();UI.Init(this);SetState(VoyageState.Harbor);
            if(Automation)gameObject.AddComponent<SmokePilot>();
        }
        void Update()
        {
            if(CinematicActive)return;
            if(Input.GetKeyDown(KeyCode.Escape)) {
                if(State==VoyageState.Harbor)UI.ShowHarbor();
                else if(State==VoyageState.Dialogue)CloseDialogue();
                else if(State==VoyageState.Shop||State==VoyageState.Route)CloseShop();
                else if(State!=VoyageState.Victory&&State!=VoyageState.Defeat)TogglePause();
            }
            if(Paused)return;
            if(State==VoyageState.Dialogue)return;
            TickIslandMission();
            if(IsPlaying&&Input.GetKeyDown(KeyCode.Tab)){TogglePause();UI.ShowBag();return;}
            if(State==VoyageState.Harbor) {
                float a=Time.unscaledTime*.012f;
                Player.Teleport(new Vector3(28+Mathf.Sin(a)*2,14,28));Player.transform.LookAt(new Vector3(0,1,-7));Player.View.transform.localRotation=Quaternion.identity;return;
            }
            if(IsPlaying)Run.elapsed+=Time.deltaTime;
            if(!Automation&&IsPlaying){
                if(Input.GetKeyDown(KeyCode.B)&&State==VoyageState.Sailing)CycleLure();
                if(Input.GetKeyDown(KeyCode.M)&&State==VoyageState.Sailing)ShowMap();
                if(Input.GetKeyDown(KeyCode.I)){TogglePause();UI.ShowJournal();}
                if(Input.GetKeyDown(KeyCode.J)){TogglePause();UI.ShowStory(false);}
                if(Input.GetKeyDown(KeyCode.Z))UseUtility("medkit");if(Input.GetKeyDown(KeyCode.X))UseUtility("bomb");if(Input.GetKeyDown(KeyCode.V))UseUtility("frost");if(Input.GetKeyDown(KeyCode.C))UseUtility("sonar");if(Input.GetKeyDown(KeyCode.G))UseUtility("tonic");
            }
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
            if(World.Region==Mathf.Clamp(region,0,8)){World.SetAct(Mathf.Clamp(region,0,8));return;}
            World.gameObject.SetActive(false);Destroy(World.gameObject);
            World=new GameObject("Coastal region "+region).AddComponent<SeaWorld>();World.Region=region;World.Build();
        }
        public void StartVoyage(bool resume=false)
        {
            CancelCinematic();
            ClearEncounter();SonarUntil=TonicUntil=0;Chummed=false;
            if(resume)Run=SaveStore.Read<RunData>("voyage")??NewRun();else {Run=NewRun();SaveStore.ClearRun();Log.voyages++;SaveStore.Write("captain",Log);}
            Run.stage=Mathf.Clamp(Run.stage,1,11);Run.health=Mathf.Clamp(Run.health,1,Run.MaxHealth);if(Run.bag==null)Run.bag=new List<CatchData>();
            EnsureExpedition();PendingSite=-1;Rng=new System.Random(Run.seed+Run.stage*997);RollShop();SetRegion(Mathf.Min(Run.stage-1,8));Player.ResetForEncounter();
            SetState(VoyageState.Sailing);Checkpoint(false);Notice("抵达 "+Island.name+" · "+QuestObjective,5);
            if(resume&&Run.endingPending)PlayCinematic(12,()=>EndVoyage(true),true);
            else if(!resume)PlayCinematic(0);
        }
        RunData NewRun(){int seed=Environment.TickCount&int.MaxValue;return new RunData{seed=seed,easy=Log.easy,rareSignal=new System.Random(seed).NextDouble()<.18};}
        void RollShop(){int workshop=Mathf.Min(9,Run.stage);Choices=Relic.Roll(new System.Random(Run.seed+workshop*881),Mathf.Min(20,5+workshop*2));}
        public void Cast()
        {
            if(State!=VoyageState.Sailing||Paused||Player.HeldFish)return;
            var direction=Player.View.transform.forward;direction.y=0;if(direction.sqrMagnitude<.1f)direction=Player.transform.forward;direction.Normalize();
            castPoint=Player.transform.position+direction*Mathf.Lerp(13,31,CastCharge);castPoint.y=-.4f;
            if(!World.IsWater(castPoint)||World.GroundAt(castPoint)>-.3f){Notice("请面向开阔海面抛竿；走到码头前端可以抛得更远",3);return;}
            // Check the same ballistic arc that is rendered, rather than a straight line through the pier.
            Vector3 previous=Player.RodTip;
            for(int i=1;i<=20;i++){float t=i/20f;var next=Vector3.Lerp(Player.RodTip,castPoint,t)+Vector3.up*Mathf.Sin(t*Mathf.PI)*3;if(Physics.Linecast(previous,next,SeaWorld.GroundMask)){Notice("抛竿路线被遮挡，请移到开阔岸边",3);return;}previous=next;}
            castStart=Player.RodTip;Chummed=Run.chum>0;if(Chummed)Run.chum--;
            bobber=Shape.Part("Red and white fishing float",PrimitiveType.Sphere,null,castStart,new Vector3(.19f,.25f,.19f),new Color(.95f,.31f,.14f));
            Shape.Part("Float cap",PrimitiveType.Sphere,bobber.transform,new Vector3(0,.25f,0),new Vector3(.84f,.7f,.84f),new Color(.95f,.92f,.8f));
            fishingLine=new GameObject("Fishing line").AddComponent<LineRenderer>();fishingLine.positionCount=25;fishingLine.material=Shape.Mat(new Color(.75f,.85f,.76f));fishingLine.startWidth=.014f;fishingLine.endWidth=.008f;
            ReelProgress=0;Tension=.1f;FishingAge=Chummed?.65f:0;SetState(VoyageState.Fishing);Audio.Cue("cast");Player.CastAnimation();
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
            if(boss&&Run.stage<=9&&(Run.questStep<3||Run.bossCleared)){Notice("先完成向导的调查，取得首领线索",3);return;}
            if(boss&&Run.stage>=10&&!Automation&&(Run.cinematicMask&(1<<Run.stage))==0){PlayCinematic(Run.stage,()=>BeginCombat(true));return;}
            ClearFishing();Charging=false;SetState(VoyageState.Combat);Audio.SetCombat(true);Player.SetRod(false);
            if(boss){var spec=ExpeditionContent.Species[Run.stage<=9?107+Run.stage:Run.stage==10?117:118];SpawnSpecies(spec,false,new Vector3(0,1.1f,38));Splash(new Vector3(0,-.35f,38));Audio.Cue("boss");Notice(spec.name+" 苏醒 · 留意专属蓄势预警",4);}
            else {var spec=ExpeditionContent.Roll(Run,Rng);bool elite=Rng.NextDouble()<.08+Run.stage*.018+(Run.route==RouteKind.Hunt?.18:0);var e=SpawnSpecies(spec,elite,castPoint);e.LaunchToward(Player.transform.position+Player.transform.forward*3.5f);Notice(spec.name+" 出水！"+ExpeditionContent.Counters[(int)spec.attack],4);}
        }
        Enemy Spawn(CreatureKind kind,bool elite,Vector3 pos)
        {var e=new GameObject(Balance.CreatureName(kind)).AddComponent<Enemy>();e.Init(this,kind,elite,pos);return e;}
        public void EnemyKilled(Enemy enemy)
        {
            if(State!=VoyageState.Combat)return;
            var item=new CatchData{kind=enemy.kind,speciesId=enemy.Spec.id,elite=enemy.elite,quality=enemy.quality,weight=enemy.weight,airshot=enemy.Airborne,weakshot=enemy.lastWeak};
            float bonus=(item.quality==2?2.6f:item.quality==1?1.6f:1)*(item.airshot?1.25f:1)*(item.weakshot?1.15f:1);
            item.value=Mathf.RoundToInt(enemy.Spec.value*(enemy.elite?1.65f:1)*bonus*(1+Run.fortuneRelics*.15f)*(Run.route==RouteKind.Shoal?1.15f:1));
            Run.kills++;Run.health=Mathf.Min(Run.MaxHealth,Run.health+Run.leechRelics*3);Log.totalKills++;
            bool first=!Log.speciesSeen[enemy.Spec.id];Log.speciesSeen[enemy.Spec.id]=true;Log.speciesKills[enemy.Spec.id]++;
            if(enemy.IsBoss){Run.bossKills++;Run.bossCleared=true;Run.bossTrophy=true;Run.coins+=item.value;Run.earned+=item.value;Run.health=Mathf.Min(Run.MaxHealth,Run.health+25);if(Run.stage==10){Run.krakenDefeated=true;Log.krakens++;}if(Run.stage==11)Run.whaleDefeated=true;Notice("获得潮核与悬赏 +"+item.value+" 金币 · 回向导处交付潮核",5);}
            else if(!enemy.Summoned){var g=new GameObject("Catch: "+item.Label);g.transform.position=enemy.transform.position;var loot=g.AddComponent<FishLoot>();loot.Init(this,item,enemy.transform.rotation);Loot.Add(loot);Notice((first?"新物种已收录！ ":"")+item.Label+" · "+item.value+" 金币 · E 拿起，F 收纳",4);}
            Enemies.Remove(enemy);Audio.Cue(first?"discovery":"catch");
            if(Enemies.Count==0){if(enemy.Spec.trait==SeaTrait.Volatile)StartCoroutine(FinishVolatileEncounter());else FinishEncounter();}
        }
        System.Collections.IEnumerator FinishVolatileEncounter(){yield return new WaitForSeconds(1.25f);if(State==VoyageState.Combat&&Enemies.Count==0)FinishEncounter();}
        void FinishEncounter(){if(ResolveMissionEncounter())return;Audio.SetCombat(false);Log.bestStage=Mathf.Max(Log.bestStage,Run.stage);SaveStore.Write("captain",Log);if(Run.bossCleared&&Run.stage>=10)EndVoyage(true);else {SetState(VoyageState.Sailing);if(PendingSite>=0)CompleteSite(PendingSite);Checkpoint(false);}}
        public void RegisterCatch(FishLoot fish)
        {
            if(fish.Registered)return;fish.Registered=true;Run.landed++;int id=fish.Data.speciesId;
            if(id>=0&&id<119){if(!Run.islandCaught.Contains(id))Run.islandCaught.Add(id);Log.speciesWeight[id]=Mathf.Max(Log.speciesWeight[id],fish.Data.weight);if(fish.Data.quality==2)Log.speciesGold[id]=true;}
            SaveStore.Write("captain",Log);if(Run.questStep==1&&Run.landed>=1)Notice("第一份鱼获已到手 · 出售可赚取金币，接着取电池修复灯塔",4);
        }
        public bool Stow(FishLoot fish)
        {
            if(!fish||Run.bag.Count>=Run.BagCapacity){Notice("鱼篓已满，先到鱼获收购柜台出售",3);return false;}
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
            string missionInteraction;if(TryMissionInteraction(out missionInteraction)){Interaction=missionInteraction;interactionKind=20;return;}
            Vector3 p=Player.transform.position;
            if(State==VoyageState.Sailing) {
                if(Run.stage<=9&&FlatDistance(p,World.QuestPoint)<4){Interaction="E  与 "+Island.npc+" 交谈 · "+Island.title;interactionKind=6;return;}
                for(int i=0;i<3&&Run.stage<=9;i++)if(FlatDistance(p,SitePoint(i))<3.5f&&(Run.exploredMask&(1<<i))==0){Interaction="E  探索 "+(i==0?Island.siteA:i==1?Island.siteB:Island.secret);interactionKind=7+i;return;}
                if(FlatDistance(p,World.SellPoint)<4.8f){Interaction="E  出售鱼获   /   鱼篓 "+Run.BagValue+" 金币";interactionKind=1;return;}
                if(FlatDistance(p,World.ShopPoint)<4.8f){Interaction="E  老船长工坊   /   金币购买装备与配件";interactionKind=2;return;}
                if(FlatDistance(p,World.ChartPoint)<3.4f){Interaction="E  查看群岛航图 · 已解锁 "+Run.maxIsland+" / 9";interactionKind=3;return;}

            }
            if(Player.HeldFish){Interaction="F  收入鱼篓   /   Q  抛出鱼获   /   带到鱼市出售";return;}
            float best=3.6f;
            foreach(var f in Loot)if(f&&!f.Held){float d=Vector3.Distance(f.transform.position,p);if(d<best){best=d;targetLoot=f;}}
            if(targetLoot){interactionKind=5;Interaction="E  拿起 "+targetLoot.Data.Label+"  ·  "+targetLoot.Data.weight.ToString("F1")+" kg  ·  "+targetLoot.Data.value+" 金币";}
            else if(State==VoyageState.Sailing&&Run.BossStage&&!Run.bossCleared&&p.z>21&&Mathf.Abs(p.x)<4){Interaction="E  摇响猎潮钟 · 挑战 "+ExpeditionContent.Species[Run.stage<=9?107+Run.stage:Run.stage==10?117:118].name;interactionKind=4;return;}
        }
        public void Interact()
        {
            UpdateInteraction();
            if(interactionKind==20)UseMissionAction();else if(interactionKind==1)SellCatch();else if(interactionKind==2)OpenShop();else if(interactionKind==3)OpenRoute();else if(interactionKind==4)BeginCombat(true);else if(interactionKind==5)Player.PickUp(targetLoot);else if(interactionKind==6)TalkGuide();else if(interactionKind>=7&&interactionKind<=9)UseSite(interactionKind-7);
        }
        public static float FlatDistance(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        public void OpenShop(){if(State!=VoyageState.Sailing||FlatDistance(Player.transform.position,World.ShopPoint)>4.8f)return;Player.StowHeld();Checkpoint(false);SetState(VoyageState.Shop);}
        public void CloseShop(){if(State!=VoyageState.Shop&&State!=VoyageState.Route)return;SetState(VoyageState.Sailing);}
        public void ChooseRelic(int i){Buy("relic"+i);}
        public bool Buy(string id)
        {
            if(State!=VoyageState.Shop)return false;int price=Price(id);if(price<0||Run.coins<price)return false;
            if(id.StartsWith("relic")){int i;if(!int.TryParse(id.Substring(5),out i)||i<0||i>2)return false;Choices[i].apply(Run);Run.shopMask|=1<<i;}
            else {var item=ShopCatalog.Find(id);if(item==null||ShopCatalog.Lock(Run,item)!="")return false;item.apply(Run);}
            Run.coins-=price;Player.Refill();Checkpoint(false);Audio.Cue("coin");UI.ShowShop();return true;
        }
        public int Price(string id)
        {
            if(id.StartsWith("relic")){int i;if(!int.TryParse(id.Substring(5),out i)||i<0||i>2||Choices==null||(Run.shopMask&(1<<i))!=0||Choices[i].AtCap(Run))return -1;return 80+Run.Act*30;}
            var item=ShopCatalog.Find(id);if(item==null||ShopCatalog.Lock(Run,item)!="")return -1;return item.Price(Run);
        }
        public void OpenRoute(){if(State!=VoyageState.Sailing&&State!=VoyageState.Shop)return;SetState(VoyageState.Route);}
        public void ChooseRoute(RouteKind route){Travel(Mathf.Min(9,Run.stage+1),route);}
        public void ChallengeKraken(){ChallengeRare(10);}
        public void ChallengeWhiteWhale(){ChallengeRare(11);}
        void ChallengeRare(int stage)
        {
            if((State!=VoyageState.Victory&&State!=VoyageState.Route)||!StoryComplete||(stage==10?!(Run.abyssBait||Run.rareSignal):!Run.rareSignal))return;
            StoreIsland();RestoreIslandProgress(9);
            Run.stage=stage;Run.bossCleared=false;Run.bossTrophy=false;Run.questStep=3;Run.landed=0;Run.health=Mathf.Min(Run.MaxHealth,Run.health+50);ClearEncounter();RollShop();SetRegion(8);Player.ResetForEncounter();Checkpoint(false);SetState(VoyageState.Sailing);Notice("稀有巨物潜伏海湾 · 在码头尽头按 E 摇响猎潮钟\n补给工坊与镜渊神庙共享随机库存",5);
        }
        public void EndVoyage(bool victory)
        {
            if(State==VoyageState.Defeat||State==VoyageState.Victory)return;
            ClearHazards();ClearFishing();Audio.SetCombat(false);Paused=false;Time.timeScale=1;if(victory&&Run.stage==9){Log.victories++;Run.endingPending=false;}
            if(victory)Checkpoint(false);else SaveStore.ClearRun();SaveStore.Write("captain",Log);SetState(victory?VoyageState.Victory:VoyageState.Defeat);Audio.Cue(victory?"win":"lose");
        }
        public void ReturnHarbor(){CancelCinematic();if(IsPlaying||State==VoyageState.Shop||State==VoyageState.Route||State==VoyageState.Dialogue){Player.StowHeld();Checkpoint(false);}Paused=false;Time.timeScale=1;ClearEncounter();SetRegion(0);SetState(VoyageState.Harbor);}
        public void Checkpoint(bool between){StoreIsland();Run.betweenEncounters=false;SaveStore.Write("voyage",Run);}
        public void TogglePause(){Paused=!Paused;Time.timeScale=Paused?0:1;Cursor.lockState=Paused?CursorLockMode.None:(IsPlaying&&!Automation?CursorLockMode.Locked:CursorLockMode.None);Cursor.visible=Paused||!IsPlaying||Automation;if(Paused)UI.ShowPause();else UI.ShowState();}
        public void SetState(VoyageState state){State=state;Cursor.lockState=IsPlaying&&!Automation?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=!IsPlaying||Automation;UI.ShowState();}
        public void Notice(string text,float seconds){Message=text;MessageUntil=Time.unscaledTime+seconds;}
        public void Warn(Vector3 pos,float radius,float delay,float damage){pos.y=World.GroundAt(pos)+.035f;var g=new GameObject("Telegraphed strike");g.transform.SetParent(Hazards);g.transform.position=pos;var w=g.AddComponent<DeckWarning>();w.game=this;w.radius=radius;w.delay=delay;w.damage=damage;w.Init();}
        public void Projectile(Vector3 from,Vector3 to,float speed,float damage,Color color){var g=Shape.Part("Hostile projectile",PrimitiveType.Sphere,Hazards,from,Vector3.one*.4f,color,false,true);var p=g.AddComponent<SeaProjectile>();p.game=this;p.velocity=(to-from).normalized*speed;p.damage=damage;}
        public void Splash(Vector3 point){Effect(point,new Color(.66f,.87f,.83f),20,.1f);var r=new GameObject("Water ripple").AddComponent<SurfaceRipple>();r.transform.position=new Vector3(point.x,-.35f,point.z);}
        public void Effect(Vector3 point,Color color,int count,float size){for(int i=0;i<count;i++){var g=Shape.Part("Spray",PrimitiveType.Cube,Hazards,point,Vector3.one*size,color);var f=g.AddComponent<Fleck>();f.velocity=UnityEngine.Random.insideUnitSphere*3+Vector3.up*1.5f;f.life=UnityEngine.Random.Range(.3f,.7f);}}
        public void Tracer(Vector3 a,Vector3 b,Color color){var g=new GameObject("Tracer");g.transform.SetParent(Hazards);var l=g.AddComponent<LineRenderer>();l.positionCount=2;l.SetPosition(0,a);l.SetPosition(1,b);l.startWidth=.016f;l.endWidth=.006f;l.material=Shape.Mat(color,true);Destroy(g,.07f);}
        void ClearHazards(){if(Hazards)for(int i=Hazards.childCount-1;i>=0;i--)Destroy(Hazards.GetChild(i).gameObject);}
        void ClearEncounter(){ResetIslandMission();Charging=false;ClearFishing();foreach(var e in Enemies)if(e)Destroy(e.gameObject);Enemies.Clear();foreach(var f in Loot)if(f)Destroy(f.gameObject);Loot.Clear();if(Player)Player.HeldFish=null;ClearHazards();}
        void OnApplicationQuit(){CancelCinematic();Time.timeScale=1;if(Player&&(IsPlaying||State==VoyageState.Shop||State==VoyageState.Route||State==VoyageState.Dialogue)){Player.StowHeld();Checkpoint(false);}SaveStore.Write("captain",Log);}
        public void Quit(){SaveStore.Write("captain",Log);Application.Quit();}
    }
}

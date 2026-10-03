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
        public bool Paused, Automation;
        public System.Random Rng;
        public SeaWorld World;
        public AnglerController Player;
        public SeaHUD UI;
        public SeaAudio Audio;
        public readonly List<Enemy> Enemies=new List<Enemy>();
        public Relic[] Choices;
        public string Message="";
        public float MessageUntil;
        public float ReelProgress,Tension,FishingAge;
        public bool Surge { get { return Mathf.Sin((FishingAge-1.5f)*2)> .28f; } }
        public bool FishBiting { get { return FishingAge>1.5f; } }
        public int LastReward;
        public Transform Hazards;
        public bool IsPlaying { get { return State==VoyageState.Sailing||State==VoyageState.Fishing||State==VoyageState.Combat; } }
        LineRenderer fishingLine;
        GameObject bobber;
        Vector3 castPoint;
        float nextLineSound;
        void Awake()
        {
            Instance=this;
            Automation=Array.IndexOf(Environment.GetCommandLineArgs(),"-tidebreakSmoke")>=0;
            if(Automation)SaveStore.DirectoryOverride=System.IO.Path.Combine(Application.temporaryCachePath,"TidebreakSmoke");
            Application.targetFrameRate=90;QualitySettings.vSyncCount=0;
            Log=SaveStore.Read<CaptainLog>("captain")??new CaptainLog();
            if(Log.discovered==null)Log.discovered=new bool[8];
            else if(Log.discovered.Length!=8)Array.Resize(ref Log.discovered,8);
            AudioListener.volume=Log.volume;
            Rng=new System.Random();
            World=new GameObject("Ocean world").AddComponent<SeaWorld>();World.Build();
            Hazards=new GameObject("Encounter effects").transform;
            Audio=gameObject.AddComponent<SeaAudio>();Audio.Init();
            Player=new GameObject("Captain").AddComponent<AnglerController>();Player.Init(this);
            UI=gameObject.AddComponent<SeaHUD>();UI.Init(this);
            SetState(VoyageState.Harbor);
            if(Automation)gameObject.AddComponent<SmokePilot>();
        }
        void Update()
        {
            if(Input.GetKeyDown(KeyCode.Escape)) {
                if(State==VoyageState.Harbor)UI.ShowHarbor();
                else if(State!=VoyageState.Victory&&State!=VoyageState.Defeat)TogglePause();
            }
            if(Paused)return;
            if(State==VoyageState.Harbor) {
                float a=Time.unscaledTime*.016f;
                Player.transform.position=new Vector3(18+Mathf.Sin(a)*3,10,-18+Mathf.Cos(a)*2);
                Player.transform.LookAt(new Vector3(-7,1.3f,1));Player.View.transform.localRotation=Quaternion.identity;
                return;
            }
            if(IsPlaying)Run.elapsed+=Time.deltaTime;
            if(State==VoyageState.Sailing&&Input.GetMouseButtonDown(0))Cast();
            if(State==VoyageState.Fishing&&!Automation)TickFishing(Input.GetMouseButton(0),Time.deltaTime);
            if(State==VoyageState.Fishing&&Input.GetKeyDown(KeyCode.Q))CancelFishing();
        }
        public void StartVoyage(bool resume=false)
        {
            ClearEncounter();
            if(resume)Run=SaveStore.Read<RunData>("voyage")??NewRun();else {Run=NewRun();SaveStore.ClearRun();Log.voyages++;SaveStore.Write("captain",Log);}
            Run.stage=Mathf.Clamp(Run.stage,1,11);Run.health=Mathf.Clamp(Run.health,1,Run.MaxHealth);
            Rng=new System.Random(Run.seed+Run.stage*997);
            World.SetAct(Run.Act);Player.ResetForEncounter();
            if(resume && Run.betweenEncounters)SetState(VoyageState.Shop);else {Checkpoint(false);SetState(VoyageState.Sailing);Notice("远征开始 · 面向海面，左键抛竿",4);}
        }
        RunData NewRun(){int seed=Environment.TickCount&int.MaxValue;return new RunData{seed=seed,easy=Log.easy,rareSignal=new System.Random(seed).NextDouble()<.18};}
        public void Cast()
        {
            if(State!=VoyageState.Sailing||Paused)return;
            var direction=Player.transform.forward;
            castPoint=Player.transform.position+direction*18;castPoint.y=-.25f;
            bobber=Shape.Part("Fishing float",PrimitiveType.Sphere,null,castPoint,new Vector3(.23f,.36f,.23f),new Color(1,.48f,.21f),false,true);
            fishingLine=new GameObject("Fishing line").AddComponent<LineRenderer>();fishingLine.positionCount=18;
            fishingLine.material=Shape.Mat(new Color(.82f,.92f,.83f));fishingLine.startWidth=fishingLine.endWidth=.012f;
            ReelProgress=0;Tension=.15f;FishingAge=0;SetState(VoyageState.Fishing);Audio.Cue("cast");
        }
        public void TickFishing(bool holding,float dt)
        {
            if(State!=VoyageState.Fishing||Paused)return;
            float old=FishingAge;FishingAge+=dt;
            if(old<1.5f&&FishBiting){Audio.Cue("bite");Notice("咬钩了！绿灯收线，红灯松开",2.7f);}
            if(FishBiting) {
                Tension=Mathf.Max(0,Tension+Balance.TensionGain(holding,Surge,Run.rodLevel)*dt);
                ReelProgress=Mathf.Max(0,ReelProgress+Balance.ReelGain(holding,Surge,Run.rodLevel)*dt*(Run.BossStage?.82f:1));
                if(Tension>=1){Notice("鱼线断裂 · 红灯时松开左键，再试一次",4);CancelFishing();return;}
                if(FishingAge>40){Notice("鱼儿逃脱 · 及时收线可防止脱钩",3);CancelFishing();return;}
                if(ReelProgress>=1){Run.catches++;ClearFishing();BeginCombat();return;}
                if(holding&&Time.time>nextLineSound){nextLineSound=Time.time+.4f;Audio.Cue("ready");}
            }
            if(bobber)bobber.transform.position=castPoint+Vector3.up*Mathf.Sin(FishingAge*7)*.12f;
            if(fishingLine) {
                Vector3 start=Player.View.transform.TransformPoint(new Vector3(.55f,.65f,2.1f));
                for(int i=0;i<18;i++){float f=i/17f;Vector3 p=Vector3.Lerp(start,castPoint,f);p.y-=Mathf.Sin(f*Mathf.PI)*.65f;fishingLine.SetPosition(i,p);}
            }
        }
        public void CancelFishing(){ClearFishing();SetState(VoyageState.Sailing);}
        void ClearFishing(){if(bobber)Destroy(bobber);if(fishingLine)Destroy(fishingLine.gameObject);}
        public void BeginCombat()
        {
            ClearFishing();Player.ResetForEncounter();SetState(VoyageState.Combat);
            Audio.SetCombat(true);Notice(Run.BossStage?"巨物苏醒 · 瞄准发光弱点":"怪鱼上钩 · 射击！注意敌方弹幕",3);
            if(Run.BossStage) {
                CreatureKind kind=Run.stage==3?CreatureKind.Crab:Run.stage==6?CreatureKind.Angler:Run.stage==9?CreatureKind.Leviathan:Run.stage==10?CreatureKind.Kraken:CreatureKind.WhiteWhale;
                Spawn(kind,false,new Vector3(0,kind==CreatureKind.Kraken?3.6f:2.6f,kind==CreatureKind.Kraken?17:14));Audio.Cue("boss");
            } else {
                int n=Balance.EnemyCount(Run.stage);
                for(int i=0;i<n;i++) {
                    CreatureKind kind=(CreatureKind)((i+Run.Act)%3);
                    bool elite=(Run.stage%3==2&&i==0)||Run.route==RouteKind.Hunt&&i==n-1;
                    Spawn(kind,elite,new Vector3((i-(n-1)*.5f)*3.6f,2.5f+(i%2)*1.1f,10.5f+(i%2)*4));
                }
            }
        }
        Enemy Spawn(CreatureKind kind,bool elite,Vector3 pos)
        {var e=new GameObject(Balance.CreatureName(kind)).AddComponent<Enemy>();e.Init(this,kind,elite,pos);return e;}
        public void EnemyKilled(Enemy enemy)
        {
            if(State!=VoyageState.Combat)return;
            int bounty=Mathf.RoundToInt(Balance.Bounty(enemy.kind,Run.stage,enemy.elite)*(1+Run.fortuneRelics*.15f)*(Run.route==RouteKind.Shoal?1.15f:1));
            Run.coins+=bounty;Run.earned+=bounty;Run.kills++;Run.health=Mathf.Min(Run.MaxHealth,Run.health+Run.leechRelics*3);
            Log.discovered[(int)enemy.kind]=true;Log.totalKills++;
            if(enemy.IsBoss)Run.bossKills++;
            if(enemy.kind==CreatureKind.Kraken){Run.krakenDefeated=true;Log.krakens++;}
            if(enemy.kind==CreatureKind.WhiteWhale)Run.whaleDefeated=true;
            Enemies.Remove(enemy);Audio.Cue("coin");
            if(Enemies.Count==0)CompleteEncounter();
        }
        void CompleteEncounter()
        {
            ClearHazards();Audio.SetCombat(false);
            LastReward=20+Run.stage*3;Run.coins+=LastReward;Run.earned+=LastReward;
            Log.bestStage=Mathf.Max(Log.bestStage,Run.stage);SaveStore.Write("captain",Log);
            if(Run.BossStage)Run.health=Mathf.Min(Run.MaxHealth,Run.health+25);
            if(Run.stage>=9){EndVoyage(true);return;}
            Choices=Relic.Roll(Rng);SetState(VoyageState.Reward);Audio.Cue("win");
        }
        public void ChooseRelic(int i)
        {
            if(State!=VoyageState.Reward||Choices==null||i<0||i>=Choices.Length)return;
            Choices[i].apply(Run);Choices=null;Player.Refill();Checkpoint(true);SetState(VoyageState.Shop);Audio.Cue("select");
        }
        public bool Buy(string id)
        {
            if(State!=VoyageState.Shop)return false;
            int price=Price(id);if(price<0||Run.coins<price){Notice("金币不足，继续垂钓积累战利品",2);Audio.Cue("hurt");return false;}
            switch(id) {
                case "weapon":if(Run.weaponLevel>=5)return false;Run.weaponLevel++;break;
                case "rod":if(Run.rodLevel>=3)return false;Run.rodLevel++;break;
                case "hull":if(Run.hullLevel>=3)return false;Run.hullLevel++;Run.health+=25;break;
                case "heal":if(Run.health>=Run.MaxHealth)return false;Run.health=Mathf.Min(Run.MaxHealth,Run.health+40);break;
                case "shotgun":if(Run.shotgun)return false;Run.shotgun=true;break;
                case "harpoon":if(Run.harpoon)return false;Run.harpoon=true;break;
                case "bait":if(Run.abyssBait)return false;Run.abyssBait=true;break;
                default:return false;
            }
            Run.coins-=price;Checkpoint(true);Audio.Cue("coin");UI.ShowShop();return true;
        }
        public int Price(string id)
        {
            switch(id){case "weapon":return Run.weaponLevel>=5?-1:Balance.UpgradePrice(85,Run.weaponLevel);case "rod":return Run.rodLevel>=3?-1:Balance.UpgradePrice(65,Run.rodLevel);case "hull":return Run.hullLevel>=3?-1:Balance.UpgradePrice(90,Run.hullLevel);case "heal":return Run.health>=Run.MaxHealth?-1:35;case "shotgun":return Run.shotgun?-1:135;case "harpoon":return Run.harpoon?-1:210;case "bait":return Run.abyssBait?-1:100;default:return -1;}
        }
        public void OpenRoute(){if(State==VoyageState.Shop)SetState(VoyageState.Route);}
        public void ChooseRoute(RouteKind route)
        {
            if(State!=VoyageState.Route)return;
            Run.route=route;Run.stage++;
            if(route==RouteKind.Abyss){Run.rareSignal|=Rng.NextDouble()<.2;Run.health=Mathf.Min(Run.MaxHealth,Run.health+12);}
            ClearEncounter();World.SetAct(Run.Act);Player.ResetForEncounter();Checkpoint(false);SetState(VoyageState.Sailing);
            Notice(Run.BossStage?"守关巨物潜伏水下 · 抛竿唤醒它":"新钓点已抵达 · 左键抛竿",3);
        }
        public void ChallengeKraken()
        {
            if(State!=VoyageState.Victory||Run.stage!=9||!(Run.abyssBait||Run.rareSignal))return;
            Run.stage=10;Run.health=Mathf.Min(Run.MaxHealth,Run.health+50);ClearEncounter();World.SetAct(2);Player.ResetForEncounter();Checkpoint(false);SetState(VoyageState.Sailing);
            Notice("禁忌鱼饵投入深渊 · 克拉肯正在苏醒",5);
        }
        public void ChallengeWhiteWhale()
        {
            if(State!=VoyageState.Victory||Run.stage!=9||!Run.rareSignal)return;
            Run.stage=11;Run.health=Mathf.Min(Run.MaxHealth,Run.health+50);ClearEncounter();World.SetAct(2);Player.ResetForEncounter();Checkpoint(false);SetState(VoyageState.Sailing);
            Notice("幽光航道的稀有访客 · 幽海白鲸浮出水面",5);
        }
        public void EndVoyage(bool victory)
        {
            if(State==VoyageState.Defeat||State==VoyageState.Victory)return;
            ClearHazards();ClearFishing();Audio.SetCombat(false);Paused=false;Time.timeScale=1;
            if(victory&&Run.stage==9)Log.victories++;
            SaveStore.ClearRun();SaveStore.Write("captain",Log);SetState(victory?VoyageState.Victory:VoyageState.Defeat);Audio.Cue(victory?"win":"lose");
        }
        public void ReturnHarbor()
        {Paused=false;Time.timeScale=1;ClearEncounter();World.SetAct(0);SetState(VoyageState.Harbor);}
        public void Checkpoint(bool between)
        {Run.betweenEncounters=between;SaveStore.Write("voyage",Run);}
        public void TogglePause()
        {
            Paused=!Paused;Time.timeScale=Paused?0:1;
            Cursor.lockState=Paused?CursorLockMode.None:(IsPlaying&&!Automation?CursorLockMode.Locked:CursorLockMode.None);Cursor.visible=Paused||!IsPlaying||Automation;
            if(Paused)UI.ShowPause();else UI.ShowState();
        }
        public void SetState(VoyageState state)
        {
            State=state;
            Cursor.lockState=IsPlaying&&!Automation?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=!IsPlaying||Automation;
            UI.ShowState();
        }
        public void Notice(string text,float seconds){Message=text;MessageUntil=Time.unscaledTime+seconds;}
        public void Warn(Vector3 pos,float radius,float delay,float damage)
        {var g=new GameObject("Telegraphed strike");g.transform.SetParent(Hazards);g.transform.position=pos;var w=g.AddComponent<DeckWarning>();w.game=this;w.radius=radius;w.delay=delay;w.damage=damage;w.Init();}
        public void Projectile(Vector3 from,Vector3 to,float speed,float damage,Color color)
        {var g=Shape.Part("Hostile projectile",PrimitiveType.Sphere,Hazards,from,Vector3.one*.4f,color,false,true);var p=g.AddComponent<SeaProjectile>();p.game=this;p.velocity=(to-from).normalized*speed;p.damage=damage;}
        public void Effect(Vector3 point,Color color,int count,float size)
        {for(int i=0;i<count;i++){var g=Shape.Part("Spray",PrimitiveType.Cube,Hazards,point,Vector3.one*size,color,false,true);var f=g.AddComponent<Fleck>();f.velocity=UnityEngine.Random.insideUnitSphere*3+Vector3.up*1.5f;f.life=UnityEngine.Random.Range(.25f,.6f);}}
        public void Tracer(Vector3 a,Vector3 b,Color color)
        {var g=new GameObject("Tracer");g.transform.SetParent(Hazards);var l=g.AddComponent<LineRenderer>();l.positionCount=2;l.SetPosition(0,a);l.SetPosition(1,b);l.startWidth=.025f;l.endWidth=.009f;l.material=Shape.Mat(color,true);Destroy(g,.07f);}
        void ClearHazards(){if(Hazards)for(int i=Hazards.childCount-1;i>=0;i--)Destroy(Hazards.GetChild(i).gameObject);}
        void ClearEncounter(){ClearFishing();foreach(var e in Enemies)if(e)Destroy(e.gameObject);Enemies.Clear();ClearHazards();}
        void OnApplicationQuit(){Time.timeScale=1;SaveStore.Write("captain",Log);}
        public void Quit(){SaveStore.Write("captain",Log);Application.Quit();}
    }
}

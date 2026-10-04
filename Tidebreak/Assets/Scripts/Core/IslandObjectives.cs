using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tidebreak
{
    public partial class GameDirector
    {
        GameObject missionRoot;
        Transform missionActor;
        readonly List<EncounterTarget> missionTargets=new List<EncounterTarget>();
        readonly List<Vector3> warmStations=new List<Vector3>();
        AudioSource melodySource;
        AudioClip[] melodyClips;
        int missionIsland, missionPhase, missionWave, melodyStep, melodyGeneration;
        float missionAge, missionProgress, missionIntegrity, missionHeat, missionPressure, missionDeadline, missionNextHazard;
        bool missionRunning, missionGoal, melodyPlaying;
        public int[] AstrolabeAxes=new int[3];
        public bool MissionActive {get{return missionRunning;}}
        public float MissionProgress {get{return missionProgress;}}
        public float MissionIntegrity {get{return missionIntegrity;}}
        public float MissionHeat {get{return missionHeat;}}
        public float MissionPressure {get{return missionPressure;}}
        public int MelodyStep {get{return melodyStep;}}
        public int DeliveredCharges {get{return (Run.eventMask>>8)&3;}}
        public int SealedAnchors {get{return (Run.eventMask>>10)&3;}}
        public string MissionTargetLabel {
            get {
                if(Run.questStep==0||Run.questStep==3&&Run.bossCleared)return Island.npc;
                if(Run.questStep>=4)return "群岛航图";
                if(Run.questStep==3||Run.stage>9)return "猎潮钟";
                if(Run.questStep==1)return "码头 · 钓回一条鱼";
                if(Run.stage==3&&missionRunning)return "净水舟 · 留在八米内";
                if(Run.stage==6&&missionRunning&&missionHeat<45)return "最近暖炉 · 靠近自动补热";
                if(Run.stage==6&&missionRunning)return "救生舱 · 站近后持续供能解冻";
                if(Run.stage==7&&missionRunning)return missionPhase==1?"东天线 · 交付电荷":"西阵列 · 领取电荷";
                if(Run.stage==8&&missionRunning)return missionPressure>58?"泄压阀":"添火阀";
                if(Run.stage==9&&missionRunning)return "逆潮锚 · 留在环内三秒";
                if(!missionRunning&&(Run.stage==3||Run.stage==7))return NarrativeContent.Chapter(Run.stage).siteA;
                return (Run.eventMask&1)==0?NarrativeContent.Chapter(Run.stage).siteA:NarrativeContent.Chapter(Run.stage).siteB;
            }
        }
        public Vector3 MissionTarget {
            get {
                if(Run.questStep==0||Run.questStep==3&&Run.bossCleared)return World.QuestPoint;
                if(Run.questStep>=4)return World.ChartPoint;
                if(Run.questStep==1||Run.questStep==3||Run.stage>9)return new Vector3(0,0,24);
                if(Run.stage==3&&missionRunning&&missionActor)return missionActor.position;
                if(Run.stage==6&&missionRunning&&missionHeat<45&&warmStations.Count>0){Vector3 closest=warmStations[0];foreach(var p in warmStations)if(FlatDistance(p,Player.transform.position)<FlatDistance(closest,Player.transform.position))closest=p;return closest;}
                if(Run.stage==6&&missionRunning)return SitePoint(1);
                if(Run.stage==7&&missionRunning)return SitePoint(missionPhase==1?1:0);
                if(Run.stage==8&&missionRunning)return ForgeValve(missionPressure>58?1:0);
                if(Run.stage==9&&missionRunning)return SitePoint(NextAnchor);
                if(!missionRunning&&(Run.stage==3||Run.stage==7))return SitePoint(0);
                return SitePoint((Run.eventMask&1)==0?0:1);
            }
        }
        public string MissionObjective {
            get {
                if(missionGoal)return "目标已完成 · 击退剩余守卫，让调查人员安全撤离";
                switch(Run.stage){
                    case 1:return !HasEvidence?"取回电池箱内的备用电池":missionRunning?"守住灯台 · 启动 "+Mathf.FloorToInt(missionProgress)+" / 12 秒":"去旧灯台安装电池，守住启动程序";
                    case 2:return !HasEvidence?"到潮池聆听共鸣贝的四拍旋律":"到珊瑚音叉回应四拍 · J 可查看录音记录";
                    case 3:return !HasEvidence?"射破净水阀旁的毒囊 "+PoisonCount+" / 3":missionRunning?"伴行净水舟 · 船体 "+Mathf.CeilToInt(missionIntegrity)+"% · 离开八米会停航":"回净水阀启动净水舟，护送到树屋";
                    case 4:return !HasEvidence?"阅读日轮石碑的三条方位证据":"到沉沙星盘，独立校准日 / 月 / 星三轴";
                    case 5:return !HasEvidence?(missionRunning?"黑匣下载 "+Mathf.FloorToInt(missionProgress)+" / 28 秒 · 完整度 "+Mathf.CeilToInt(missionIntegrity)+"%":"启动北星号黑匣，守住五米内的下载范围"):"前往搁浅船钟，接收那一夜的录音";
                    case 6:return missionRunning?"解冻 "+missionProgress.ToString("F1")+" / 18 秒 · 热量 "+Mathf.CeilToInt(missionHeat)+"% · "+(missionHeat<45?"离舱去暖炉补热，进度保留":"站在舱旁供能；热量低于35会暂停"):"去测温站领取热芯，在救生舱持续供能解冻十八秒";
                    case 7:return missionRunning?(missionPhase==1?"送电 "+DeliveredCharges+" / 3 · "+Mathf.CeilToInt(Mathf.Max(0,missionDeadline-Time.time))+" 秒 · 中央接地可延时":"送电 "+DeliveredCharges+" / 3 · 回西阵列领取下一轮电荷"):"启动西阵列 · 三轮电荷送到东天线";
                    case 8:return !HasEvidence?"从黑曜矿脉取出断潮钥坯":missionRunning?"锻造 "+Mathf.FloorToInt(missionProgress)+" / 22 · 温度 "+Mathf.CeilToInt(missionHeat)+" · 压力 "+Mathf.CeilToInt(missionPressure):"到古熔炉开启双阀锻造 · 稳温 45–75，压力低于 85";
                    case 9:return !HasEvidence?"在记忆棱镜，决定被困航行的去向":missionRunning?"封住逆潮锚 "+AnchorCount+" / 2 · 当前稳定 "+missionProgress.ToString("F1")+" / 3 秒":"去逆潮祭坛启动封锚程序";
                    default:return "继续探索这片海域";
                }
            }
        }
        bool HasEvidence {get{return (Run.eventMask&1)!=0;}}
        int PoisonCount {get{return ((Run.eventMask>>8)&1)+((Run.eventMask>>9)&1)+((Run.eventMask>>10)&1);}}
        int AnchorCount {get{return (SealedAnchors&1)+((SealedAnchors>>1)&1);}}
        int NextAnchor {get{int first=Run.storyChoice==1?0:1;return (SealedAnchors&(1<<first))==0?first:1-first;}}

        void MigrateIslandStory()
        {
            if(Run.checkpointVersion>=4)return;
            bool legacy=Run.checkpointVersion<3;
            if(legacy&&Run.bag!=null)foreach(var fish in Run.bag)fish.speciesId=-1;
            // Completed islands stay completed. Unfinished v0.3 errands restart at a
            // safe briefing, without touching the wallet, purchases or discoveries.
            for(int i=0;i<Run.islands.Length;i++){
                var p=Run.islands[i];if(p==null)continue;
                if(p.step<4){p.step=p.boss&&p.trophy?3:0;p.eventMask=0;p.explored&=4;}
            }
            if(Run.stage<=9&&Run.questStep<4){Run.questStep=Run.bossCleared&&Run.bossTrophy?3:0;Run.eventMask=0;Run.exploredMask&=4;}
            Run.checkpointVersion=4;
        }

        public void ResetIslandMission()
        {
            missionRunning=missionGoal=false;missionAge=missionProgress=0;missionPhase=missionWave=0;
            foreach(var t in missionTargets)if(t)Destroy(t.gameObject);missionTargets.Clear();
            if(missionRoot)Destroy(missionRoot);missionRoot=null;missionActor=null;missionIsland=0;warmStations.Clear();
            melodyPlaying=false;melodyStep=0;melodyGeneration++;Array.Clear(AstrolabeAxes,0,AstrolabeAxes.Length);
            if(melodySource)melodySource.Stop();
        }

        void EnsureMissionScene()
        {
            if(missionRoot&&missionIsland==Run.stage)return;
            ResetIslandMission();missionIsland=Run.stage;missionRoot=new GameObject("Island mission: "+Run.stage);
            if(Run.stage==3&&!HasEvidence){
                for(int i=0;i<3;i++){
                    if((Run.eventMask&(1<<(8+i)))!=0)continue;
                    int index=i;Vector3 at=SitePoint(0)+new Vector3((i-1)*2.6f,0,2.8f);at.y=World.GroundAt(at)+1.5f;
                    var target=EncounterTarget.Create(this,at,"毒囊 "+(i+1),34,new Color(.48f,.88f,.2f),t=>{
                        if(Run.stage!=3||Run.questStep!=2)return;Run.eventMask|=1<<(8+index);
                        Effect(t.transform.position,new Color(.4f,.8f,.2f),20,.13f);
                        if(PoisonCount==3){Run.eventMask|=1;Notice("水路已净化 · 在净水阀按 E 启动护送",5);}
                        else Notice("清除毒囊 "+PoisonCount+" / 3",2);Checkpoint(false);
                    });missionTargets.Add(target);
                    Shape.Beam(missionRoot.transform,at-Vector3.up*1.5f,at,.17f,new Color(.2f,.32f,.1f));
                }
            }
            if(Run.stage==6){
                for(int i=1;i<=2;i++){Vector3 p=Vector3.Lerp(SitePoint(0),SitePoint(1),i/3f);p.z+=i==1?4:-3;p.y=World.GroundAt(p);warmStations.Add(p);MissionMarker("救援暖炉",p,new Color(1,.57f,.2f),1.7f);}
            }
            if(Run.stage==7){Vector3 p=Vector3.Lerp(SitePoint(0),SitePoint(1),.5f);p.y=World.GroundAt(p);MissionMarker("接地环",p,Color.cyan,2.3f);}
            if(Run.stage==8){MissionMarker("添火阀",ForgeValve(0),new Color(1,.35f,.12f),1.2f);MissionMarker("泄压阀",ForgeValve(1),new Color(.3f,.85f,.95f),1.2f);}
            if(Run.stage==9){for(int i=0;i<2;i++)MissionMarker("逆潮锚",SitePoint(i),new Color(.45f,.85f,.95f),2.7f);}
        }

        void MissionMarker(string name,Vector3 p,Color color,float radius)
        {
            p.y=World.GroundAt(p)+.08f;
            Shape.Part(name,PrimitiveType.Cylinder,missionRoot.transform,p,new Vector3(radius,.06f,radius),color,false,true);
            Shape.Part(name+" core",PrimitiveType.Sphere,missionRoot.transform,p+Vector3.up*.9f,Vector3.one*.35f,color,false,true);
        }
        Vector3 ForgeValve(int valve){Vector3 p=SitePoint(1)+Vector3.right*(valve==0?-3.6f:3.6f)+Vector3.forward*2;p.y=World.GroundAt(p);return p;}

        public void TickIslandMission()
        {
            if(Paused||!IsPlaying||Run.stage>9)return;
            if(Run.questStep==1&&Run.landed>=1){Run.questStep=2;Checkpoint(false);Notice("第一份鱼获已到手 · 可去鱼市出售，再取电池修复灯塔",5);}
            if(Run.questStep!=2)return;
            EnsureMissionScene();
            if(!missionRunning||State!=VoyageState.Combat)return;
            float dt=Time.deltaTime;missionAge+=dt;
            if(missionGoal){if(Enemies.Count==0)CompleteIslandMission();return;}
            switch(Run.stage){
                case 1:
                    if(FlatDistance(Player.transform.position,SitePoint(1))<6)missionProgress+=dt;
                    if(missionAge>5&&missionWave==0){missionWave++;SpawnMissionEnemy(4,false,SitePoint(1)+Vector3.forward*7);}
                    if(missionProgress>=12)ReachMissionGoal();
                    break;
                case 3:TickEscort(dt);break;
                case 5:
                    if(FlatDistance(Player.transform.position,SitePoint(0))<5)missionProgress+=dt;
                    foreach(var e in Enemies)if(e&&FlatDistance(e.transform.position,SitePoint(0))<6)missionIntegrity-=dt*3.5f;
                    if(missionAge>9*(missionWave+1)&&missionWave<2){missionWave++;SpawnMissionEnemy(missionWave*3,missionWave==2,SitePoint(0)+Vector3.forward*8);}
                    if(missionIntegrity<=0)FailIslandMission("黑匣连接损坏 · 在黑匣处重新启动下载");
                    else if(missionProgress>=28&&Enemies.Count==0){missionRunning=false;Run.eventMask|=1;Run.exploredMask|=1;SetState(VoyageState.Sailing);Audio.SetCombat(false);Checkpoint(false);Notice("下载完成 · 前往搁浅船钟，接收完整录音",5);}
                    break;
                case 6:TickRescue(dt);break;
                case 7:TickRelay();break;
                case 8:TickForge(dt);break;
                case 9:TickAnchors(dt);break;
            }
        }

        public bool TryMissionInteraction(out string prompt)
        {
            prompt="";
            if(Paused||!IsPlaying||Run.questStep!=2||Run.stage>9||missionGoal)return false;
            if(missionRunning){
                if(Run.stage==7){int site=missionPhase==1?1:0;if(FlatDistance(Player.transform.position,SitePoint(site))<3.5f){prompt=missionPhase==1?"E  交付电荷 · "+DeliveredCharges+" / 3":"E  领取下一轮电荷";return true;}}
                if(Run.stage==8){int valve=FlatDistance(Player.transform.position,ForgeValve(0))<FlatDistance(Player.transform.position,ForgeValve(1))?0:1;if(FlatDistance(Player.transform.position,ForgeValve(valve))<2.6f){prompt=valve==0?"E  添火 · 温度 +28 / 压力 +18":"E  泄压 · 压力 −30 / 温度 −12";return true;}}
                return false;
            }
            for(int site=0;site<2;site++)if(FlatDistance(Player.transform.position,SitePoint(site))<3.5f){prompt="E  "+(site==0?NarrativeContent.Chapter(Run.stage).siteA:NarrativeContent.Chapter(Run.stage).siteB);return true;}
            return false;
        }

        public void UseMissionAction()
        {
            string prompt;if(!TryMissionInteraction(out prompt))return;
            if(!missionRunning){UseSite(FlatDistance(Player.transform.position,SitePoint(0))<FlatDistance(Player.transform.position,SitePoint(1))?0:1);return;}
            if(Run.stage==7){
                if(missionPhase==0){missionPhase=1;missionDeadline=Time.time+14;missionProgress=0;Audio.Cue("ready");Notice("电荷已充入 · 十四秒内送至东天线，中央接地可延时四秒",4);}
                else {int n=DeliveredCharges+1;Run.eventMask=(Run.eventMask&~(3<<8))|(n<<8);missionPhase=0;Checkpoint(false);Audio.Cue("discovery");if(n>=3)ReachMissionGoal();else {SpawnMissionEnemy(n*2,n==2,SitePoint(1)+Vector3.forward*6);Notice("送电 "+n+" / 3 · 回西阵列领取下一轮",3);}}
            }
            if(Run.stage==8&&Time.time>=missionDeadline){
                missionDeadline=Time.time+1.3f;int valve=FlatDistance(Player.transform.position,ForgeValve(0))<FlatDistance(Player.transform.position,ForgeValve(1))?0:1;
                if(valve==0){missionHeat+=28;missionPressure+=18;Audio.Cue("explosion");}else {missionPressure=Mathf.Max(0,missionPressure-30);missionHeat=Mathf.Max(0,missionHeat-12);Audio.Cue("splash");}
                Effect(ForgeValve(valve)+Vector3.up,valve==0?new Color(1,.5f,.1f):Color.cyan,14,.1f);
            }
        }

        void UseIslandObjective(int site)
        {
            EnsureMissionScene();
            switch(Run.stage){
                case 1:
                    if(site==0){RecordEvidence("备用电池还留着余温 · 去旧灯台启动归航灯");}
                    else if(RequireEvidence())StartMission(1);
                    break;
                case 2:
                    if(site==0){RecordEvidence("共鸣贝记录了四个音 · 到珊瑚音叉回应，可反复聆听");PlayMissionMelody();}
                    else if(RequireEvidence()){melodyStep=0;SetState(VoyageState.Dialogue);UI.ShowResonancePuzzle();}
                    break;
                case 3:
                    if(!HasEvidence){Notice("先用枪射破三个绿色毒囊，切断污染源",4);Player.SetRod(false);}
                    else if(site==0)StartMission(3);else Notice("净水舟还在阀门处等待启动",3);
                    break;
                case 4:
                    if(site==0){RecordEvidence("碑文：星光面向北；日轮在星光右侧；月潮与星光背向。去星盘校准三轴。");SetState(VoyageState.Dialogue);UI.ShowAstrolabeEvidence();}
                    else if(RequireEvidence()){SetState(VoyageState.Dialogue);UI.ShowAstrolabe();}
                    break;
                case 5:
                    if(site==0&&!HasEvidence)StartMission(5);
                    else if(site==1&&RequireEvidence())PlayCinematic(13,()=>CompleteIslandMission());
                    else Notice("下载已经完成，录音将在船钟处播放",3);
                    break;
                case 6:
                    if(site==0)StartMission(6);else Notice("先从测温站带来热芯，这里还有人在等你",4);
                    break;
                case 7:if(site==0)StartMission(7);else Notice("先去西阵列取电，东天线只接收稳定电荷",3);break;
                case 8:
                    if(site==0)RecordEvidence("钥坯已取出 · 去古熔炉操作添火阀与泄压阀");else if(RequireEvidence())StartMission(8);
                    break;
                case 9:
                    if(site==0&&!HasEvidence){SetState(VoyageState.Dialogue);UI.ShowMemoryChoice();}
                    else if(RequireEvidence())StartMission(9);
                    break;
            }
        }

        bool RequireEvidence(){if(HasEvidence)return true;Notice("先完成："+NarrativeContent.Chapter(Run.stage).siteA,4);return false;}
        void RecordEvidence(string message){Run.eventMask|=1;Run.exploredMask|=1;Checkpoint(false);Audio.Cue("discovery");Notice(message,7);}

        void StartMission(int stage)
        {
            if(missionRunning||Enemies.Count>0)return;
            ClearFishing();Charging=false;PendingSite=-1;missionRunning=true;missionGoal=false;missionAge=missionProgress=0;missionIntegrity=100;missionWave=missionPhase=0;missionNextHazard=Time.time+3;missionDeadline=0;
            SetState(VoyageState.Combat);Player.SetRod(false);Audio.SetCombat(true);
            if(stage==1)SpawnMissionEnemy(0,false,SitePoint(1)+Vector3.forward*7);
            if(stage==3){
                var boat=new GameObject("净水舟");boat.transform.SetParent(missionRoot.transform,false);boat.transform.position=SitePoint(0);missionActor=boat.transform;
                Shape.Part("船体",PrimitiveType.Cube,boat.transform,Vector3.up*.65f,new Vector3(1.7f,.5f,2.8f),new Color(.3f,.43f,.33f));
                Shape.Part("滤芯",PrimitiveType.Cylinder,boat.transform,Vector3.up*1.2f,new Vector3(.55f,.4f,.55f),Color.cyan,false,true);
                SpawnMissionEnemy(2,false,SitePoint(0)+Vector3.forward*6);
            }
            if(stage==5)SpawnMissionEnemy(4,false,SitePoint(0)+Vector3.forward*8);
            if(stage==6){missionHeat=100;SpawnMissionEnemy(1,false,Vector3.Lerp(SitePoint(0),SitePoint(1),.4f)+Vector3.forward*3);}
            if(stage==7){missionPhase=1;missionDeadline=Time.time+14;Run.eventMask|=1;}
            if(stage==8){missionHeat=58;missionPressure=20;missionDeadline=Time.time;}
            if(stage==9)SpawnMissionEnemy(9,true,SitePoint(NextAnchor)+Vector3.forward*6);
            // The final interaction is checkpointed before the last guards die.
            // Resume that cleanup, never ask for a fourth charge or a third anchor.
            if(stage==7&&DeliveredCharges>=3){SpawnMissionEnemy(4,true,SitePoint(1)+Vector3.forward*7);ReachMissionGoal();}
            if(stage==9&&AnchorCount>=2)ReachMissionGoal();
            Notice(QuestObjective,6);
        }

        public Enemy SpawnMissionEnemy(int variation,bool elite,Vector3 at)
        {
            if(Enemies.Count>=4)return null;
            at.y=World.GroundAt(at)+1.1f;
            int id=(Mathf.Clamp(Run.stage,1,9)-1)*12+Mathf.Abs(variation)%12;
            return SpawnSpecies(ExpeditionContent.Species[id],elite,at,true);
        }

        void TickEscort(float dt)
        {
            if(!missionActor){FailIslandMission("净水舟停航 · 回净水阀重新启动");return;}
            Vector3 target=SitePoint(1);Vector3 pos=missionActor.position;
            bool near=FlatDistance(Player.transform.position,pos)<8;
            if(near){Vector3 next=Vector3.MoveTowards(pos,target,dt*1.35f);next.y=World.GroundAt(next)+.25f;missionActor.position=next;Vector3 d=target-pos;d.y=0;if(d.sqrMagnitude>.1f)missionActor.rotation=Quaternion.Slerp(missionActor.rotation,Quaternion.LookRotation(d),dt*3);}
            foreach(var e in Enemies)if(e&&FlatDistance(e.transform.position,pos)<5)missionIntegrity-=dt*3;
            missionProgress=1-Mathf.Clamp01(FlatDistance(pos,target)/Mathf.Max(1,FlatDistance(SitePoint(0),target)));
            if(missionProgress>.28f&&missionWave==0){missionWave++;SpawnMissionEnemy(7,false,pos+Vector3.forward*7);}
            if(missionProgress>.65f&&missionWave==1){missionWave++;SpawnMissionEnemy(2,true,pos+Vector3.forward*8);}
            if(missionIntegrity<=0)FailIslandMission("净水舟遭腐蚀停航 · 清理敌人后可在净水阀重新启动");
            else if(FlatDistance(pos,target)<1.3f)ReachMissionGoal();
        }

        void TickRescue(float dt)
        {
            bool warm=false;foreach(var p in warmStations)if(FlatDistance(Player.transform.position,p)<2.9f)warm=true;
            bool supplying=FlatDistance(Player.transform.position,SitePoint(1))<3.5f&&missionHeat>=35;
            // Walking the short ice bridge is only the approach. The real rescue is
            // a powered thaw that forces safe retreats to the two heaters.
            if(supplying)missionProgress=Mathf.Min(18,missionProgress+dt);
            missionHeat=Mathf.Clamp(missionHeat+dt*(warm?19:-3.1f)-(supplying?dt*5:0),0,100);
            if(missionProgress>=5&&missionWave==0){missionWave++;SpawnMissionEnemy(2,false,SitePoint(1)+Vector3.forward*7);Notice("舱门开始松动 · 冰下守卫听见了供能声，离舱补热不会丢失解冻进度",4);}
            if(missionProgress>=12&&missionWave==1){missionWave++;SpawnMissionEnemy(8,true,SitePoint(1)+Vector3.left*7);Notice("舱内有人回应 · 继续解冻，留意新出现的冰海精英",4);}
            if(Time.time>missionNextHazard){missionNextHazard=Time.time+6;Warn(Player.transform.position,2.2f,1.7f,13);Notice("碎冰将落下 · 留意落点，热芯仍在冷却",2);}
            if(missionHeat<=0)FailIslandMission("热芯已经冷却 · 测温站备有替换热芯，沿途暖炉可以补热");
            else if(missionProgress>=18){Run.eventMask|=1;ReachMissionGoal();}
        }

        void TickRelay()
        {
            if(Time.time>missionNextHazard){missionNextHazard=Time.time+Mathf.Max(2.7f,5-DeliveredCharges*.8f);Warn(Player.transform.position,2.1f,1.35f,17);}
            if(missionPhase!=1)return;
            var ground=Vector3.Lerp(SitePoint(0),SitePoint(1),.5f);
            if(missionProgress==0&&FlatDistance(Player.transform.position,ground)<2.7f){missionDeadline+=4;missionProgress=1;Audio.Cue("sonar");Notice("接地完成 · 电荷稳定时间 +4 秒",2);}
            if(Time.time>missionDeadline){missionPhase=0;Notice("电荷耗尽 · 已送达轮次保留，回西阵列重新取电",4);}
        }

        void TickForge(float dt)
        {
            missionHeat=Mathf.Max(0,missionHeat-dt*4.5f);missionPressure=Mathf.Max(0,missionPressure+dt*(missionHeat>40?3.1f:-2));
            if(missionHeat>=45&&missionHeat<=75&&missionPressure<85)missionProgress+=dt;
            if(missionHeat>88||missionPressure>95){missionHeat=58;missionPressure=40;missionProgress=Mathf.Max(0,missionProgress-3);Warn(SitePoint(1),3.3f,1.1f,20);Notice("熔炉过载 · 躲开泄流；锻造进度退回三秒",3);}
            if(missionProgress>7&&missionWave==0){missionWave++;SpawnMissionEnemy(4,false,SitePoint(1)+Vector3.forward*8);}
            if(missionProgress>15&&missionWave==1){missionWave++;SpawnMissionEnemy(0,true,SitePoint(1)+Vector3.forward*8);}
            if(missionProgress>=22)ReachMissionGoal();
        }

        void TickAnchors(float dt)
        {
            int anchor=NextAnchor;
            if(FlatDistance(Player.transform.position,SitePoint(anchor))<2.9f)missionProgress+=dt;
            else missionProgress=Mathf.Max(0,missionProgress-dt*1.5f);
            if(Time.time>missionNextHazard){missionNextHazard=Time.time+4.2f;ThreatField.Line(this,SitePoint(1-anchor)+Vector3.up*.5f,Player.transform.position,1,1.4f,19,new Color(.55f,.55f,1));}
            if(missionProgress<3)return;
            Run.eventMask|=1<<(10+anchor);missionProgress=0;Audio.Cue("discovery");Checkpoint(false);
            if(AnchorCount>=2)ReachMissionGoal();
            else {SpawnMissionEnemy(7,true,SitePoint(1-anchor)+Vector3.forward*6);Notice("第一道锚已封住 · 去另一端完成封锚，既有进度会保留",4);}
        }

        void ReachMissionGoal()
        {
            if(missionGoal)return;missionGoal=true;
            if(Enemies.Count==0)CompleteIslandMission();else Notice("机关已完成 · 击退剩余守卫，保证撤离安全",4);
        }
        public bool ResolveMissionEncounter()
        {
            if(!missionRunning)return false;
            if(missionGoal&&Enemies.Count==0)CompleteIslandMission();
            return true;
        }

        void FailIslandMission(string reason)
        {
            foreach(var e in Enemies)if(e)Destroy(e.gameObject);Enemies.Clear();ClearHazards();
            ResetIslandMission();SetState(VoyageState.Sailing);Audio.SetCombat(false);Checkpoint(false);Notice(reason,6);
        }

        void CompleteIslandMission()
        {
            if(Run.questStep>=3)return;
            int chapter=Run.stage;Run.questStep=3;Run.eventMask|=3;Run.exploredMask|=3;
            int stipend=70+chapter*15;Run.coins+=stipend;Run.earned+=stipend;
            ResetIslandMission();ClearHazards();SetState(VoyageState.Sailing);Audio.SetCombat(false);Audio.Cue("discovery");SaveStore.Write("captain",Log);Checkpoint(false);
            if(chapter==7){PlayCinematic(14,()=>Notice("阵列已恢复 · 调查报酬 +"+stipend+" 金币\nJ 查看完整证据 · 准备好后摇响猎潮钟",7));return;}
            Notice("调查完成 · 报酬 +"+stipend+" 金币\nJ 查看新的故事证据 · 准备好后摇响猎潮钟",7);
        }

        public void ChooseMemory(int choice)
        {
            if(Run.stage!=9||Run.questStep!=2||State!=VoyageState.Dialogue||HasEvidence||FlatDistance(Player.transform.position,SitePoint(0))>3.5f)return;
            Run.storyChoice=Mathf.Clamp(choice,0,1);RecordEvidence(choice==1?"决定：让世界保留这些航行的记忆 · 先封西锚，再封东锚":"决定：让被困者放下昨天 · 先封东锚，再封西锚");CloseDialogue();
        }
        public void RotateAstrolabe(int axis)
        {
            if(Run.stage!=4||State!=VoyageState.Dialogue||Run.questStep!=2||axis<0||axis>2)return;
            AstrolabeAxes[axis]=(AstrolabeAxes[axis]+1)%3;Audio.Cue("select");UI.ShowAstrolabe();
        }
        public bool ConfirmAstrolabe()
        {
            if(Run.stage!=4||State!=VoyageState.Dialogue||!HasEvidence||Run.questStep!=2||FlatDistance(Player.transform.position,SitePoint(1))>3.5f)return false;
            if(!NarrativeContent.AstrolabeSolved(AstrolabeAxes[0],AstrolabeAxes[1],AstrolabeAxes[2])){UI.ShowAstrolabe("星盘仍未对齐：检查三条方位关系，再调整各轴。");Audio.Cue("select");return false;}
            CloseDialogue();CompleteIslandMission();return true;
        }
        public void AnswerMissionTone(int note)
        {
            if(Run.stage!=2||Run.questStep!=2||State!=VoyageState.Dialogue||!HasEvidence||melodyPlaying||FlatDistance(Player.transform.position,SitePoint(1))>3.5f)return;
            PlayTone(note);int[] target=NarrativeContent.Melody(Run.seed);
            if(note!=target[melodyStep]){melodyStep=0;UI.ShowResonancePuzzle("回声散去了。可以回放，再从第一拍回应。");return;}
            melodyStep++;if(melodyStep==target.Length){CloseDialogue();CompleteIslandMission();return;}UI.ShowResonancePuzzle();
        }
        public void PlayMissionMelody()
        {
            if(melodyPlaying)return;StartCoroutine(MissionMelody());
        }
        IEnumerator MissionMelody()
        {
            melodyPlaying=true;int chapter=Run.stage,generation=melodyGeneration;int[] notes=NarrativeContent.Melody(Run.seed);
            for(int i=0;i<notes.Length;i++){
                if(chapter!=Run.stage||Run.questStep!=2||generation!=melodyGeneration){if(generation==melodyGeneration)melodyPlaying=false;yield break;}
                PlayTone(notes[i]);Notice("共鸣贝 · 第 "+(i+1)+" 拍："+NarrativeContent.ToneNames[notes[i]],.8f);
                if(State==VoyageState.Dialogue)UI.ShowResonancePuzzle("第 "+(i+1)+" 拍 · "+NarrativeContent.ToneNames[notes[i]]);
                yield return new WaitForSecondsRealtime(.85f);
            }
            if(generation!=melodyGeneration)yield break;
            melodyPlaying=false;if(State==VoyageState.Dialogue&&Run.stage==2&&Run.questStep==2)UI.ShowResonancePuzzle();
        }
        void PlayTone(int note)
        {
            if(note<0||note>2)return;
            if(!melodySource)melodySource=gameObject.AddComponent<AudioSource>();
            if(melodyClips==null){melodyClips=new AudioClip[3];for(int n=0;n<3;n++){
                const int rate=22050;float[] samples=new float[rate/2];float frequency=new[]{261.63f,329.63f,392f}[n];
                for(int i=0;i<samples.Length;i++){float t=i/(float)rate;samples[i]=Mathf.Sin(t*frequency*Mathf.PI*2)*Mathf.Exp(-t*7)*Mathf.Min(1,t*55)*.28f;}
                melodyClips[n]=AudioClip.Create("Coral tone "+n,samples.Length,1,rate,false);melodyClips[n].SetData(samples,0);
            }}Audio.PlayMechanismTone(melodyClips[note],.42f);
        }
    }
}

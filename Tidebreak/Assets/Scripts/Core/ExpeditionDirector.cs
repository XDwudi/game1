using System;
using System.Collections.Generic;
using UnityEngine;
namespace Tidebreak
{
    public partial class GameDirector
    {
        public int PendingSite=-1,SecretStep;
        public float SonarUntil,TonicUntil;
        public bool Chummed;
        public bool StoryComplete {get{return Run.stage==9&&Run.questStep>=4||Run.islands!=null&&Run.islands.Length==9&&Run.islands[8]!=null&&Run.islands[8].step>=4;}}
        public IslandDefinition Island {get{return ExpeditionContent.Island(Run.stage);}}
        public string QuestObjective {
            get {
                if(Run.stage>=10)return "传说远征 · 在猎潮钟前呼唤巨物";
                if(Run.questStep==0)return "与 "+Island.npc+" 交谈 · "+Island.title;
                if(Run.questStep==1)return Run.landed<1?"钓回一条鱼 · 击败后 E 拿起 / F 收纳；出售可获得升级金币":"前往电池箱，恢复灯塔供电";
                if(Run.questStep==2)return MissionObjective;
                if(Run.questStep==3)return Run.bossCleared?"潮核已取回 · 与 "+Island.npc+" 告别，开启新航线":"调查完成 · 摇响码头猎潮钟，迎战 "+Island.bossName;
                return "航线已恢复 · 可出发，也可留下寻找新物种与隐藏日志";
            }
        }
        void EnsureExpedition()
        {
            if(Log.speciesSeen==null)Log.speciesSeen=new bool[119];else Array.Resize(ref Log.speciesSeen,119);
            if(Log.speciesGold==null)Log.speciesGold=new bool[119];else Array.Resize(ref Log.speciesGold,119);
            if(Log.speciesWeight==null)Log.speciesWeight=new float[119];else Array.Resize(ref Log.speciesWeight,119);
            if(Log.speciesKills==null)Log.speciesKills=new int[119];else Array.Resize(ref Log.speciesKills,119);
            if(Run.islands==null||Run.islands.Length!=9)Run.islands=new IslandProgress[9];
            if(Run.islandCaught==null)Run.islandCaught=new List<int>();
            Run.maxIsland=Mathf.Clamp(Mathf.Max(Run.maxIsland,Mathf.Min(Run.stage,9)),1,9);
            MigrateIslandStory();
            if(Run.stage>9&&Run.islands[8]!=null){
                // Legendary harbours use the ninth island's workshop. Its saved
                // purchase mask is authoritative, including when migrating a save
                // made before rare voyages stopped inheriting their departure island.
                Run.shopMask=Run.islands[8].shopMask;
                if(Run.bossCleared&&Run.islands[8].step>=4)RestoreIslandProgress(9);
            }
        }
        void StoreIsland()
        {
            if(Run.stage>9){
                if(Run.islands!=null&&Run.islands.Length==9&&Run.islands[8]!=null)Run.islands[8].shopMask=Run.shopMask;
                return;
            }
            EnsureExpedition();
            Run.islands[Run.stage-1]=new IslandProgress{step=Run.questStep,landed=Run.landed,explored=Run.exploredMask,boss=Run.bossCleared,trophy=Run.bossTrophy,shopMask=Run.shopMask,eventMask=Run.eventMask,caught=new List<int>(Run.islandCaught)};
        }
        void RestoreIslandProgress(int stage)
        {
            var p=Run.islands[stage-1]??new IslandProgress();
            Run.stage=stage;Run.questStep=p.step;Run.landed=p.landed;Run.exploredMask=p.explored;
            Run.bossCleared=p.boss;Run.bossTrophy=p.trophy;Run.shopMask=p.shopMask;Run.eventMask=p.eventMask;
            Run.islandCaught=new List<int>(p.caught??new List<int>());
        }
        public void Travel(int stage,RouteKind route=RouteKind.Shoal)
        {
            if(State!=VoyageState.Route||stage<1||stage>Run.maxIsland||stage==Run.stage)return;
            if(!Player.StowHeld()){Notice("鱼篓已满，先售卖手中的鱼获再离岛",3);return;}StoreIsland();bool first=Run.islands[stage-1]==null;
            RestoreIslandProgress(stage);Run.route=route;
            if(first&&route==RouteKind.Abyss){Run.rareSignal|=Rng.NextDouble()<.2;Run.health=Mathf.Min(Run.MaxHealth,Run.health+12);}
            ClearEncounter();PendingSite=-1;SecretStep=0;RollShop();SetRegion(stage-1);Player.ResetForEncounter();SetState(VoyageState.Sailing);Checkpoint(false);Audio.Cue("discovery");Notice("抵达 "+Island.name+" · "+Island.title,5);PlayCinematic(stage);
        }
        public void TalkGuide()
        {
            if(State!=VoyageState.Sailing||Run.stage>9||FlatDistance(Player.transform.position,World.QuestPoint)>4)return;
            SetState(VoyageState.Dialogue);UI.ShowStory(true);
        }
        public bool AdvanceStory()
        {
            if(State!=VoyageState.Dialogue||FlatDistance(Player.transform.position,World.QuestPoint)>4||Run.stage>9)return false;
            if(Run.questStep==0){Run.questStep=Run.stage==1&&Run.landed<1?1:2;CloseDialogue();Checkpoint(false);Notice(QuestObjective,6);return true;}
            if(Run.questStep==3&&Run.bossCleared&&Run.bossTrophy){
                Run.questStep=4;Run.bossTrophy=false;Run.maxIsland=Mathf.Max(Run.maxIsland,Mathf.Min(9,Run.stage+1));
                int pay=45+Run.stage*8;Run.coins+=pay;Run.earned+=pay;
                if(Run.stage==9)Run.endingPending=true;
                Audio.Cue("discovery");Checkpoint(false);CloseDialogue();
                if(Run.stage==9)PlayCinematic(12,()=>EndVoyage(true));
                else Notice("航线恢复 · +"+pay+" 金币\n下一岛开放新的购买资格，请在航图出发",6);
                return true;
            }
            CloseDialogue();return false;
        }
        public void CloseDialogue(){if(State==VoyageState.Dialogue)SetState(VoyageState.Sailing);}
        public void ExploreAfterEnding()
        {
            if(State!=VoyageState.Victory||!StoryComplete)return;
            StoreIsland();ClearEncounter();RestoreIslandProgress(9);RollShop();SetRegion(8);
            PendingSite=-1;SecretStep=0;Player.ResetForEncounter();SetState(VoyageState.Sailing);Checkpoint(false);
            Notice("返回镜渊神庙 · 已完成的调查、秘密与工坊购买记录均已保留",5);
        }
        public Vector3 SitePoint(int index){return World.SitePoints[Mathf.Clamp(index,0,2)];}
        public void UseSite(int site)
        {
            if(State!=VoyageState.Sailing||Run.stage>9||FlatDistance(Player.transform.position,SitePoint(site))>3.5f)return;
            if(site==2){if((Run.exploredMask&4)!=0){Notice("这份隐藏日志已经收录",2);return;}SecretStep=0;SetState(VoyageState.Dialogue);UI.ShowRune();return;}
            if(Run.questStep==0){Notice("先与 "+Island.npc+" 交谈，了解这里发生了什么",4);return;}
            if(Run.questStep==1&&Run.landed<1){Notice("灯塔启动前，先钓回一条鱼。鱼获需要 E 拿起才能计入任务",4);return;}
            if(Run.questStep>=3){Notice("这里的调查已经完成 · "+QuestObjective,3);return;}
            Run.questStep=2;UseIslandObjective(site);
        }
        void StartSurveyBattle(int site)
        {
            PendingSite=site;ClearFishing();SetState(VoyageState.Combat);Player.SetRod(false);Audio.SetCombat(true);
            SpawnMissionEnemy(0,false,Player.transform.position+Player.transform.forward*6);
            SpawnMissionEnemy(1,true,Player.transform.position+Player.transform.forward*7+Player.transform.right*3);
            Notice("日志的守卫醒了 · 击败守卫可以取回隐藏发现",4);
        }
        public void SolveRune(int choice)
        {
            if(State!=VoyageState.Dialogue||FlatDistance(Player.transform.position,SitePoint(2))>3.5f)return;
            if(choice!=(Run.stage-1+SecretStep)%3){CloseDialogue();StartSurveyBattle(2);return;}
            SecretStep++;if(SecretStep<3){UI.ShowRune();return;}CloseDialogue();CompleteSite(2);
        }
        void CompleteSite(int site)
        {
            if((Run.exploredMask&(1<<site))!=0){PendingSite=-1;return;}
            Run.exploredMask|=1<<site;PendingSite=-1;
            if(site==2){int coins=50+Run.stage*8;Run.coins+=coins;Run.earned+=coins;Run.sonarCharges++;Run.rareSignal=true;Notice("发现 "+Island.secret+" · +"+coins+" 金币 · 记录一条古神航路",5);}
            Audio.Cue("discovery");Checkpoint(false);
        }
        public void CycleLure(){Run.selectedLure=(Run.selectedLure+1)%(Run.lureTier+1);Notice("拟饵："+ExpeditionContent.Lures[Run.selectedLure]+" · 可吸引更多本地物种",3);Checkpoint(false);}
        public void UseUtility(string id)
        {
            if(!IsPlaying||Paused)return;
            if(id=="medkit"&&Run.medkits>0&&Run.health<Run.MaxHealth){Run.medkits--;Run.health=Mathf.Min(Run.MaxHealth,Run.health+45);Audio.Cue("heal");Notice("急救包 · 恢复 45 生命",2);}
            else if(id=="sonar"&&Run.sonarCharges>0){Run.sonarCharges--;SonarUntil=Time.time+30;Audio.Cue("sonar");Notice("声呐回波 · 30 秒内显示探索地点的方向与距离",3);}
            else if(id=="tonic"&&Run.tonics>0){Run.tonics--;TonicUntil=Time.time+12;Audio.Cue("heal");Notice("乘风药剂 · 12 秒加速与快速冲刺",3);}
            else if((id=="bomb"&&Run.bombs>0||id=="frost"&&Run.frostbombs>0)&&State==VoyageState.Combat){if(id=="bomb")Run.bombs--;else Run.frostbombs--;ThrownUtility.Create(this,id=="frost");}
            else return;
            Checkpoint(false);
        }
        Enemy SpawnSpecies(SpeciesDefinition spec,bool elite,Vector3 at,bool summoned=false)
        {
            var e=new GameObject(spec.name).AddComponent<Enemy>();e.InitSpecies(this,spec,elite,at);e.Summoned=summoned;return e;
        }
        public void SpawnMinion(Enemy owner)
        {
            if(Enemies.Count>=5)return;
            // Sea bosses sit beyond the pier. Their escorts enter on visible land,
            // with a guaranteed gap from the captain, instead of water-teleporting.
            Vector3 p=World.Spawn;float start=UnityEngine.Random.value*Mathf.PI*2;
            for(int i=0;i<12;i++){
                float angle=start+i*Mathf.PI/6;Vector3 candidate=Player.transform.position+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*6.5f;
                float ground=World.GroundAt(candidate);if(ground<.1f)continue;p=candidate;p.y=ground+1;break;
            }
            p.y=World.GroundAt(p)+1;
            var minion=SpawnSpecies(ExpeditionContent.Species[owner.Spec.island*12],false,p,true);minion.health=minion.maxHealth*=.35f;minion.transform.localScale*=.6f;
        }
        public void ShowMap(){if(State!=VoyageState.Sailing)return;SetState(VoyageState.Route);}
    }
}

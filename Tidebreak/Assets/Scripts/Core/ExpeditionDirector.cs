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
                if(Run.stage>=10)return "稀有远征 · 在码头摇钟唤醒巨物";
                switch(Run.questStep){
                    case 0:return "前往向导处，与 "+Island.npc+" 交谈";
                    case 1:return Run.SamplesReady?"样本齐备 · 回到向导处提交研究":"收集本地鱼获 "+Run.landed+" / "+Run.Quota+" · 不同物种 "+Run.islandCaught.Count+" / 2";
                    case 2:return Run.SurveyReady?"部件齐备 · 回向导处获得首领线索":"探索："+((Run.exploredMask&1)!=0?"✓ ":"○ ")+Island.siteA+" / "+((Run.exploredMask&2)!=0?"✓ ":"○ ")+Island.siteB;
                    case 3:return Run.bossCleared?"带着潮核回到向导处，解锁下一座岛":"在码头摇钟挑战 "+Island.bossName+" · 先买好补给";
                    default:return "岛屿已完成 · 航图前往下一岛，或寻找剩余物种与秘密";
                }
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
            if(Run.checkpointVersion<3){Run.questStep=0;Run.landed=0;Run.bossCleared=false;if(Run.bag!=null)foreach(var catchData in Run.bag)catchData.speciesId=-1;Run.checkpointVersion=3;}
        }
        void StoreIsland()
        {
            if(Run.stage>9)return;EnsureExpedition();
            Run.islands[Run.stage-1]=new IslandProgress{step=Run.questStep,landed=Run.landed,explored=Run.exploredMask,boss=Run.bossCleared,trophy=Run.bossTrophy,shopMask=Run.shopMask,eventMask=Run.eventMask,caught=new List<int>(Run.islandCaught)};
        }
        public void Travel(int stage,RouteKind route=RouteKind.Shoal)
        {
            if(State!=VoyageState.Route||stage<1||stage>Run.maxIsland||stage==Run.stage)return;
            if(!Player.StowHeld()){Notice("鱼篓已满，先售卖手中的鱼获再离岛",3);return;}StoreIsland();bool first=Run.islands[stage-1]==null;var p=Run.islands[stage-1]??new IslandProgress();
            Run.stage=stage;Run.route=route;Run.questStep=p.step;Run.landed=p.landed;Run.exploredMask=p.explored;Run.bossCleared=p.boss;Run.bossTrophy=p.trophy;Run.shopMask=p.shopMask;Run.eventMask=p.eventMask;Run.islandCaught=new List<int>(p.caught??new List<int>());
            if(first&&route==RouteKind.Abyss){Run.rareSignal|=Rng.NextDouble()<.2;Run.health=Mathf.Min(Run.MaxHealth,Run.health+12);}
            ClearEncounter();PendingSite=-1;SecretStep=0;RollShop();SetRegion(stage-1);Player.ResetForEncounter();SetState(VoyageState.Sailing);Checkpoint(false);Audio.Cue("discovery");Notice("抵达 "+Island.name+" · "+Island.title,5);
        }
        public void TalkGuide()
        {
            if(State!=VoyageState.Sailing||FlatDistance(Player.transform.position,World.QuestPoint)>4)return;
            SetState(VoyageState.Dialogue);UI.ShowStory(true);
        }
        public bool AdvanceStory()
        {
            if(State!=VoyageState.Dialogue||FlatDistance(Player.transform.position,World.QuestPoint)>4||Run.stage>9)return false;
            bool advance=Run.questStep==0||Run.questStep==1&&Run.SamplesReady||Run.questStep==2&&Run.SurveyReady||Run.questStep==3&&Run.bossCleared&&Run.bossTrophy;
            if(!advance){CloseDialogue();return false;}
            Run.questStep++;if(Run.questStep==4){Run.bossTrophy=false;Run.maxIsland=Mathf.Max(Run.maxIsland,Mathf.Min(9,Run.stage+1));Run.coins+=50+Run.stage*10;Run.earned+=50+Run.stage*10;}
            Audio.Cue("discovery");Checkpoint(false);
            if(Run.questStep==4&&Run.stage==9){CloseDialogue();EndVoyage(true);}else {CloseDialogue();Notice(Run.questStep==4?"潮核已交付 · 新岛屿与新的购买许可证已解锁":QuestObjective,5);}
            return true;
        }
        public void CloseDialogue(){if(State==VoyageState.Dialogue)SetState(VoyageState.Sailing);}
        public void ExploreAfterEnding(){if(State!=VoyageState.Victory)return;Run.stage=9;Run.questStep=4;Run.bossCleared=true;SetState(VoyageState.Sailing);Checkpoint(false);}
        public Vector3 SitePoint(int index){return World.SitePoints[Mathf.Clamp(index,0,2)];}
        public void UseSite(int site)
        {
            if(State!=VoyageState.Sailing||FlatDistance(Player.transform.position,SitePoint(site))>3.5f)return;
            if((Run.exploredMask&(1<<site))!=0){Notice("这里已经探索完成",2);return;}
            if(site==2){SecretStep=0;SetState(VoyageState.Dialogue);UI.ShowRune();return;}
            if(Run.questStep<2){Notice("先向 "+Island.npc+" 提交样本，了解这处遗迹的线索",3);return;}
            if(site==0){Run.exploredMask|=1;Run.coins+=25;Run.earned+=25;Checkpoint(false);Audio.Cue("discovery");Notice("找到 "+Island.siteA+" · +25 金币\n"+Island.survey,5);}
            else StartSurveyBattle(site);
        }
        void StartSurveyBattle(int site)
        {
            PendingSite=site;ClearFishing();SetState(VoyageState.Combat);Player.SetRod(false);Audio.SetCombat(true);
            for(int i=0;i<2;i++){
                var spec=ExpeditionContent.Species[(Mathf.Min(Run.stage,9)-1)*12+(i+Run.stage+3)%6];
                Vector3 p=Player.transform.position+Player.transform.forward*(4+i*2)+Player.transform.right*(i==0?-2:2);p.y=World.GroundAt(p)+1;
                SpawnSpecies(spec,i==1,p,false);
            }
            Notice("遗迹守卫苏醒 · 击败它们，取回 "+(site==2?Island.secret:Island.siteB),5);
        }
        public void SolveRune(int choice)
        {
            if(State!=VoyageState.Dialogue||FlatDistance(Player.transform.position,SitePoint(2))>3.5f)return;
            if(choice!=(Run.stage-1+SecretStep)%3){CloseDialogue();StartSurveyBattle(2);return;}
            SecretStep++;if(SecretStep<3){UI.ShowRune();return;}
            CloseDialogue();CompleteSite(2);
        }
        void CompleteSite(int site)
        {
            Run.exploredMask|=1<<site;PendingSite=-1;
            if(site==2){int coins=60+Run.stage*10;Run.coins+=coins;Run.earned+=coins;Run.sonarCharges++;Run.rareSignal=true;Notice("发现 "+Island.secret+" · +"+coins+" 金币 · 古神信号已记录",5);}
            else Notice("取回 "+Island.siteB+" · 回向导处拼合线索",4);
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
            if(Enemies.Count>=5)return;Vector3 p=owner.transform.position+UnityEngine.Random.insideUnitSphere*2;p.y=World.GroundAt(p)+1;
            var minion=SpawnSpecies(ExpeditionContent.Species[owner.Spec.island*12],false,p,true);minion.health=minion.maxHealth*=.35f;minion.transform.localScale*=.6f;
        }
        public void ShowMap(){if(State!=VoyageState.Sailing)return;SetState(VoyageState.Route);}
    }
}

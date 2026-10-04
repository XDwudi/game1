using UnityEngine;

namespace Tidebreak
{
    public partial class RunData
    {
        public int keystoneMask,storyChoice,cinematicMask;
        public bool endingPending;
        public bool HasKeystone(int index){return (keystoneMask&(1<<index))!=0;}
        public bool HasActKeystone(int act){return (keystoneMask&(7<<(act*3)))!=0;}
    }
    public static class VoyageBuilds
    {
        public static readonly string[] Names={"猎手的节拍","蒸汽冷凝器","踏浪快装","孤注猎约","风暴回路","守潮反击","死点测距仪","热雷引爆器","不灭归航灯"};
        public static readonly string[] Descriptions={
            "连续 4 次弱点命中后，下一枪伤害 +65%。\n适合左轮 / 三连发；打空会中断节拍。",
            "同时拥有灼烧与冰霜时，命中产生蒸汽爆发。\n范围伤害，每 4 秒一次；购买后附带各 1 层。",
            "冲刺后 3 秒内伤害 +25%，装填加快 25%。\n适合贴近打完霰弹，再冲刺撤离。",
            "生命低于 35% 时伤害 +45%。\n所有受到的伤害 +12%；高风险残血流。",
            "累计 6 次命中，连锁打击附近最多 3 个敌人。\n额外造成该次伤害的 45%，不会递归触发。",
            "在冲刺无敌帧挡下攻击，下一枪伤害 +70%。\n同时回复 5 生命；每 3 秒最多触发一次。",
            "距目标超过 12 米时，弱点伤害额外 +35%。\n贴脸时无加成；适合鱼叉与精准射击。",
            "火雷装置搭配时，命中触发热雷爆裂。\n每 5 秒一次；购买后附带灼烧与带电各 1 层。",
            "弱点命中恢复 3 生命，每 2 秒一次。\n生命低于 35% 时，冲刺冷却缩短 20%。"
        };
        public static int Price(int act){return 110+act*90;}
        public static int Island(int act){return 2+act*3;}
        public static bool CanBuy(RunData run,int index)
        {int act=index/3;return index>=0&&index<9&&run.maxIsland>=Island(act)&&!run.HasActKeystone(act)&&run.coins>=Price(act);}
        public static void Apply(RunData run,int index)
        {run.keystoneMask|=1<<index;if(index==1){run.fireRelics=Mathf.Max(1,run.fireRelics);run.iceRelics=Mathf.Max(1,run.iceRelics);}if(index==7){run.fireRelics=Mathf.Max(1,run.fireRelics);run.shockRelics=Mathf.Max(1,run.shockRelics);}}
    }
    public partial class GameDirector
    {
        public bool BuyKeystone(int index)
        {
            if(State!=VoyageState.Shop||!VoyageBuilds.CanBuy(Run,index))return false;
            Run.coins-=VoyageBuilds.Price(index/3);VoyageBuilds.Apply(Run,index);Checkpoint(false);Audio.Cue("discovery");UI.ShowShop();return true;
        }
    }
    public class BuildSynergy : MonoBehaviour
    {
        AnglerController player;GameDirector game;int weakStreak,hitCount;float dashUntil,steamReady,thunderReady,healReady,counterReady;bool precision,counter;
        public string Status {get{return counter?"守潮反击 · 下一枪强化":precision?"猎手节拍 · 下一枪强化":Time.time<dashUntil&&game.Run.HasKeystone(2)?"踏浪增伤":game.Run.HasKeystone(0)?"猎手节拍 "+weakStreak+" / 4":"";}}
        public void Init(AnglerController p){player=p;game=p.game;}
        public void Reset(){weakStreak=hitCount=0;dashUntil=steamReady=thunderReady=healReady=counterReady=0;precision=counter=false;}
        public void Dash(){dashUntil=Time.time+3;}
        public float ReloadSpeed {get{return Time.time<dashUntil&&game.Run.HasKeystone(2)?1.25f:1;}}
        public float ShotMultiplier()
        {
            float m=1;if(precision){m*=1.65f;precision=false;}if(counter){m*=1.7f;counter=false;}
            if(Time.time<dashUntil&&game.Run.HasKeystone(2))m*=1.25f;
            if(game.Run.HasKeystone(3)&&game.Run.health<game.Run.MaxHealth*.35f)m*=1.45f;return m;
        }
        public void AvoidedAttack()
        {
            if(!game.Run.HasKeystone(5)||Time.time<counterReady)return;counterReady=Time.time+3;counter=true;game.Run.health=Mathf.Min(game.Run.MaxHealth,game.Run.health+5);game.Notice("完美闪避 · 守潮反击已充能",2);
        }
        public void RegisterShot(bool hit,bool weak)
        {
            if(!game.Run.HasKeystone(0))return;if(!hit||!weak)weakStreak=0;
            else if(++weakStreak>=4){weakStreak=0;precision=true;game.Audio.Cue("ready");}
        }
        public void OnHit(Enemy e,bool weak,float damage)
        {
            if(weak&&game.Run.HasKeystone(8)&&Time.time>=healReady){healReady=Time.time+2;game.Run.health=Mathf.Min(game.Run.MaxHealth,game.Run.health+3);}
            hitCount++;bool chain=game.Run.HasKeystone(4)&&hitCount%6==0;
            bool steam=game.Run.HasKeystone(1)&&game.Run.fireRelics>0&&game.Run.iceRelics>0&&Time.time>=steamReady;
            bool thunder=game.Run.HasKeystone(7)&&game.Run.fireRelics>0&&game.Run.shockRelics>0&&Time.time>=thunderReady;
            if(steam){steamReady=Time.time+4;Blast(e,18+game.Run.weaponLevel*6,4,Color.cyan);}
            if(thunder){thunderReady=Time.time+5;Blast(e,26+game.Run.weaponLevel*7,5,Color.yellow);}
            if(chain){int n=0;foreach(var other in game.Enemies.ToArray())if(other&&other!=e&&Vector3.Distance(other.transform.position,e.transform.position)<9&&n<3){other.Hit(damage*.45f,false,false,false);game.Tracer(e.transform.position,other.transform.position,Color.cyan);n++;}}
        }
        void Blast(Enemy e,float damage,float radius,Color color)
        {
            game.Effect(e.transform.position,color,22,.14f);game.Audio.Cue("explosion");
            foreach(var other in game.Enemies.ToArray())if(other&&other!=e&&Vector3.Distance(other.transform.position,e.transform.position)<radius)other.Hit(damage,false,false,false);
            if(!e.dead)e.Hit(damage,false,false,false);
        }
    }
}

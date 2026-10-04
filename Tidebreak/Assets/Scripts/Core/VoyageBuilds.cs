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
            "命中积 1 点节拍，弱点积 2 点；满 6 点后下一枪 +85%。\n打空只扣 1 点；适合左轮与精准点射。",
            "累计 5 次有效命中产生蒸汽，范围伤害随改装成长。\n内置冷热装置；每 4 秒最多一次，适合对群。",
            "冲刺后 3 秒内伤害 +25%，装填加快 25%。\n适合贴近打完霰弹，再冲刺撤离。",
            "生命低于 35% 时伤害 +45%。\n所有受到的伤害 +12%；高风险残血流。",
            "每 6 次有效命中，对原目标追加 20% 伤害。\n再以 45% 伤害连锁附近 3 个敌人；不会递归。",
            "在冲刺无敌帧挡下攻击，下一枪伤害 +70%。\n同时回复 5 生命；每 3 秒最多触发一次。",
            "距目标超过 12 米时，弱点伤害额外 +35%。\n贴脸时无加成；适合鱼叉与精准射击。",
            "累计 7 次有效命中释放热雷，爆裂范围大于蒸汽。\n内置火雷装置；每 5 秒最多一次，适合密集敌群。",
            "弱点命中恢复 3 生命，每 2 秒一次。\n生命低于 35% 时，冲刺冷却缩短 20%。"
        };
        public static int Price(int act){return 110+act*90;}
        public static int Island(int act){return 2+act*3;}
        public static bool CanBuy(RunData run,int index)
        {int act=index/3;return index>=0&&index<9&&run.maxIsland>=Island(act)&&!run.HasActKeystone(act)&&run.coins>=Price(act);}
        public static void Apply(RunData run,int index)
        {run.keystoneMask|=1<<index;}
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
        AnglerController player;GameDirector game;int rhythm,hitCount,steamHits,thunderHits;float dashUntil,steamReady,thunderReady,healReady,counterReady;bool precision,counter;
        IslandMasteryCombat mastery;
        public int Rhythm {get{return rhythm;}}
        public int SteamHits {get{return steamHits;}}
        public int ThunderHits {get{return thunderHits;}}
        public string Status {get{
            if(counter)return "守潮反击 · 下一枪 +70%";
            if(precision)return "猎手节拍 · 下一枪 +85%";
            string status=game.Run.HasKeystone(0)?"节拍 "+rhythm+" / 6":"";
            if(game.Run.HasKeystone(1))status+="  蒸汽 "+steamHits+" / 5"+(Time.time<steamReady?" · 冷凝":"");
            if(game.Run.HasKeystone(7))status+="  热雷 "+thunderHits+" / 7"+(Time.time<thunderReady?" · 充电":"");
            if(Time.time<dashUntil&&game.Run.HasKeystone(2))status+="  踏浪增伤";
            if(game.Run.HasKeystone(3)&&game.Run.health<game.Run.MaxHealth*.35f)status+="  猎约 +45%";
            if(mastery!=null&&mastery.Status!="")status+="  "+mastery.Status;
            return status.Trim();
        }}
        public void Init(AnglerController p){player=p;game=p.game;mastery=new IslandMasteryCombat(p);}
        public void Reset(){rhythm=hitCount=steamHits=thunderHits=0;dashUntil=steamReady=thunderReady=healReady=counterReady=0;precision=counter=false;if(mastery!=null)mastery.Reset();}
        public void Dash(){dashUntil=Time.time+3;if(mastery!=null)mastery.Dash();}
        public float TargetDamageMultiplier(Enemy enemy,bool weak){return mastery!=null?mastery.TargetDamageMultiplier(enemy,weak):1;}
        public float IncomingDamageMultiplier(){return mastery!=null?mastery.IncomingDamageMultiplier():1;}
        public void ReloadCompleted(bool precise){if(mastery!=null)mastery.ReloadCompleted(precise);}
        public float ReloadSpeed {get{return Time.time<dashUntil&&game.Run.HasKeystone(2)?1.25f:1;}}
        public float ShotMultiplier()
        {
            float m=mastery!=null?mastery.ShotMultiplier():1;if(precision){m*=1.85f;precision=false;}if(counter){m*=1.7f;counter=false;}
            if(Time.time<dashUntil&&game.Run.HasKeystone(2))m*=1.25f;
            if(game.Run.HasKeystone(3)&&game.Run.health<game.Run.MaxHealth*.35f)m*=1.45f;return m;
        }
        public void AvoidedAttack()
        {
            if(mastery!=null)mastery.AvoidedAttack();
            if(!game.Run.HasKeystone(5)||Time.time<counterReady)return;counterReady=Time.time+3;counter=true;game.Run.health=Mathf.Min(game.Run.MaxHealth,game.Run.health+5);game.Notice("完美闪避 · 守潮反击已充能",2);
        }
        public void RegisterShot(bool hit,bool weak)
        {
            if(!game.Run.HasKeystone(0))return;
            if(!hit){rhythm=Mathf.Max(0,rhythm-1);return;}
            rhythm+=weak?2:1;
            if(rhythm>=6){rhythm=0;precision=true;game.Audio.Cue("ready");}
        }
        public void OnHit(Enemy e,bool weak,float damage)
        {
            if(mastery!=null)mastery.OnHit(e,weak,damage);
            if(weak&&game.Run.HasKeystone(8)&&Time.time>=healReady){healReady=Time.time+2;game.Run.health=Mathf.Min(game.Run.MaxHealth,game.Run.health+3);}
            hitCount++;bool chain=game.Run.HasKeystone(4)&&hitCount%6==0;
            if(game.Run.HasKeystone(1))steamHits=Mathf.Min(5,steamHits+1);
            if(game.Run.HasKeystone(7))thunderHits=Mathf.Min(7,thunderHits+1);
            bool steam=game.Run.HasKeystone(1)&&steamHits>=5&&Time.time>=steamReady;
            bool thunder=game.Run.HasKeystone(7)&&thunderHits>=7&&Time.time>=thunderReady;
            if(steam){steamHits=0;steamReady=Time.time+4;Blast(e,24+Mathf.Min(5,game.Run.weaponLevel)*6,4,Color.cyan);}
            if(thunder){thunderHits=0;thunderReady=Time.time+5;Blast(e,38+Mathf.Min(5,game.Run.weaponLevel)*7,5,Color.yellow);}
            if(chain){if(!e.dead)e.Hit(damage*.2f,false,false,false);int n=0;foreach(var other in game.Enemies.ToArray())if(other&&other!=e&&Vector3.Distance(other.transform.position,e.transform.position)<9&&n<3){other.Hit(damage*.45f,false,false,false);game.Tracer(e.transform.position,other.transform.position,Color.cyan);n++;}}
        }
        void Blast(Enemy e,float damage,float radius,Color color)
        {
            game.Effect(e.transform.position,color,22,.14f);game.Audio.Cue("explosion");
            foreach(var other in game.Enemies.ToArray())if(other&&other!=e&&Vector3.Distance(other.transform.position,e.transform.position)<radius)other.Hit(damage,false,false,false);
            if(!e.dead)e.Hit(damage,false,false,false);
        }
    }
}

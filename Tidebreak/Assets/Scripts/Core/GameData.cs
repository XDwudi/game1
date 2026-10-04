using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tidebreak
{
    public enum VoyageState { Harbor, Sailing, Fishing, Combat, Reward, Shop, Route, Victory, Defeat, Dialogue, Cinematic }
    public enum CreatureKind { Snapper, Puffer, Razorfin, Crab, Angler, Leviathan, Kraken, WhiteWhale }
    public enum WeaponKind { Revolver, Scattergun, Harpoon, Carbine, BurstRifle, ArcCaster }
    public enum RouteKind { Shoal, Hunt, Abyss }

    [Serializable]
    public partial class RunData
    {
        public int seed, stage = 1, coins = 25, kills, catches, bossKills, earned;
        public int landed, sold, shopMask;
        public bool bossCleared;
        public List<CatchData> bag=new List<CatchData>();
        public int BagValue {get {int n=0;if(bag!=null)foreach(var f in bag)n+=f.value;return n;}}
        public int Quota {get{return ExpeditionContent.Island(stage).quota;}}
        public bool RouteReady {get{return stage>=10?bossCleared:questStep>=4;}}
        public float health = 100, elapsed;
        public int weaponLevel, rodLevel, hullLevel, damageRelics, hasteRelics, criticalRelics;
        public int leechRelics, fortuneRelics, dodgeRelics, magazineRelics, shieldRelics;
        public bool shotgun, harpoon, abyssBait, krakenDefeated, whaleDefeated, rareSignal, easy;
        public RouteKind route;
        public int selectedWeapon;
        public int checkpointVersion = 3;
        public bool betweenEncounters;
        public float MaxHealth { get { return 100 + hullLevel * 25; } }
        public float DamageMultiplier { get { return 1 + .11f * Mathf.Min(5,weaponLevel) + .09f * Mathf.Min(4,damageRelics); } }
        public float CriticalChance { get { return Mathf.Min(.5f, .08f + .07f * criticalRelics); } }
        public float FireRateMultiplier { get { return 1 + Mathf.Min(4,hasteRelics) * .08f; } }
        public float DodgeCooldown { get { return Mathf.Max(1.6f, 3.5f - .35f * dodgeRelics); } }
        public float DamageTakenMultiplier { get { return (easy ? .65f : 1) * Mathf.Max(.65f, 1 - shieldRelics * .07f) * (HasKeystone(3)?1.12f:1); } }
        public int Act { get { return Mathf.Clamp((stage - 1) / 3, 0, 2); } }
        public bool BossStage { get { return questStep>=3 || stage>=10; } }
    }

    public struct WeaponSpec
    {
        public string name;
        public float damage, interval, reload, spread;
        public int magazine, pellets;
        public WeaponSpec(string n, float d, float i, float r, int m, int p, float s)
        { name=n; damage=d; interval=i; reload=r; magazine=m; pellets=p; spread=s; }
    }

    public static class Balance
    {
        public static readonly WeaponSpec[] Weapons = {
            new WeaponSpec("潮汐左轮", 19, .4f, 1.5f, 8, 1, .002f),
            new WeaponSpec("礁石霰弹枪", 7, .95f, 2.05f, 5, 7, .047f),
            new WeaponSpec("雷鸣鱼叉", 68, 1.12f, 2.4f, 3, 1, .0008f),
            new WeaponSpec("港卫卡宾枪", 9, .15f, 2.0f, 22, 1, .008f),
            new WeaponSpec("巡风三连发", 14, .15f, 2.1f, 18, 1, .003f),
            new WeaponSpec("风暴电弧枪", 31, .67f, 2.3f, 8, 1, .005f)
        };
        public static readonly string[] Seas = { "日光浅滩", "风暴群礁", "幽光深渊" };
        public static readonly string[] SeaCaptions = { "SUNLIT SHOALS", "TEMPEST REEF", "THE LUMINOUS DEEP" };
        public static int EnemyCount(int stage) { return 2 + Mathf.Min(3, (stage - 1) / 2); }
        public static float Health(CreatureKind kind, int stage, bool elite)
        {
            float basis = kind == CreatureKind.Snapper ? 45 : kind == CreatureKind.Puffer ? 65 : 80;
            if (kind == CreatureKind.Crab) return 750;
            if (kind == CreatureKind.Angler) return 1400;
            if (kind == CreatureKind.Leviathan) return 2200;
            if (kind == CreatureKind.Kraken) return 3300;
            if (kind == CreatureKind.WhiteWhale) return 2900;
            return basis * (1 + (stage - 1) * .11f) * (elite ? 1.8f : 1);
        }
        public static int Bounty(CreatureKind kind, int stage, bool elite)
        { return kind >= CreatureKind.Crab ? 150 + stage * 15 : (elite ? 52 : 26) + stage * 3; }
        public static string CreatureName(CreatureKind kind)
        {
            switch(kind) {
                case CreatureKind.Snapper: return "赤鳍凶鲷";
                case CreatureKind.Puffer: return "荆棘河豚";
                case CreatureKind.Razorfin: return "电刃猎鲨";
                case CreatureKind.Crab: return "铁壳领主";
                case CreatureKind.Angler: return "噬光灯笼鱼";
                case CreatureKind.Leviathan: return "风暴利维坦";
                case CreatureKind.Kraken: return "古神 · 克拉肯";
                default: return "幽海白鲸 · 莫比";
            }
        }
        public static int UpgradePrice(int basePrice, int level) { return basePrice + level * 40; }
        public static float ReelGain(bool holding, bool surge, int rod)
        { return holding ? (surge ? .075f : .2f) * (1 + rod * .24f) : -.018f; }
        public static float TensionGain(bool holding, bool surge, int rod)
        { return holding ? (surge ? .44f : .12f) / (1 + rod * .22f) : -.5f; }
    }

    [Serializable]
    public class CatchData
    {
        public CreatureKind kind;
        public int speciesId=-1;
        public int habitat=-1;
        public int caughtIsland=-1;
        public bool naturalHook;
        public bool fieldSample;
        public int value,quality;
        public float weight;
        public bool elite,airshot,weakshot;
        public string Label {get{return (quality==2?"金鳞 · ":quality==1?"巨型 · ":elite?"精英 · ":"")+(speciesId>=0?ExpeditionContent.Species[speciesId].name:Balance.CreatureName(kind));}}
    }

    public class Relic
    {
        public string id, name, description, category;
        public Color color;
        public Action<RunData> apply;
        public Relic(string i, string n, string d, string c, Color col, Action<RunData> a)
        { id=i; name=n; description=d; category=c; color=col; apply=a; }
        public bool AtCap(RunData r)
        {
            switch(id){case "fang":return r.damageRelics>=4;case "clock":return r.hasteRelics>=4;case "eye":return r.criticalRelics>=6;
                case "wind":return r.dodgeRelics>=6;case "ward":return r.shieldRelics>=5;case "steady":return r.brakeLevel>=4;
                case "stride":return r.bootsLevel>=4;case "hold":return r.bagLevel>=5;case "keen":return r.scopeLevel>=4;default:return false;}
        }
        public static readonly Relic[] All = {
            new Relic("fang", "鲨齿弹头", "武器伤害 +9%（最多 4 层）\n与枪械改造的伤害加成相加。", "火力", new Color(1,.52f,.32f), r=>r.damageRelics++),
            new Relic("clock", "潮汐发条", "射速 +8%（最多 4 层）\n缩短两次射击之间的空隙。", "火力", new Color(1,.73f,.32f), r=>r.hasteRelics++),
            new Relic("eye", "猎手之眼", "暴击率 +7%\n暴击造成 1.75 倍伤害，上限 50%。", "精准", new Color(.5f,.85f,1), r=>r.criticalRelics++),
            new Relic("coral", "生命珊瑚", "击杀恢复 3 点生命\n多次获得可叠加。", "生存", new Color(.4f,1,.74f), r=>r.leechRelics++),
            new Relic("pearl", "幸运黑珍珠", "战利品金币 +15%\n深渊也有自己的馈赠。", "财富", new Color(.83f,.65f,1), r=>r.fortuneRelics++),
            new Relic("wind", "乘风羽鳍", "冲刺冷却 -0.35 秒\n最低冷却 1.6 秒。", "机动", new Color(.45f,.88f,1), r=>r.dodgeRelics++),
            new Relic("shell", "无限螺壳", "弹匣容量 +2\n备用弹药无限，装填仍然重要。", "弹药", new Color(1,.81f,.5f), r=>r.magazineRelics++),
            new Relic("ward", "海神鳞片", "受到伤害 -7%\n最多减伤 35%。", "防护", new Color(.43f,.87f,.8f), r=>r.shieldRelics++),
            new Relic("burn", "熔盐弹头", "命中附加持续灼烧\n擅长消耗厚甲目标。", "元素", Color.red, r=>r.fireRelics++),
            new Relic("frost", "霜纹弹匣", "减缓移动与普通怪物出招\n首领专属招式需要破解机关。", "元素", Color.cyan, r=>r.iceRelics++),
            new Relic("shock", "蓄电导轨", "命中有概率打断普通怪物\n精英专属技能依照各自机制反制。", "元素", Color.yellow, r=>r.shockRelics++),
            new Relic("execute", "猎首刻印", "低血量敌人额外受到伤害\n加快战斗收尾。", "火力", Color.red, r=>r.executeRelics++),
            new Relic("stagger", "破甲锤针", "提高失衡伤害与击退\n攻击蓄势中的敌人。", "控制", Color.yellow, r=>r.staggerRelics++),
            new Relic("chain", "分叉线圈", "电弧枪额外连接目标\n强化对群作战。", "元素", Color.cyan, r=>r.chainLevel++),
            new Relic("steady", "船匠配重", "减少镜头后坐\n更稳地连续命中。", "操控", Color.gray, r=>r.brakeLevel++),
            new Relic("stride", "浪行绑腿", "移动速度提升\n缩短冰霜减速时间。", "机动", Color.green, r=>r.bootsLevel++),
            new Relic("hold", "渔网夹层", "鱼篓容量 +4\n单次探索携带更多鱼获。", "探索", Color.green, r=>r.bagLevel++),
            new Relic("keen", "鹰眼棱镜", "瞄准散布降低\n精确打击更远处的弱点。", "精准", Color.cyan, r=>r.scopeLevel++),
            new Relic("medical", "行医包", "获得 2 个随身急救包\nZ 使用，消耗后需再购买。", "补给", Color.green, r=>r.medkits+=2),
            new Relic("powder", "爆破袋", "获得 3 枚深水震爆弹\nX 投掷，消耗后需再购买。", "补给", Color.red, r=>r.bombs+=3)
        };
        public static Relic[] Roll(System.Random rng,int available=20)
        {
            var pool = new List<Relic>(All).GetRange(0,Math.Min(available,All.Length));
            var result = new Relic[3];
            for(int i=0;i<3;i++) { int index=rng.Next(pool.Count); result[i]=pool[index]; pool.RemoveAt(index); }
            return result;
        }
    }
}

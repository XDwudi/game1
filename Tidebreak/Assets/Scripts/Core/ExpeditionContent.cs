using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tidebreak
{
    public enum BodyFamily { Perch, Puffer, Eel, Ray, Crab, Jelly, Turtle, Squid, Swordfish, Seahorse, Urchin, Shark }
    public enum AttackStyle { Bite, Charge, Fan, Mortar, Ring, Beam, Mine, Leap, Spiral, Pull, Heal, Split, Boomerang, Burrow }
    public enum SeaTrait { None, Armored, Venom, Frost, Electric, Frenzy, Leech, Volatile, Blinker }
    [Serializable] public class IslandProgress
    {
        public int step,landed,explored,shopMask,eventMask;
        public bool boss,trophy;
        public List<int> caught=new List<int>();
    }
    public partial class RunData
    {
        public int questStep,exploredMask,eventMask,maxIsland=1,lureTier,selectedLure,bagLevel;
        public bool bossTrophy,carbine,burstRifle,arcCaster;
        public int medkits,bombs,frostbombs,sonarCharges,chum,tonics;
        public int brakeLevel,scopeLevel,bootsLevel,chainLevel,fireRelics,iceRelics,shockRelics,executeRelics,staggerRelics;
        public List<int> islandCaught=new List<int>();
        public IslandProgress[] islands=new IslandProgress[9];
        public int BagCapacity {get{return 8+bagLevel*4;}}
        public bool SamplesReady {get{return landed>=Quota&&islandCaught.Count>=Mathf.Min(2,Quota);}}
        public bool SurveyReady {get{return (exploredMask&3)==3;}}
        public bool Owns(WeaponKind w){return w==WeaponKind.Revolver||w==WeaponKind.Scattergun&&shotgun||w==WeaponKind.Harpoon&&harpoon||w==WeaponKind.Carbine&&carbine||w==WeaponKind.BurstRifle&&burstRifle||w==WeaponKind.ArcCaster&&arcCaster;}
    }
    public class IslandDefinition
    {
        public string name,english,npc,title,intro,samples,survey,conclusion,bossName,siteA,siteB,secret;
        public int quota,index;
        public Color ground,accent,sky;
        public Vector3 market,workshop,guide,site1,site2,site3;
        public IslandDefinition(int i,string n,string en,string person,string chapter,string opening,string catchText,string explore,string ending,string boss,string a,string b,string hidden,Color land,Color tint)
        {
            index=i;name=n;english=en;npc=person;title=chapter;intro=opening;samples=catchText;survey=explore;conclusion=ending;bossName=boss;siteA=a;siteB=b;secret=hidden;ground=land;accent=tint;sky=Color.Lerp(new Color(.24f,.47f,.63f),tint,.24f);quota=i<2?3:i<6?4:5;
            float side=i%2==0?1:-1;market=new Vector3(-16*side,0,-6-i%3*3);workshop=new Vector3(16*side,0,-14+i%3*3);guide=new Vector3((i%3-1)*5,0,-21);
            site1=new Vector3(-16+i%3*3,0,-25-i%2*5);site2=new Vector3(15-i%2*3,0,-30+i%3*3);site3=new Vector3((i%2==0?-1:1)*23,0,-3);
        }
    }
    public class SpeciesDefinition
    {
        public int id,island,lure;public string name,lore;
        public BodyFamily body;public AttackStyle attack;public SeaTrait trait;
        public Color color;public float hp,speed,size,tempo;public int value;public bool boss;
        public string Hint {get{return ExpeditionContent.Islands[island].name+" · "+ExpeditionContent.Lures[lure]+" · "+ExpeditionContent.AttackNames[(int)attack];}}
    }
    public static class ExpeditionContent
    {
        public static readonly string[] Lures={"海蚯蚓","甲壳拟饵","荧光拟饵","深渊拟饵"};
        public static readonly string[] AttackNames={"近身扑咬","蓄力冲撞","扇形水弹","延迟落点轰炸","扩散潮环","追踪蓄能射线","布设毒雷","腾跃砸击","旋转弹幕","旋涡牵引","愈合呼唤","分裂幼体","回旋水刃","潜地突袭"};
        public static readonly string[] Counters={"后退并瞄准嘴部","等冲锋锁定后横移","从弹道间隙穿过","离开橙色落点","跳过扩散水环","蓄力线锁定后冲刺","绕开绿色毒池","离开跃击阴影","利用地形间隙移动","先横移脱离涡心","及时打断治疗蓄势","先处理分裂幼体","留意回来的第二段水刃","看地面波纹，延迟闪避"};
        public static readonly string[] TraitNames={"普通","装甲：非弱点减伤","毒液：持续伤害","冰霜：短暂减速","导电：命中麻痹","狂热：半血加速","汲取：攻击回血","易爆：倒下留下预警","闪烁：交战时侧移"};
        public static readonly IslandDefinition[] Islands={
            new IslandDefinition(0,"松风灯塔","PINEHAVEN","守灯人 · 米罗","第一章：失去的归航灯","昨夜黑潮撞碎了归航灯。你的船还在，回家的航线却消失了。先替村里带回三份鱼获，我就告诉你灯塔熄灭的原因。","收集至少两种本地生物。样本会留在你的鱼篓，之后仍可售卖。","山坡电池箱和旧灯台之间断了供电。找齐两处航灯部件，再回来找我。","这块甲壳上也刻着潮纹！把它接在雷达上，第一条坐标出现了：珊瑚环礁。下一岛的工坊可以制造霰弹枪。","裂桨巨蟹","灯塔电池箱","旧灯台透镜","漂流船长的日记",new Color(.37f,.5f,.24f),new Color(.9f,.68f,.3f)),
            new IslandDefinition(1,"珊瑚环礁","CORAL CROWN","养珊人 · 露珂","第二章：不会歌唱的珊瑚","珊瑚停止了歌唱，鱼却长出了石头牙齿。替我采集环礁样本；我可以用它们追踪海底失踪的共鸣器。","甲壳拟饵能引出礁缝里的物种。新枪能对付近身怪鱼，但别离它们太近。","潮池里的共鸣贝与珊瑚拱门的音叉仍然有回应。把两处遗物都找回来。","共鸣器没有坏，是黑潮在发出相反的声音。继续向红树林追踪源头，卡宾枪的蓝图已经送到那里。","千齿珊瑚鳐","潮池共鸣贝","珊瑚音叉","沉没潜水员的箱子",new Color(.73f,.67f,.47f),new Color(.95f,.43f,.45f)),
            new IslandDefinition(2,"雾根红树林","MISTROOT","湿地向导 · 乌芦","第三章：会移动的根","昨晚树根走到了我的门口。拿样本来，我想知道是树在捕鱼，还是鱼在长根。","这里的毒鱼会留下毒池，长身鱼擅长冲刺。用快速火力打断它们的蓄势。","旧木桥的净水阀与树屋里的滤芯能清除沼泽毒气。请沿木栈道寻找。","净化的水里浮出一段旧航海日志：黑潮来自人造机器。盐沙遗迹有制造者的名字。","沼泽吞舟鳗","净水阀门","树屋滤芯","走私者的藏金罐",new Color(.27f,.4f,.23f),new Color(.53f,.75f,.32f)),
            new IslandDefinition(3,"盐沙遗迹","SALTGLASS","考古师 · 阿砂","第四章：埋在沙下的海","这片沙漠曾经是海床，墙上记载的却是未来的潮汐。我要用现存生物的鳞片校准那台古老仪器。","荧光拟饵已经能买到了。试着寻找普通鱼群之外的稀有样本。","方尖碑上的日轮和半埋神殿里的星盘缺一不可。小心守卫者的地下突袭。","碑文说：九枚潮核能关闭引潮机。但第一任船长把其中一枚带到了沉船墓地。","砂冠守陵龟","日轮石碑","沉沙星盘","被封存的藏宝图",new Color(.72f,.55f,.33f),new Color(.94f,.73f,.38f)),
            new IslandDefinition(4,"沉船墓地","WRECKWARD","潜水匠 · 洛恩","第五章：船没有忘记","这些船来自同一场风暴，年代却相差百年。带回鱼群样本，我能分辨哪个船舱还保留着那一夜的电流。","鱼叉能精准击破远处弱点。墓地中的装甲生物正适合练习。","北星号黑匣子和搁浅船钟记录着不同的时间。两件都需要带回来。","黑匣子里是你自己的求救声！引潮机把航路折成了环。去寒霜峡湾找能读懂时间的人。","沉钟铁甲章鱼","北星号黑匣子","搁浅船钟","海盗的秘密赌注",new Color(.34f,.39f,.36f),new Color(.53f,.73f,.76f)),
            new IslandDefinition(5,"寒霜峡湾","FROSTFJORD","冰海测绘员 · 伊芙","第六章：被冻结的昨天","冰层里封着九种不同的海。它们不是过去，是被困住的航路。给我这里的生物样本，我需要确定裂缝的位置。","冰霜攻击会短暂减速。防寒靴与热鱼汤能让你走得更远。","测温站的热芯与冰桥尽头的罗盘可以定位裂缝。别被移动的冰影引入海中。","罗盘指向雷暴岛。那里有引潮机最后一位工程师，也是唯一知道关闭代价的人。","霜脊独角鲸","测温站热芯","冰桥罗盘","雪下研究日志",new Color(.7f,.81f,.82f),new Color(.48f,.83f,.95f)),
            new IslandDefinition(6,"雷暴之巅","THUNDERHEAD","失踪工程师 · 赛因","第七章：谁制造了风暴","我造机器是为了让渔船平安归来。它却学会了把每一艘船留在安全的同一天。是我欠大海一条出口。","电弧武器能连接相邻目标。先别急着使用：导电生物也会反过来利用它。","去高处重启两座避雷阵列。旧机器会把维修者当成入侵者。","我把关闭指令交给你。熔潮火山可以锻造断潮钥，最后的机器在镜渊神庙下面。","天线风暴水母","西侧避雷阵列","东侧校准天线","工程师未寄出的信",new Color(.33f,.38f,.42f),new Color(.68f,.61f,.96f)),
            new IslandDefinition(7,"熔潮火山","CINDERWAKE","铸潮匠 · 赤岩","第八章：给海一把钥匙","普通钢铁无法切断时间。你需要一把能听见潮声的钥匙；这里的生物把潮声藏在骨头里。","炽热的怪物倒下后仍可能爆裂。别为了捡鱼站在预警圈内。","取回黑曜矿脉的钥坯与古熔炉的冷却印。两者合在一起才是断潮钥。","钥匙做好了。镜渊岛没有商人的笑声，只有等你回答的问题：你愿意失去这段安全的循环吗？","熔甲火山龙虾","黑曜钥坯","古熔炉冷却印","熔岩后的藏宝匣",new Color(.29f,.26f,.25f),new Color(1,.37f,.15f)),
            new IslandDefinition(8,"镜渊神庙","MIRRORDEEP","潮汐记录者 · 零","终章：最后一次归航","我保存每一次失去的航行。你寻找的家并不在下一座岛，而在你敢于结束的一天之后。带来最后一组样本，让机器记住真实的大海。","深渊拟饵能找齐最后的深渊物种。准备好补给，再面对守门者。","开启记忆棱镜与逆潮祭坛。它们会让你看见被困住的每一条航线。","潮汐重新向前流动。归航灯亮了，家就在地平线后。但深海仍有未被命名的巨物——克拉肯和白鲸正在醒来。","镜渊吞星者","记忆棱镜","逆潮祭坛","第一艘船的航海日志",new Color(.25f,.36f,.43f),new Color(.38f,.9f,.82f))
        };
        static readonly string[] Names={
            "铜须溪鲈|刺球河豚|条纹海鳗|滑翔银鳐|搬石蟹|玻璃水母|青苔海龟|墨点乌贼|针嘴梭鱼|卷尾海马|夜灯海胆|浅滩猎鲨",
            "胭脂鹦鲷|蜜刺箱鲀|珊瑚带鳗|折扇蝶鳐|红钳寄居蟹|桃心水母|瑰纹玳瑁|花冠章鱼|蓝针旗鱼|枝角海龙|宝石火胆|礁背角鲨",
            "泥鳍弹涂|孢子刺鲀|藤根电鳗|落叶鬼鳐|苔壳招潮蟹|沼灯海蜇|枯木鳄龟|毒囊短蛸|锯齿雀鳝|幽苔海马|菌绒棘胆|潜泥牛鲨",
            "琥珀石鲈|沙葬刺豚|银沙盲鳗|太阳盘鳐|圣甲沙蟹|浮沙水母|石纹象龟|铜壶乌贼|月刃剑鱼|金冠海龙|化石针胆|沙脊锯鲨",
            "铁锈船鲈|铆钉铁鲀|链尾海鳗|破帆幽鳐|锚爪巨蟹|瓶中灯母|炮甲海龟|船钟八腕|刺舷枪鱼|舵轮海马|罗盘磁胆|钢吻沉鲨",
            "雪鳍银鲈|霜刺冰鲀|极光长鳗|冰镜雪鳐|冻钳雪蟹|水晶冠母|冰盾棱龟|霜墨枪乌|冰锥剑鱼|雪鬃海马|寒针海胆|白牙格陵鲨",
            "雷纹电鲈|蓄电刺鲀|线圈雷鳗|翼雷魔鳐|磁钳机蟹|极电脉母|避雷甲龟|涡轮章鱼|风矛旗鱼|风暴海龙|闪弧星胆|裂空锤鲨",
            "炭鳞火鲈|硫磺焰鲀|熔线火鳗|灰烬翼鳐|煤甲岩蟹|熔核水母|玄武火龟|熔墨赤蛸|赤刃剑鱼|焰鬃海马|火晶爆胆|黑曜火鲨",
            "镜鳞幻鲈|星芒虚鲀|时隙长鳗|月幕梦鳐|星砂灵蟹|星河冠母|纪元古龟|夜幕八腕|断潮剑鱼|回声海龙|零点星胆|吞梦巨鲨"
        };
        public static readonly SpeciesDefinition[] Species=BuildSpecies();
        public static IslandDefinition Island(int stage){return Islands[Mathf.Clamp(stage-1,0,8)];}
        static SpeciesDefinition[] BuildSpecies()
        {
            var all=new List<SpeciesDefinition>();
            for(int island=0;island<9;island++){
                var names=Names[island].Split('|');
                for(int k=0;k<12;k++){
                    int attack=(k+island*3)%14;SeaTrait trait=island==0&&k<6?SeaTrait.None:(SeaTrait)((k+island)%9);
                    all.Add(new SpeciesDefinition{id=all.Count,island=island,name=names[k],body=(BodyFamily)k,attack=(AttackStyle)attack,trait=trait,lure=k<6?0:k<9?1:k<11?2:3,
                        color=Color.Lerp(Islands[island].accent,Color.HSVToRGB((k*.071f+island*.043f)%1,.48f,.72f),.52f),hp=(64+k*5)*(1+island*.19f),speed=3.1f+(k%4)*.55f,size=.7f+(k%3)*.13f+island*.016f,tempo=3.25f-(k%3)*.22f,value=25+island*7+k*2,
                        lore=Islands[island].name+"的"+new[]{"潮池居民","礁缝伏击者","浅海巡游者","夜间觅食者"}[k%4]+"。"+Counters[attack]+"；"+TraitNames[(int)trait]+"。"});
                }
            }
            BodyFamily[] forms={BodyFamily.Crab,BodyFamily.Ray,BodyFamily.Eel,BodyFamily.Turtle,BodyFamily.Squid,BodyFamily.Swordfish,BodyFamily.Jelly,BodyFamily.Crab,BodyFamily.Shark};
            AttackStyle[] styles={AttackStyle.Leap,AttackStyle.Ring,AttackStyle.Burrow,AttackStyle.Mortar,AttackStyle.Pull,AttackStyle.Boomerang,AttackStyle.Beam,AttackStyle.Mine,AttackStyle.Spiral};
            for(int i=0;i<11;i++)all.Add(new SpeciesDefinition{id=108+i,island=Mathf.Min(i,8),name=i<9?Islands[i].bossName:i==9?"古神 · 克拉肯":"幽海白鲸 · 莫比",body=i<9?forms[i]:i==9?BodyFamily.Squid:BodyFamily.Swordfish,attack=i<9?styles[i]:i==9?AttackStyle.Pull:AttackStyle.Fan,trait=i<9?(SeaTrait)(i%8+1):SeaTrait.None,boss=true,hp=i<9?820+i*340:i==9?6600:6100,speed=2,size=i<9?3.3f+i*.1f:5,tempo=5.2f,value=i<9?140+i*30:650,color=Islands[Mathf.Min(i,8)].accent,lore=BossNarrative.Teaching(108+i)});
            return all.ToArray();
        }
        public static SpeciesDefinition Roll(RunData run,System.Random random)
        {
            int start=Mathf.Clamp(run.stage-1,0,8)*12;var pool=new List<SpeciesDefinition>();
            for(int i=start;i<start+12;i++)if(Species[i].lure<=run.selectedLure)pool.Add(Species[i]);
            // Prefer an uncollected local species, so the main story never relies on repeated unlucky rolls.
            if(random.NextDouble()<.62){var fresh=pool.FindAll(s=>!run.islandCaught.Contains(s.id));if(fresh.Count>0)pool=fresh;}
            return pool[random.Next(pool.Count)];
        }
    }
}

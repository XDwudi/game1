using System;
using System.Collections.Generic;
namespace Tidebreak
{
    public class ShopOffer
    {
        public string id,name,description;public int category,island,cost,max;
        public Func<RunData,int> level;public Action<RunData> apply;
        public ShopOffer(string key,string title,string desc,int tab,int gate,int price,int limit,Func<RunData,int> read,Action<RunData> buy){id=key;name=title;description=desc;category=tab;island=gate;cost=price;max=limit;level=read;apply=buy;}
        public int Price(RunData r){return level(r)>=max?-1:cost+(max>1&&max<=5?level(r)*40:0);}
    }
    public static class ShopCatalog
    {
        public static readonly ShopOffer[] All={
            new ShopOffer("weapon","枪械改装","所有武器基础伤害 +11%\n升级上限随主线许可证提升",0,1,85,5,r=>r.weaponLevel,r=>r.weaponLevel++),
            new ShopOffer("shotgun","礁石霰弹枪","七发散射 · 近距离爆发\n按 3 切换",0,2,150,1,r=>r.shotgun?1:0,r=>r.shotgun=true),
            new ShopOffer("carbine","港卫卡宾枪","22 发弹匣 · 全自动\n按 5 切换；连续开火散布扩大",0,3,230,1,r=>r.carbine?1:0,r=>r.carbine=true),
            new ShopOffer("harpoon","雷鸣鱼叉","精确射击 · 机关伤害 +50%\n按 4 切换；破甲工具，3 发弹匣",0,4,300,1,r=>r.harpoon?1:0,r=>r.harpoon=true),
            new ShopOffer("burst","巡风三连发","一次扣扳机射出三发\n按 6 切换；控制爆发节奏",0,5,360,1,r=>r.burstRifle?1:0,r=>r.burstRifle=true),
            new ShopOffer("arc","风暴电弧枪","电流连接相邻目标\n按 7 切换；打断普通怪物蓄势",0,7,480,1,r=>r.arcCaster?1:0,r=>r.arcCaster=true),
            new ShopOffer("brake","枪口补偿器","每级减少 18% 镜头后坐\n所有武器通用",0,2,75,3,r=>r.brakeLevel,r=>r.brakeLevel++),
            new ShopOffer("scope","精密瞄具","每级减少瞄准散布\n提高远距离命中稳定性",0,4,95,2,r=>r.scopeLevel,r=>r.scopeLevel++),
            new ShopOffer("rod","钓具改装","更快收线，更稳控线\n高级鱼竿逐岛开放",1,1,65,3,r=>r.rodLevel,r=>r.rodLevel++),
            new ShopOffer("bag","扩容鱼篓","容量 +4 条鱼\n可以多探索一会再返回鱼市",1,1,70,3,r=>r.bagLevel,r=>r.bagLevel++),
            new ShopOffer("lure1","甲壳拟饵","本局解锁拟饵 I\n吸引每座岛的礁缝物种，B 切换",1,2,95,1,r=>r.lureTier>=1?1:0,r=>{r.lureTier=Math.Max(1,r.lureTier);r.selectedLure=1;}),
            new ShopOffer("lure2","荧光拟饵","本局解锁拟饵 II\n吸引夜行与深水物种，B 切换",1,4,190,1,r=>r.lureTier>=2?1:0,r=>{r.lureTier=Math.Max(2,r.lureTier);r.selectedLure=2;}),
            new ShopOffer("lure3","深渊拟饵","本局解锁拟饵 III\n可以钓到各岛最罕见的猎手",1,7,310,1,r=>r.lureTier>=3?1:0,r=>{r.lureTier=3;r.selectedLure=3;}),
            new ShopOffer("chum","诱鱼粉 ×3","下一次抛竿更快咬钩\n并提高金鳞变体出现机会",1,2,35,99,r=>r.chum/3,r=>r.chum+=3),
            new ShopOffer("bait","禁忌鱼饵","终章之后可唤醒克拉肯\n每次远征需重新购买",1,8,260,1,r=>r.abyssBait?1:0,r=>r.abyssBait=true),
            new ShopOffer("hull","防护背心","生命上限 +25\n购买时立即恢复 25 生命",2,1,90,3,r=>r.hullLevel,r=>{r.hullLevel++;r.health+=25;}),
            new ShopOffer("heal","热鱼汤","在工坊恢复 40 生命\n生命满时不可购买",2,1,25,99,r=>r.health>=r.MaxHealth?99:0,r=>r.health=UnityEngine.Mathf.Min(r.MaxHealth,r.health+40)),
            new ShopOffer("medkit","随身急救包","Z 使用，恢复 45 生命\n战斗中也能使用",2,2,45,9,r=>r.medkits,r=>r.medkits++),
            new ShopOffer("bomb","深水震爆弹","X 投掷，范围伤害与失衡\n飞行后延迟引爆",2,3,40,9,r=>r.bombs,r=>r.bombs++),
            new ShopOffer("frost","冰封瓶","V 投掷，减速附近怪物\n为装填或撤退创造窗口",2,5,50,9,r=>r.frostbombs,r=>r.frostbombs++),
            new ShopOffer("sonar","便携声呐","C 使用，标记附近探索地点\n已完成地点不会重复标记",2,2,25,9,r=>r.sonarCharges,r=>r.sonarCharges++),
            new ShopOffer("boots","防寒行靴","每级移速 +6%\n缩短冰霜减速时间",2,5,95,2,r=>r.bootsLevel,r=>r.bootsLevel++),
            new ShopOffer("tonic","乘风药剂","G 使用，12 秒冲刺冷却加快\n提高移速，适合探索与撤退",2,6,35,9,r=>r.tonics,r=>r.tonics++)
        };
        public static ShopOffer Find(string id){return Array.Find(All,x=>x.id==id);}
        public static string Lock(RunData r,ShopOffer offer)
        {
            if(r.maxIsland<offer.island)return "抵达 "+ExpeditionContent.Islands[offer.island-1].name+" 解锁";
            int cap=offer.id=="weapon"?Math.Min(5,1+(r.maxIsland-1)/2):offer.id=="rod"?Math.Min(3,1+(r.maxIsland-1)/3):3;
            if((offer.id=="weapon"||offer.id=="rod")&&offer.level(r)>=cap&&offer.level(r)<offer.max)return "后续岛屿开放下一等级";
            return "";
        }
    }
}

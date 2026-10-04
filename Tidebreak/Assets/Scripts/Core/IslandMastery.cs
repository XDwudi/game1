using System;
using UnityEngine;

namespace Tidebreak
{
    public partial class RunData
    {
        public int[] masteryLevels=new int[9];
        public int[] endemicRecords=new int[9];
        public int reloadLevel,handlingLevel,precisionReloadLevel,reelBearingLevel,salvageLevel,fieldDressingLevel;
        public float ReloadTraining {get{return 1+.08f*Mathf.Clamp(reloadLevel,0,3);}}
        public float HandlingRecovery {get{return 1+.2f*Mathf.Clamp(handlingLevel,0,3);}}
        public float PrecisionReloadPadding {get{return .025f*Mathf.Clamp(precisionReloadLevel,0,3);}}
        public float ReelRelease {get{return 1+.18f*Mathf.Clamp(reelBearingLevel,0,3);}}
        public float EliteSalvage {get{return 1+.1f*Mathf.Clamp(salvageLevel,0,3);}}
        public int MedkitRecovery {get{return 45+8*Mathf.Clamp(fieldDressingLevel,0,3);}}
    }

    // Run-scoped, purchased research. A specimen unlocks a blueprint; it never
    // grants a free combat rank and repeated kills cannot bypass exploration.
    public static class IslandMastery
    {
        public static readonly string[] Names={"松风追潮","珊瑚折射","根须萃取","日轮破势","沉钟快装","霜径回身","避雷蓄能","熔心处决","镜渊换弦"};
        public static void Ensure(RunData run)
        {
            if(run.masteryLevels==null)run.masteryLevels=new int[9];
            else if(run.masteryLevels.Length!=9)Array.Resize(ref run.masteryLevels,9);
            if(run.endemicRecords==null)run.endemicRecords=new int[9];
            else if(run.endemicRecords.Length!=9)Array.Resize(ref run.endemicRecords,9);
            for(int i=0;i<9;i++){run.masteryLevels[i]=Mathf.Clamp(run.masteryLevels[i],0,3);run.endemicRecords[i]&=3;}
        }
        public static int Level(RunData run,int island)
        {return run.masteryLevels!=null&&island>=0&&island<run.masteryLevels.Length?Mathf.Clamp(run.masteryLevels[island],0,3):0;}
        public static int SpecimenCount(RunData run,int island)
        {int n=run.endemicRecords!=null&&island>=0&&island<run.endemicRecords.Length?run.endemicRecords[island]:0;return (n&1)+((n>>1)&1);}
        public static int[] RequiredSpecimens(int island)
        {int start=Mathf.Clamp(island,0,8)*12;return new[]{start,start+5};}
        public static bool IsSpecimenKnown(RunData run,int speciesId)
        {
            if(speciesId<0||speciesId>=108)return false;int k=speciesId%12,island=speciesId/12;
            if(k!=0&&k!=5)return false;
            return run.endemicRecords!=null&&island<run.endemicRecords.Length&&(run.endemicRecords[island]&(k==0?1:2))!=0;
        }
        public static bool RecordNaturalKill(RunData run,SpeciesDefinition species,int caughtIsland,int habitat,bool natural,bool summoned)
        {
            if(!natural||summoned||species==null||species.boss||!species.endemic||species.id<0||species.id>=108||caughtIsland<0||caughtIsland>8||caughtIsland!=species.island||habitat!=species.habitat)return false;
            Ensure(run);int bit=species.id%12==0?1:2;
            if((run.endemicRecords[caughtIsland]&bit)!=0)return false;
            run.endemicRecords[caughtIsland]|=bit;return true;
        }
        public static int Price(RunData run,int island)
        {return Level(run,island)>=3?-1:55+Mathf.Clamp(island,0,8)*8+Level(run,island)*40;}
        public static string Lock(RunData run,int island)
        {
            if(island<0||island>8)return "无效专研";
            if(run.maxIsland<=island)return "抵达 "+ExpeditionContent.Islands[island].name+" 解锁";
            int rank=Level(run,island),known=SpecimenCount(run,island);
            if(rank>=3)return "";
            if(known==0)return "击败本岛任一种专属生物解锁";
            if(rank==1&&known<2)return "找到另一种专属生物 · 标本 "+known+" / 2";
            if(rank==2&&(known<2||run.habitatRecords==null||run.habitatRecords.Length<=island||run.habitatRecords[island]!=7))return "记录本岛三处钓场后开放 III 级";
            return "";
        }
        public static string UnlockHint(RunData run,int island)
        {
            island=Mathf.Clamp(island,0,8);
            foreach(int id in RequiredSpecimens(island))if(!IsSpecimenKnown(run,id)){
                var s=ExpeditionContent.Species[id];
                return "专属标本 "+SpecimenCount(run,island)+" / 2 · "+s.name+" / "+GameDirector.HabitatNames[s.habitat]+" / 海蚯蚓";
            }
            bool surveyed=run.habitatRecords!=null&&run.habitatRecords.Length>island&&run.habitatRecords[island]==7;
            return surveyed?"两份专属标本与三生境齐全 · 工坊可研究至 III 级":"两份专属标本齐全 · 完成三生境开放 III 级";
        }
        public static string Effect(int island,int rank)
        {
            rank=Mathf.Clamp(rank,0,3);if(rank==0)return "未研究";
            switch(island){
                case 0:return "冲刺后 3 秒首个弱点 +"+(6+rank*8)+"% · 冷却 5 秒";
                case 1:return "弱点折射到 6 米内另一目标："+(10+rank*10)+"% 伤害 · 冷却 2.5 秒";
                case 2:return "击败自然钓获恢复 "+(2+rank*2)+" 生命；召唤物无效";
                case 3:return "蓄势中的弱点 +"+(5+rank*10)+"% · 冷却 3 秒";
                case 4:return "精准装填后 5 秒下一枪 +"+(2+rank*8)+"%";
                case 5:return "冲刺减速 5 米内小怪 "+(.3f+rank*.25f).ToString("0.00")+" 秒 · 冷却 6 秒";
                case 6:return "完美闪避后 5 秒下一次受伤降低 "+(10+rank*10)+"%";
                case 7:return "30% 血量以下目标，弱点 +"+(10+rank*10)+"% · 冷却 4 秒";
                default:return "4 秒内换枪命中弱点 +"+(4+rank*6)+"% · 冷却 3 秒";
            }
        }
        public static string Description(RunData run,int island)
        {
            int rank=Level(run,island);string current=rank==0?"标本解锁蓝图，金币购买能力":"当前 "+Effect(island,rank);
            return current+"\n"+(rank>=3?"III 级已完成":("下一阶 "+Effect(island,rank+1)));
        }
        public static void Upgrade(RunData run,int island)
        {Ensure(run);if(island>=0&&island<9&&Lock(run,island)==""&&run.masteryLevels[island]<3)run.masteryLevels[island]++;}
    }

    public partial class GameDirector
    {
        public void OnNaturalKill(Enemy enemy)
        {
            if(!enemy||!enemy.dead||State!=VoyageState.Combat||!enemy.NaturalHook||enemy.Summoned||enemy.IsBoss||enemy.CaughtIsland<0||enemy.CaughtIsland>8||enemy.CaughtHabitat<0||enemy.CaughtHabitat>2)return;
            int roots=IslandMastery.Level(Run,2);
            if(roots>0)Run.health=Mathf.Min(Run.MaxHealth,Run.health+2+roots*2);
            if(!IslandMastery.RecordNaturalKill(Run,enemy.Spec,enemy.CaughtIsland,enemy.CaughtHabitat,enemy.NaturalHook,enemy.Summoned))return;
            Notice("专属标本已解析 · "+enemy.Spec.name+"\n"+IslandMastery.Names[enemy.CaughtIsland]+" · "+IslandMastery.SpecimenCount(Run,enemy.CaughtIsland)+" / 2 · 到工坊用金币研究",6);
            Audio.Cue("discovery");
        }
    }
}

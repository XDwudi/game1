using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tidebreak
{
    public partial class RunData
    {
        public int[] habitatRecords = new int[9];
        public int researchRewards;
    }

    // A catch remembers where it was hooked. Moving between coasts changes the
    // available ecology; rewards are for observation, not for grinding kills.
    public partial class GameDirector
    {
        int hookedHabitat;
        public static readonly string[] HabitatNames = { "西岸礁隙", "湾心水道", "东岸海草" };
        public static readonly string[] HabitatClues = {
            "礁隙有甲壳与短鳍鱼；沿西侧水纹抛竿。",
            "水道有刺球与游猎者；长抛越过码头前端。",
            "海草藏着细长与发光生物；转向东侧浮叶。"
        };
        public static int HabitatAt(Vector3 point) { return point.x < -9 ? 0 : point.x > 9 ? 2 : 1; }
        public int ResearchRecord
        {
            get { EnsureResearch(); return Run.habitatRecords[Mathf.Clamp(Run.stage - 1, 0, 8)]; }
        }
        public int ResearchCount
        {
            get { int n = ResearchRecord; return (n & 1) + ((n >> 1) & 1) + ((n >> 2) & 1); }
        }
        public string ResearchHint
        {
            get {
                int record = ResearchRecord;
                if ((Run.researchRewards & (1 << Mathf.Clamp(Run.stage - 1, 0, 8))) != 0)
                    return "本岛生态笔记已完成 · 升级拟饵可重访寻找深层物种";
                for (int i = 0; i < 3; i++) if ((record & (1 << i)) == 0)
                    return "生态笔记 " + ResearchCount + "/3 · " + HabitatClues[i];
                return "三处钓场已记录 · 生态笔记已收录";
            }
        }
        public string AimedHabitat
        {
            get {
                Vector3 d = Player.View.transform.forward; d.y = 0;if(d.sqrMagnitude<.1f)d=Player.transform.forward;
                Vector3 point = Player.transform.position + d.normalized * Mathf.Lerp(13, 31, CastCharge);
                if(!World.IsWater(point)||World.GroundAt(point)>-.3f)return "面向开阔海面 · 码头前端适合抛竿";
                int h = HabitatAt(point);
                return HabitatNames[h] + ((ResearchRecord & (1 << h)) == 0 ? " · 尚未记录" : " · 已记录")
                    + " / " + ExpeditionContent.Lures[Run.selectedLure];
            }
        }
        void EnsureResearch()
        {
            if (Run.habitatRecords == null) Run.habitatRecords = new int[9];
            else if (Run.habitatRecords.Length != 9) Array.Resize(ref Run.habitatRecords, 9);
        }
        SpeciesDefinition RollCoastalCatch()
        {
            hookedHabitat = HabitatAt(castPoint);
            int start = Mathf.Clamp(Run.stage - 1, 0, 8) * 12;
            var pool = new List<SpeciesDefinition>();
            // Each family has a habitat. At the basic lure there are two species
            // in every habitat; later lures reveal additional silhouettes there.
            for (int k = 0; k < 12; k++) {
                var s = ExpeditionContent.Species[start + k];
                if (k % 3 == hookedHabitat && s.lure <= Run.selectedLure) pool.Add(s);
            }
            var fresh = pool.FindAll(s => !Run.islandCaught.Contains(s.id));
            if (fresh.Count > 0 && Rng.NextDouble() < .8) pool = fresh;
            return pool[Rng.Next(pool.Count)];
        }
        void RecordHabitat(CatchData item)
        {
            if (!item.fieldSample || item.habitat < 0 || item.habitat > 2 || item.speciesId < 0 || item.speciesId >= 108) return;
            EnsureResearch();
            int island = item.speciesId / 12, bit = 1 << item.habitat;
            if ((Run.habitatRecords[island] & bit) != 0) return;
            Run.habitatRecords[island] |= bit;
            if (Run.habitatRecords[island] == 7 && (Run.researchRewards & (1 << island)) == 0) {
                Run.researchRewards |= 1 << island;
                const int stipend = 35;
                Run.coins += stipend; Run.earned += stipend;
                Audio.Cue("discovery");
                Notice("生态笔记完成 · 三处钓场已记录 · 研究津贴 +35 金币", 5);
            } else Notice("记录钓场：" + HabitatNames[item.habitat] + " · " + ResearchCount + "/3 · J 查看生态笔记", 4);
        }
    }
}

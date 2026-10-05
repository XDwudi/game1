using UnityEngine;

namespace Tidebreak
{
    // Theme is explicit gameplay data, never inferred from a telegraph's warning colour.
    public enum SkillTheme { Tide, Coral, VenomRoot, SaltCrystal, Wraith, Frost, Lightning, Cinder, Mirror, Ink, WhaleSong }

    public static class SkillThemes
    {
        static readonly SkillTheme[] IslandThemes={SkillTheme.Tide,SkillTheme.Coral,SkillTheme.VenomRoot,SkillTheme.SaltCrystal,SkillTheme.Wraith,SkillTheme.Frost,SkillTheme.Lightning,SkillTheme.Cinder,SkillTheme.Mirror};
        public static SkillTheme ForSpecies(SpeciesDefinition species)
        {
            if(species==null)return SkillTheme.Tide;
            if(species.id==117)return SkillTheme.Ink;
            if(species.id==118)return SkillTheme.WhaleSong;
            if(species.boss)return IslandThemes[Mathf.Clamp(species.island,0,8)];
            switch(species.trait){
                case SeaTrait.Venom:return SkillTheme.VenomRoot;
                case SeaTrait.Frost:return SkillTheme.Frost;
                case SeaTrait.Electric:return SkillTheme.Lightning;
                case SeaTrait.Volatile:return SkillTheme.Cinder;
                case SeaTrait.Blinker:return SkillTheme.Mirror;
            }
            return ForEcology(species.ecology);
        }
        public static SkillTheme ForEcology(EcologyStyle ecology){return IslandThemes[Mathf.Clamp((int)ecology,0,8)];}
        public static SkillTheme Resolve(Enemy source,SkillTheme? theme=null){return theme??(source?ForSpecies(source.Spec):SkillTheme.Tide);}
        public static Color ColorOf(SkillTheme theme)
        {
            switch(theme){
                case SkillTheme.Coral:return new Color(1,.43f,.48f);
                case SkillTheme.VenomRoot:return new Color(.55f,.85f,.17f);
                case SkillTheme.SaltCrystal:return new Color(1,.77f,.38f);
                case SkillTheme.Wraith:return new Color(.34f,.86f,.69f);
                case SkillTheme.Frost:return new Color(.52f,.85f,1);
                case SkillTheme.Lightning:return new Color(.63f,.68f,1);
                case SkillTheme.Cinder:return new Color(1,.3f,.075f);
                case SkillTheme.Mirror:return new Color(.8f,.93f,1);
                case SkillTheme.Ink:return new Color(.48f,.19f,.68f);
                case SkillTheme.WhaleSong:return new Color(.58f,1,.96f);
                default:return new Color(.27f,.77f,.83f);
            }
        }
        public static string Signature(SkillTheme theme)
        {
            switch(theme){
                case SkillTheme.Coral:return "branching coral fans / drifting polyps";
                case SkillTheme.VenomRoot:return "coiling thorn roots / rising spores";
                case SkillTheme.SaltCrystal:return "amber rhombohedral prisms / sand splinters";
                case SkillTheme.Wraith:return "tattered spectral sails / lantern wisps";
                case SkillTheme.Frost:return "six-sided ice spears / suspended snow";
                case SkillTheme.Lightning:return "forked descending lightning / ground arcs";
                case SkillTheme.Cinder:return "twisting flame columns / rising embers and ash";
                case SkillTheme.Mirror:return "paired mirror blades / counter-rotating shards";
                case SkillTheme.Ink:return "curling sucker arms / dark ink plumes";
                case SkillTheme.WhaleSong:return "standing sonar ribs / double acoustic wavefront";
                default:return "breaking sea crests / falling white foam";
            }
        }
    }
}

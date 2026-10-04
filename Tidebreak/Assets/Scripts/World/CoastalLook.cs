using UnityEngine;

namespace Tidebreak
{
    // Deliberate light/colour relationships: cool open sea, warm inhabited shores.
    public static class CoastalLook
    {
        public static void Apply(SeaWorld world,Light sun,Material sky)
        {
            int region=world.Region;bool night=region>=6;
            Color deep,crest,zenith,horizon;
            if(region==0){deep=new Color(.018f,.15f,.21f);crest=new Color(.16f,.52f,.48f);zenith=new Color(.08f,.3f,.53f);horizon=new Color(.48f,.7f,.76f);}
            else if(region==1){deep=new Color(.018f,.2f,.27f);crest=new Color(.21f,.66f,.58f);zenith=new Color(.06f,.35f,.59f);horizon=new Color(.52f,.77f,.81f);}
            else if(region==2){deep=new Color(.035f,.12f,.14f);crest=new Color(.2f,.38f,.29f);zenith=new Color(.13f,.27f,.31f);horizon=new Color(.46f,.59f,.5f);}
            else if(region==3){deep=new Color(.025f,.19f,.24f);crest=new Color(.26f,.58f,.51f);zenith=new Color(.17f,.35f,.55f);horizon=new Color(.75f,.69f,.53f);}
            else if(region==4){deep=new Color(.025f,.105f,.15f);crest=new Color(.2f,.39f,.43f);zenith=new Color(.095f,.19f,.29f);horizon=new Color(.42f,.54f,.61f);}
            else if(region==5){deep=new Color(.025f,.14f,.23f);crest=new Color(.43f,.7f,.76f);zenith=new Color(.15f,.35f,.53f);horizon=new Color(.65f,.78f,.84f);}
            else if(region==6){deep=new Color(.02f,.065f,.125f);crest=new Color(.22f,.29f,.49f);zenith=new Color(.055f,.07f,.15f);horizon=new Color(.21f,.27f,.41f);}
            else if(region==7){deep=new Color(.08f,.055f,.07f);crest=new Color(.43f,.2f,.11f);zenith=new Color(.12f,.075f,.1f);horizon=new Color(.42f,.24f,.21f);}
            else{deep=new Color(.015f,.095f,.145f);crest=new Color(.13f,.43f,.43f);zenith=new Color(.045f,.075f,.15f);horizon=new Color(.19f,.35f,.4f);}
            world.Ocean.SetColor("_DeepColor",deep);world.Ocean.SetColor("_CrestColor",crest);
            sky.SetColor("_SkyTint",zenith);sky.SetColor("_Horizon",horizon);
            sky.SetColor("_Cloud",night?new Color(.38f,.42f,.53f):new Color(.94f,.96f,.94f));
            sky.SetFloat("_Exposure",night?.88f:1.02f);sky.SetFloat("_CloudCover",region==2||region==4||region==6?.65f:.35f);
            RenderSettings.fogColor=horizon;RenderSettings.fogDensity=region==2?.0055f:.0037f;
            RenderSettings.ambientSkyColor=night?new Color(.28f,.36f,.49f):new Color(.48f,.64f,.74f);
            RenderSettings.ambientEquatorColor=night?new Color(.2f,.25f,.34f):new Color(.29f,.39f,.41f);
            RenderSettings.ambientGroundColor=night?new Color(.12f,.13f,.18f):new Color(.19f,.21f,.16f);
            sun.intensity=night?.9f:region==2||region==4?1.05f:1.38f;
            sun.color=region==3||region==7?new Color(1,.78f,.57f):region==5?new Color(.9f,.96f,1):new Color(1,.95f,.84f);
            sun.shadowStrength=.86f;sun.shadowNormalBias=.22f;
        }
    }
}

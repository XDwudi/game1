using System.Collections;
using System.Linq;
using UnityEngine;

namespace Tidebreak
{
    public partial class SmokePilot
    {
        IEnumerator V05SoundAndRestoration()
        {
            g.StartVoyage();Time.timeScale=v05Speed;
            Check(g.Audio.LoadedCueCount==33,"audio imports all 33 authored effects");
            Check(g.Audio.LoadedThemeCount==8,"audio imports all eight music and percussion tracks");
            Check(g.Audio.MissingImportedCueCount==0,"audio has zero missing imported effect fallbacks");
            g.SetState(VoyageState.Dialogue);yield return new WaitForSecondsRealtime(.4f);
            Check(g.Audio.MusicDuck<.7f,"story conversation ducks music so its mechanism and dialogue space stays clear");g.CloseDialogue();
            for(int island=1;island<=9;island++){
                g.StartVoyage();g.Run.maxIsland=9;if(island>1){g.ShowMap();g.Travel(island);}yield return new WaitForSecondsRealtime(.6f);
                var restoration=g.World.GetComponent<IslandRestoration>();
                Check(restoration&&restoration.VisibleLevel==0&&restoration.DisplayRoot&&restoration.DisplayRoot.gameObject.activeInHierarchy,"island "+island+" begins with its unrepaired physical consequence layer");
                if(!restoration)continue;
                g.Run.questStep=3;g.Checkpoint(false);yield return new WaitForSecondsRealtime(.6f);
                Check(restoration.VisibleLevel==1&&restoration.DisplayRoot.childCount>0,"island "+island+" completed investigation changes its visible world state");
                g.Run.bossCleared=true;g.Run.questStep=4;g.Checkpoint(false);yield return new WaitForSecondsRealtime(.6f);
                Check(restoration.VisibleLevel==2,"island "+island+" boss victory advances the world to its released state");
                if(island==3||island==6||island==9){
                    Vector3 focus=island==6?g.World.QuestPoint+new Vector3(2.2f,1.2f,.25f):island==9?new Vector3(0,1,34):g.SitePoint(1)+new Vector3(3.7f,1,-1);
                    At(island==9?new Vector3(0,0,22):focus+Vector3.back*6,focus);yield return null;Capture("restored-island-"+island);
                }
                if(island==6)Check(restoration.DisplayRoot.GetComponentsInChildren<Transform>().Any(t=>t.name.StartsWith("A-Lan /")),"rescue outcome adds the living passenger with her ticket beside the guide");
                g.ReturnHarbor();g.StartVoyage(true);yield return new WaitForSecondsRealtime(.6f);restoration=g.World.GetComponent<IslandRestoration>();
                Check(restoration&&restoration.VisibleLevel==2,"island "+island+" restoration survives a real checkpoint reload");
            }
            g.Run.rareSignal=true;g.Run.abyssBait=true;g.Run.krakenDefeated=false;g.ShowMap();g.ChallengeKraken();yield return new WaitForSecondsRealtime(.6f);
            var rare=g.World.GetComponent<IslandRestoration>();
            Check(g.Run.stage==10&&rare&&(!rare.DisplayRoot||!rare.DisplayRoot.gameObject.activeInHierarchy),"legendary arena disables borrowed mirror-island consequence props");
            g.StartVoyage();yield return null;
        }
    }
}

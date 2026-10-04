using System.Collections;
using System.Linq;
using UnityEngine;
namespace Tidebreak
{
    public partial class SmokePilot
    {
        IEnumerator LegendaryCounterContracts()
        {
            Time.timeScale=1;
            var kraken=RevisionCreateBoss(117,false);
            // Direct damage here isolates the breakable-to-anatomy contract. The
            // following full boss battle separately requires actual player fire.
            kraken.Hit(1000000,true,false,false);yield return null;
            var brain=kraken.Encounter;int before=brain.KrakenSlamCount;
            brain.Targets[0].Hit(1000000);yield return new WaitForSeconds(.5f);
            var arm=kraken.transform.GetChild(0).Find("Tentacle 1");
            Check(brain.SeveredArms==1&&brain.KrakenSlamCount==before-1,
                "breaking a landed Kraken arm immediately removes one subsequent slam");
            Check(arm&&arm.localScale.x<.5f,
                "the corresponding living Kraken arm visibly retracts after its shore target breaks");
            foreach(var target in brain.Targets.ToArray())target.Hit(1000000);
            Check(brain.KrakenSlamCount==2,"Kraken retains at least two readable counterattacks after all arms are severed");
            kraken.Hit(1000000,true,false,false);yield return new WaitForSeconds(.5f);
            Check(brain.Phase==3&&brain.SeveredArms==0&&brain.KrakenSlamCount==5&&arm.localScale.x>.85f,
                "the final Kraken phase visibly regrows arms and restores its full pressure pattern");
            for(int trial=0;trial<2;trial++){
                g.StartVoyage();yield return null;At(new Vector3(0,0,5));g.SetState(VoyageState.Combat);
                Vector3 origin=g.Player.transform.position+Vector3.forward*12;
                TrackingBreach.Create(g,origin,24);float health=g.Run.health;
                yield return new WaitForSeconds(1.3f);
                Check(g.Run.health==health,"whale tracking lane cannot damage the captain before its visible lock finishes");
                if(trial==1)g.Player.Motor.Move(Vector3.right*5);
                yield return new WaitForSeconds(.8f);
                Check(trial==0?g.Run.health<health:g.Run.health==health,trial==0?
                    "locked whale lane damages a captain who stays in its path":"moving sideways after whale lock avoids its actual hit volume");
            }
            g.StartVoyage();yield return null;
        }
    }
}

using System.Collections;
using System.IO;
using UnityEngine;

namespace Tidebreak
{
    public partial class SmokePilot
    {
        // Synthetic visual fixtures only. They exercise the rendering/input-facing
        // APIs but are not evidence of natural combat, progression or player skill.
        IEnumerator V06PresentationContracts()
        {
            float previousScale=Time.timeScale;Time.timeScale=1;
            File.WriteAllLines(Path.Combine(output,"v06-presentation-fixtures.txt"),new[]{
                "Visual-only synthetic fixtures; not natural combat or balance passes.",
                "Each resolution: real pier camera, normal DeckWarning geometry/countdown, explicit damage source, explicit weak-hit HUD marker, real Fire and Reload APIs.",
                "The weak-hit number is a HUD fixture, not claimed enemy damage. Test loadout grants carbine ownership.",
                "Temporary warnings, health changes and loadout are discarded by StartVoyage before the existing presentation suite."});
            foreach(var size in new[]{new Vector2Int(1600,900),new Vector2Int(1280,720)}){
                Screen.SetResolution(size.x,size.y,false);yield return new WaitForSecondsRealtime(.2f);
                string suffix=size.x+"x"+size.y;
                g.StartVoyage();g.Run.carbine=true;g.Player.Equip(WeaponKind.Revolver);g.Player.Refill();
                g.SetState(VoyageState.Combat);
                Vector3 strike=new Vector3(0,0,19);strike.y=g.World.GroundAt(strike)+.1f;
                At(new Vector3(0,0,14),strike);
                g.Warn(strike,2.8f,3.2f,10);
                yield return new WaitForSeconds(.48f);yield return new WaitForEndOfFrame();
                CapturePresentation("v06-ground-warning-full-range-"+suffix,size.x,size.y);

                // Inside the same radius, the danger banner and leftward wound arc
                // are rendered together from the ordinary captain camera.
                At(new Vector3(0,0,17),strike+Vector3.forward);
                g.Player.InvulnerableUntil=0;
                g.Player.TakeDamage(8,g.Player.transform.position-g.Player.transform.right*5);
                yield return null;yield return new WaitForEndOfFrame();
                CapturePresentation("v06-danger-and-left-damage-"+suffix,size.x,size.y);
                foreach(var warning in g.Hazards.GetComponentsInChildren<DeckWarning>())Destroy(warning.gameObject);
                yield return new WaitForSeconds(.62f);

                At(new Vector3(0,0,18),new Vector3(0,2,36));
                g.UI.HitMarker(true,42);
                yield return null;yield return new WaitForEndOfFrame();
                CapturePresentation("v06-weak-hit-confirmation-"+suffix,size.x,size.y);

                g.Player.Equip(WeaponKind.Carbine);g.Player.Refill();
                yield return new WaitForSeconds(.4f);
                g.Player.Fire();yield return new WaitForEndOfFrame();
                CapturePresentation("v06-carbine-firing-"+suffix,size.x,size.y);
                yield return new WaitForSeconds(.14f);g.Player.Reload();
                float deadline=Time.realtimeSinceStartup+4;
                while(g.Player.Reloading&&g.Player.ReloadProgress<.46f&&Time.realtimeSinceStartup<deadline)yield return null;
                yield return new WaitForEndOfFrame();
                CapturePresentation("v06-carbine-magazine-withdrawn-"+suffix,size.x,size.y);

                g.Player.Equip(WeaponKind.Revolver);g.Player.Refill();
                yield return new WaitForSeconds(.4f);g.Player.Fire();
                yield return new WaitForSeconds(.14f);g.Player.Reload();
                deadline=Time.realtimeSinceStartup+4;
                while(g.Player.Reloading&&g.Player.ReloadProgress<.46f&&Time.realtimeSinceStartup<deadline)yield return null;
                yield return new WaitForEndOfFrame();
                CapturePresentation("v06-revolver-open-cylinder-"+suffix,size.x,size.y);
                g.StartVoyage();yield return null;
            }
            Time.timeScale=previousScale;
        }
    }
}

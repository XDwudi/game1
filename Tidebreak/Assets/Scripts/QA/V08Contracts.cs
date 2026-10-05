using System;
using System.Collections;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Tidebreak
{
    public partial class SmokePilot
    {
        void V08TargetHud(EncounterTarget target)
        {
            Canvas.ForceUpdateCanvases();
            var label=FindObjectsOfType<TMP_Text>().FirstOrDefault(t=>t.isActiveAndEnabled&&t.text.StartsWith("可破坏物 · ")&&t.text.Contains(target.Label));
            bool readable=false;
            if(label){
                label.ForceMeshUpdate();
                readable=label.textInfo.lineCount==2&&!label.isTextOverflowing&&label.rectTransform.rect.height>=label.preferredHeight-1&&
                    label.textInfo.characterInfo.Take(label.textInfo.characterCount).Where(c=>!char.IsWhiteSpace(c.character)).All(c=>c.isVisible);
            }
            Check(readable,"v08 aiming at "+target.Label+" displays the actual two-line target label and health without clipping");
        }
        IEnumerator V08Contracts()
        {
            output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Artifacts/V08QA",V05Arg("-v08Run","manual")));
            Directory.CreateDirectory(output);Time.timeScale=1;v05Speed=1;
            string mode=V05Arg("-v08Mode","All");
            File.WriteAllText(Path.Combine(output,"scope.txt"),"v0.8 automated integration fixtures. Boss counter tests use disclosed encounter/loadout fixtures; full finite-health battles and natural opening progression run in separate V05 suites. Effects screenshots are staged in the real Unity renderer. Display tests change actual Screen mode asynchronously. These are not human usability or completion-quality certifications.\n");
            if(mode=="All"||mode=="Bosses")yield return V08BossContracts();
            if(mode=="All"||mode=="Effects")yield return V08EffectsContracts();
            if(mode=="All"||mode=="Display")yield return V08DisplayContracts();
            Time.timeScale=1;
            yield return null;
        }
    }
}

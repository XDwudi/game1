using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Tidebreak
{
    public partial class SmokePilot
    {
        IEnumerator V05WhaleEchoVisibility(BossMechanism mechanism)
        {
            // The caller has walked/placed its isolated fixture at the real sonar
            // station and pressed the shared action. Do not reveal the solution
            // through CurrentTargetIndex: inspect all three displayed projections.
            yield return new WaitForSeconds(.12f);
            var echoes=mechanism.Nodes.Where(n=>n.Kind=="echo").ToArray();
            Vector3 center=Vector3.zero;foreach(var echo in echoes)center+=echo.Position;
            if(echoes.Length>0)g.Player.AimAt(center/echoes.Length);
            yield return null;yield return new WaitForEndOfFrame();
            Check(mechanism.StateStep==1&&echoes.Length==3&&echoes.All(e=>e.Root.gameObject.activeInHierarchy),
                "whale phase "+mechanism.Phase+" normal sonar action displays all three echo alternatives");
            var evidence=new List<string>{"phase,echo,ring,samples,terrain_clear,in_viewport,min_ground_clearance"};
            int totalRings=0;
            for(int echoIndex=0;echoIndex<echoes.Length;echoIndex++){
                var rings=echoes[echoIndex].Root.GetComponentsInChildren<MeshFilter>().Where(f=>f.name=="Forged ring"&&f.GetComponent<Renderer>()&&f.GetComponent<Renderer>().enabled).ToArray();
                totalRings+=rings.Length;
                for(int ringIndex=0;ringIndex<rings.Length;ringIndex++){
                    var mesh=rings[ringIndex];var vertices=mesh.sharedMesh.vertices;
                    int sampled=0,clear=0,inView=0;float clearance=float.PositiveInfinity;
                    for(int i=0;i<vertices.Length;i+=Mathf.Max(1,vertices.Length/48)){
                        Vector3 p=mesh.transform.TransformPoint(vertices[i]);Vector3 eye=g.Player.View.transform.position;
                        sampled++;if(!Physics.Linecast(eye,p,SeaWorld.GroundMask,QueryTriggerInteraction.Ignore))clear++;
                        Vector3 view=g.Player.View.WorldToViewportPoint(p);if(view.z>0&&view.x>0&&view.x<1&&view.y>0&&view.y<1)inView++;
                        clearance=Mathf.Min(clearance,p.y-g.World.GroundAt(p));
                    }
                    evidence.Add(mechanism.Phase+","+echoIndex+","+ringIndex+","+sampled+","+clear+","+inView+","+F(clearance));
                    Check(sampled>0&&clear>=sampled*.9f&&inView==sampled&&clearance>0,
                        "whale phase "+mechanism.Phase+" echo "+echoIndex+" ring "+ringIndex+" is readable from sonar station above terrain: "+clear+"/"+sampled+" clear, "+inView+"/"+sampled+" in view");
                }
            }
            Check(totalRings==4,"whale phase "+mechanism.Phase+" displays one double-ring signal and two single-ring decoys");
            File.WriteAllLines(Path.Combine(output,"whale-sonar-visibility-phase-"+mechanism.Phase+".csv"),evidence);
            Capture("whale-sonar-from-station-phase-"+mechanism.Phase);
        }

        void V05RenderedColour(Renderer renderer,Color expected,string label)
        {
            Color actual=Color.clear;
            if(renderer){var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);actual=!block.isEmpty?block.GetColor("_Color"):renderer.sharedMaterial&&renderer.sharedMaterial.HasProperty("_Color")?renderer.sharedMaterial.GetColor("_Color"):Color.clear;}
            Check(renderer&&Mathf.Abs(actual.r-expected.r)<.015f&&Mathf.Abs(actual.g-expected.g)<.015f&&Mathf.Abs(actual.b-expected.b)<.015f&&Mathf.Abs(actual.a-expected.a)<.015f,
                label+"; effective shader _Color "+actual.ToString());
        }

        void V05KrakenRenderedColours(BossMechanism mechanism)
        {
            var guides=FindObjectsOfType<LineRenderer>().Where(l=>l.transform.parent&&l.transform.parent.name=="Boss mechanism 117 phase "+mechanism.Phase).ToArray();
            string context="Kraken phase "+mechanism.Phase+(mechanism.Surge?" red tide":" stable reeling");
            V05RenderedColour(guides.FirstOrDefault(l=>!l.loop),mechanism.Surge?new Color(1,.27f,.18f):new Color(.2f,.88f,.88f),context+" tether changes the rendered material colour");
            V05RenderedColour(guides.FirstOrDefault(l=>l.loop),mechanism.Braced?new Color(.38f,.96f,.55f):new Color(1,.72f,.25f),context+" brace circle changes the rendered material colour");
        }

        IEnumerator V05BreachRenderedColours()
        {
            yield return new WaitForSeconds(.12f);
            var breach=FindObjectsOfType<TrackingBreach>().FirstOrDefault(b=>!b.Locked&&b.Remaining>.5f);
            Check(breach,"sonar selection creates a real unlocked blue breach warning");
            if(!breach)yield break;
            foreach(var line in breach.GetComponentsInChildren<LineRenderer>())V05RenderedColour(line,new Color(.25f,.8f,1),"unlocked breach "+line.name+" has an actual blue material");
            float begin=Time.time;while(breach&&!breach.Locked&&Time.time-begin<1.5f)yield return null;
            Check(breach&&breach.Locked&&breach.Remaining>0,"breach changes to its locked warning before the real impact");
            if(breach)foreach(var line in breach.GetComponentsInChildren<LineRenderer>())V05RenderedColour(line,new Color(1,.45f,.15f),"locked breach "+line.name+" has an actual orange material");
        }
    }
}

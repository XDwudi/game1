using System.Collections.Generic;
using UnityEngine;

namespace Tidebreak
{
    // Three recognizable coast silhouettes teach where a cast will land. All
    // elements are decorative and remain outside the walkable arena.
    public static class CoastalHabitatArt
    {
        public static Transform Build(Transform parent,int island)
        {
            var root=new GameObject("Observable coastal habitats").transform;root.SetParent(parent,false);
            var stage=ExpeditionContent.Island(island);Color stone=Color.Lerp(new Color(.23f,.35f,.37f),stage.accent,.2f);
            Color brass=new Color(.69f,.49f,.22f),wood=new Color(.3f,.23f,.17f),foam=new Color(.66f,.84f,.81f);
            var motion=root.gameObject.AddComponent<CoastalHabitatMotion>();
            var west=Group(root,"West / reef crevices",new Vector3(-20,0,30));
            for(int i=0;i<7;i++){
                float a=i*2.39f;Vector3 at=new Vector3(Mathf.Cos(a)*(1.3f+i*.23f),-.5f,Mathf.Sin(a)*(1.3f+i*.25f));
                Shape.Rock(west,at,new Vector3(1.6f,.75f+(i%3)*.28f,1.3f),Color.Lerp(stone,foam,i*.035f),6);
                if(i%2==0){var shell=Shape.Part("Tide shell",PrimitiveType.Sphere,west,at+new Vector3(.15f,.72f,0),new Vector3(.48f,.15f,.34f),new Color(.82f,.63f,.42f));shell.transform.localRotation=Quaternion.Euler(0,i*53,17);}
            }
            Beacon(west,new Vector3(2.8f,.15f,1),1,wood,brass,foam,motion);
            var mid=Group(root,"Center / current channel",new Vector3(0,0,33));
            Beacon(mid,new Vector3(-4,.12f,0),2,wood,brass,foam,motion);Beacon(mid,new Vector3(4,.12f,2),2,wood,brass,foam,motion);
            for(int ribbon=0;ribbon<3;ribbon++){
                var g=new GameObject("Narrow current foam");g.transform.SetParent(mid,false);g.transform.localPosition=new Vector3((ribbon-1)*1.7f,.11f,0);
                var line=g.AddComponent<LineRenderer>();line.sharedMaterial=Shape.Mat(Color.Lerp(foam,stage.accent,.16f));line.useWorldSpace=false;line.positionCount=15;line.startWidth=.05f;line.endWidth=.015f;
                for(int i=0;i<15;i++)line.SetPosition(i,new Vector3(Mathf.Sin(i*.65f+ribbon)*.36f,0,-5+i*.82f));
                motion.Foam.Add(line);
            }
            var east=Group(root,"East / seagrass meadow",new Vector3(20,0,30));
            var grass=island==6?new Color(.32f,.55f,.56f):island==8?new Color(.4f,.33f,.19f):new Color(.19f,.42f,.3f);
            for(int i=0;i<13;i++){
                float a=i*2.39f;Vector3 at=new Vector3(Mathf.Cos(a)*(1+i*.15f),.06f,Mathf.Sin(a)*(1+i*.17f));
                var blade=Group(east,"Floating kelp blade",at);blade.localRotation=Quaternion.Euler(0,i*43,0);
                Shape.Beam(blade,Vector3.down*.3f,new Vector3(.15f,.12f,.4f),.06f,grass);
                var leaf=Shape.Part("Broad kelp leaf",PrimitiveType.Sphere,blade,new Vector3(.1f,.04f,.32f),new Vector3(.65f,.055f,1.15f),Color.Lerp(grass,foam,(i%3)*.1f));
                leaf.transform.localRotation=Quaternion.Euler(0,22,0);motion.Leaves.Add(blade);
            }
            Beacon(east,new Vector3(-2.8f,.13f,.4f),3,wood,brass,foam,motion);
            // Fish silhouettes create an observable difference before any menus.
            for(int h=0;h<3;h++)for(int i=0;i<4;i++){
                var school=Group(root,"Coastal silver wake",new Vector3((h-1)*20+(i-1.5f)*.75f,.13f,31+i*.65f));
                var fish=Shape.MeshObject("Dorsal silhouette",school,new[]{new Vector3(-.05f,0,-.18f),new Vector3(.05f,0,-.18f),new Vector3(0,.18f,.1f),new Vector3(0,0,.29f)},new[]{0,2,1,0,3,2,1,2,3},Color.Lerp(stone,foam,.3f));
                fish.transform.localScale=Vector3.one*(.65f+i*.13f);motion.Schools.Add(school);
            }
            return root;
        }
        static Transform Group(Transform parent,string name,Vector3 at)
        {var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=at;return t;}
        static void Beacon(Transform parent,Vector3 at,int marks,Color wood,Color brass,Color foam,CoastalHabitatMotion motion)
        {
            var buoy=Group(parent,"Chart buoy / "+marks+" tide marks",at);
            Shape.Part("Weathered float",PrimitiveType.Cylinder,buoy,Vector3.zero,new Vector3(.66f,.13f,.66f),wood).GetComponent<Renderer>().sharedMaterial=Shape.Wood(wood);
            Shape.Part("Brass rim",PrimitiveType.Cylinder,buoy,new Vector3(0,.16f,0),new Vector3(.69f,.045f,.69f),brass).GetComponent<Renderer>().sharedMaterial=Shape.Metal(brass);
            Shape.Beam(buoy,new Vector3(0,.17f,0),new Vector3(0,1.75f,0),.045f,wood);
            Shape.MeshObject("Sea chart pennant",buoy,new[]{new Vector3(.02f,1.7f,0),new Vector3(.7f,1.47f,0),new Vector3(.02f,1.2f,0)},new[]{0,1,2,2,1,0},foam);
            for(int i=0;i<marks;i++)Shape.Part("Readable habitat tally",PrimitiveType.Cube,buoy,new Vector3(.12f+i*.09f,1.46f,-.013f),new Vector3(.025f,.13f,.018f),wood);
            motion.Buoys.Add(buoy);
        }
    }
    public class CoastalHabitatMotion : MonoBehaviour
    {
        public readonly List<Transform> Buoys=new List<Transform>(),Leaves=new List<Transform>(),Schools=new List<Transform>();
        public readonly List<LineRenderer> Foam=new List<LineRenderer>();
        readonly List<Vector3> buoyOrigins=new List<Vector3>(),leafOrigins=new List<Vector3>(),schoolOrigins=new List<Vector3>();
        void Start(){foreach(var t in Buoys)buoyOrigins.Add(t.localPosition);foreach(var t in Leaves)leafOrigins.Add(t.localPosition);foreach(var t in Schools)schoolOrigins.Add(t.localPosition);}
        void Update()
        {
            float time=Time.time;
            for(int i=0;i<Buoys.Count;i++){var t=Buoys[i];t.localPosition=buoyOrigins[i]+Vector3.up*Mathf.Sin(time*1.2f+i)*.06f;t.localRotation=Quaternion.Euler(Mathf.Sin(time*.7f+i)*3,0,Mathf.Cos(time*.9f+i)*2);}
            for(int i=0;i<Leaves.Count;i++){var t=Leaves[i];t.localPosition=leafOrigins[i]+new Vector3(Mathf.Sin(time*.6f+i)*.1f,Mathf.Cos(time*.8f+i)*.018f,0);}
            for(int i=0;i<Schools.Count;i++){var t=Schools[i];float phase=time*.36f+i*.9f;t.localPosition=schoolOrigins[i]+new Vector3(Mathf.Sin(phase)*1.5f,Mathf.Sin(time*1.8f+i)*.035f,Mathf.Cos(phase)*1.1f);t.localRotation=Quaternion.LookRotation(new Vector3(Mathf.Cos(phase)*1.5f,0,-Mathf.Sin(phase)*1.1f));}
            for(int i=0;i<Foam.Count;i++){float value=.025f+.02f*(.5f+.5f*Mathf.Sin(time+i));Foam[i].startWidth=value;}
        }
    }
}

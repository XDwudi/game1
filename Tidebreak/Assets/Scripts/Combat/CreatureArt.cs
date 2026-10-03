using UnityEngine;

namespace Tidebreak
{
    public static class CreatureArt
    {
        public static Transform Build(Transform parent, CreatureKind kind, bool elite)
        {
            var rig=new GameObject("Creature silhouette").transform; rig.SetParent(parent,false);
            var coral=new Color(.95f,.33f,.23f); var teal=new Color(.15f,.63f,.65f); var dark=new Color(.09f,.15f,.23f);
            Color c=kind==CreatureKind.Snapper?coral:kind==CreatureKind.Puffer?new Color(.89f,.65f,.21f):teal;
            if(elite)c=new Color(.7f,.28f,.72f);
            if(kind==CreatureKind.Kraken)
            {
                c=new Color(.32f,.2f,.51f);
                Shape.Rock(rig,new Vector3(0,1.3f,0),new Vector3(2.4f,3.4f,2.3f),c,12);
                Shape.Part("Mantle",PrimitiveType.Sphere,rig,Vector3.zero,new Vector3(4.5f,3.8f,4),c,true);
                Eyes(rig,1.35f,.5f,1.55f,1);
                for(int i=0;i<8;i++) {
                    var arm=new GameObject("Tentacle "+i).transform; arm.SetParent(rig,false); arm.localRotation=Quaternion.Euler(0,i*45,0);
                    var a=new Vector3(1.5f,-.4f,0);
                    for(int j=0;j<5;j++) {
                        var b=new Vector3(2+j*.83f,-.65f+Mathf.Sin(j*.8f)*1.9f,Mathf.Sin(j*.55f)*.7f);
                        Shape.Beam(arm,a,b,.85f-j*.14f,c);
                        Shape.Part("Sucker",PrimitiveType.Sphere,arm,b+Vector3.forward*.17f,Vector3.one*(.3f-j*.035f),new Color(.68f,.48f,.7f));
                        a=b;
                    }
                }
                WeakPoint(rig,new Vector3(0,.25f,2),.8f);
            }
            else if(kind==CreatureKind.Crab)
            {
                c=new Color(.84f,.32f,.15f);
                Shape.Rock(rig,Vector3.zero,new Vector3(2.5f,1.4f,1.85f),c,10);
                Shape.Part("Armored body",PrimitiveType.Sphere,rig,Vector3.zero,new Vector3(4.4f,2.1f,3),c,true);
                for(int s=-1;s<=1;s+=2) {
                    for(int i=0;i<3;i++) {
                        var a=new Vector3(s*1.5f,-.2f,-.9f+i*.75f); var b=new Vector3(s*3,-.4f,-1.8f+i*1.3f);
                        Shape.Beam(rig,a,b,.23f,c); Shape.Beam(rig,b,b+new Vector3(s*.4f,-1.1f,.3f),.16f,c);
                    }
                    Shape.Beam(rig,new Vector3(s*1.7f,.1f,1),new Vector3(s*3,.5f,2.8f),.45f,c);
                    Shape.Rock(rig,new Vector3(s*3,.6f,3),new Vector3(.8f,.8f,1.2f),new Color(.96f,.49f,.23f));
                    Shape.Beam(rig,new Vector3(s*.8f,.7f,.9f),new Vector3(s*.8f,1.7f,1.1f),.17f,c);
                }
                Eyes(rig,.8f,1.7f,1.1f,.45f); WeakPoint(rig,new Vector3(0,.45f,1.55f),.65f);
            }
            else
            {
                bool boss=kind>=CreatureKind.Angler;
                float size=boss?2.6f:1;
                if(kind==CreatureKind.Angler)c=new Color(.17f,.3f,.44f);
                if(kind==CreatureKind.Leviathan)c=new Color(.14f,.43f,.5f);
                if(kind==CreatureKind.WhiteWhale)c=new Color(.83f,.88f,.81f);
                var fish=new GameObject("Fish anatomy").transform; fish.SetParent(rig,false); fish.localScale=Vector3.one*size;
                Shape.Part("Body",PrimitiveType.Sphere,fish,Vector3.zero,new Vector3(1.5f,1.5f,kind==CreatureKind.Puffer?1.5f:2.6f),c,true);
                Shape.Part("Belly",PrimitiveType.Sphere,fish,new Vector3(0,-.25f,.2f),new Vector3(1.35f,.8f,2),Color.Lerp(c,Color.white,.48f));
                Shape.MeshObject("Tail fin",fish,new[]{new Vector3(0,0,-1),new Vector3(-1,.7f,-2),new Vector3(1,.7f,-2),new Vector3(0,-.3f,-1.7f)},new[]{0,1,2,0,3,1,0,2,3},Color.Lerp(c,dark,.25f));
                Shape.MeshObject("Dorsal fin",fish,new[]{new Vector3(0,.4f,.5f),new Vector3(0,1.65f,-.25f),new Vector3(0,.4f,-1)},new[]{0,1,2,2,1,0},Color.Lerp(c,dark,.4f));
                for(int s=-1;s<=1;s+=2)
                    Shape.MeshObject("Pectoral fin",fish,new[]{new Vector3(s*.5f,0,.3f),new Vector3(s*1.5f,-.2f,-.4f),new Vector3(s*.5f,-.3f,-.8f)},new[]{0,1,2,2,1,0},c);
                Eyes(fish,.51f,.3f,.9f,.42f);
                Shape.Part("Mouth",PrimitiveType.Sphere,fish,new Vector3(0,-.17f,1.21f),new Vector3(.9f,.65f,.25f),dark);
                for(int i=0;i<5;i++) {
                    float x=(i-2)*.14f;
                    Shape.MeshObject("Tooth",fish,new[]{new Vector3(x-.06f,.09f,1.35f),new Vector3(x+.06f,.09f,1.35f),new Vector3(x,-.18f,1.4f)},new[]{0,1,2,2,1,0},new Color(1,.95f,.8f));
                }
                if(kind==CreatureKind.Puffer || elite) for(int i=0;i<12;i++) {
                    float a=i*Mathf.PI*2/12;
                    var p=new Vector3(Mathf.Cos(a)*.8f,Mathf.Sin(a)*.8f,0);
                    Shape.Beam(fish,p,p*1.4f,.11f,new Color(1,.84f,.4f));
                }
                if(kind==CreatureKind.Angler) {
                    Shape.Beam(fish,new Vector3(0,.6f,.2f),new Vector3(0,1.8f,.4f),.07f,c);
                    Shape.Beam(fish,new Vector3(0,1.8f,.4f),new Vector3(0,1.8f,1.4f),.07f,c);
                    Shape.Part("Lure",PrimitiveType.Sphere,fish,new Vector3(0,1.55f,1.4f),Vector3.one*.45f,new Color(.3f,1,.84f),false,true);
                }
                if(kind==CreatureKind.WhiteWhale) {
                    fish.localScale=new Vector3(3.2f,2.5f,3.8f);
                    Shape.Beam(fish,new Vector3(0,.35f,1),new Vector3(0,.45f,2.9f),.065f,new Color(.48f,1,1),true);
                    Shape.MeshObject("Moon flukes",fish,new[]{new Vector3(0,0,-1.4f),new Vector3(-1.9f,0,-2.6f),new Vector3(0,.2f,-2.2f),new Vector3(1.9f,0,-2.6f)},new[]{0,1,2,0,2,3,2,1,0,3,2,0},c);
                }
                WeakPoint(fish,new Vector3(0,.05f,1.4f),boss?.3f:.21f);
            }
            return rig;
        }
        static void Eyes(Transform p,float x,float y,float z,float size)
        {
            for(int s=-1;s<=1;s+=2) {
                Shape.Part("Eye",PrimitiveType.Sphere,p,new Vector3(s*x,y,z),Vector3.one*size,new Color(1,.84f,.41f),false,true);
                Shape.Part("Pupil",PrimitiveType.Sphere,p,new Vector3(s*x,y,z+size*.4f),new Vector3(size*.22f,size*.7f,size*.3f),new Color(.035f,.075f,.1f));
            }
        }
        static void WeakPoint(Transform p,Vector3 pos,float radius)
        {
            var g=Shape.Part("Glowing weak point",PrimitiveType.Sphere,p,pos,Vector3.one*radius*2,new Color(.45f,1,.83f),true,true);
            g.AddComponent<HitRegion>();
        }
    }
    public class HitRegion : MonoBehaviour { }
}

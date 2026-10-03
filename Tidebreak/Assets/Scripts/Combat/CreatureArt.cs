using UnityEngine;

namespace Tidebreak
{
    public static class CreatureArt
    {
        public static Transform Build(Transform parent,CreatureKind kind,bool elite)
        {
            var rig=new GameObject("Creature anatomy").transform;rig.SetParent(parent,false);
            if(kind==CreatureKind.Kraken)Kraken(rig);
            else if(kind==CreatureKind.Crab)Crab(rig);
            else Fish(rig,kind,elite);
            return rig;
        }
        static void Fish(Transform rig,CreatureKind kind,bool elite)
        {
            Color back=kind==CreatureKind.Snapper?new Color(.66f,.27f,.14f):kind==CreatureKind.Puffer?new Color(.43f,.48f,.2f):new Color(.17f,.34f,.37f);
            if(kind==CreatureKind.Angler)back=new Color(.19f,.27f,.29f);if(kind==CreatureKind.WhiteWhale)back=new Color(.7f,.76f,.73f);if(elite)back=new Color(.42f,.24f,.49f);
            Color belly=Color.Lerp(back,new Color(.92f,.87f,.64f),.65f),fin=back*.72f;fin.a=1;
            var root=new GameObject("Fish body").transform;root.SetParent(rig,false);
            bool boss=kind>=CreatureKind.Angler;root.localScale=Vector3.one*(boss?3.1f:.8f);
            if(kind==CreatureKind.WhiteWhale)root.localScale=new Vector3(3.7f,3.1f,4.3f);
            bool puffer=kind==CreatureKind.Puffer;
            float[] z={-1.43f,-1.16f,-.87f,-.38f,.15f,.58f,.9f,1.1f,1.17f};
            float[] radius={.045f,.12f,.31f,.48f,.51f,.46f,.31f,.17f,.015f};
            var m=new CoastalMesh();const int sides=14;
            for(int k=0;k<z.Length-1;k++)for(int j=0;j<sides;j++) {
                float a=j*Mathf.PI*2/sides,b=(j+1)*Mathf.PI*2/sides;
                float squash=puffer?1.3f:1;
                var aa=new Vector3(Mathf.Cos(a)*radius[k]*squash,Mathf.Sin(a)*radius[k],z[k]);
                var ab=new Vector3(Mathf.Cos(b)*radius[k]*squash,Mathf.Sin(b)*radius[k],z[k]);
                var ba=new Vector3(Mathf.Cos(a)*radius[k+1]*squash,Mathf.Sin(a)*radius[k+1],z[k+1]);
                var bb=new Vector3(Mathf.Cos(b)*radius[k+1]*squash,Mathf.Sin(b)*radius[k+1],z[k+1]);
                Color color=Color.Lerp(belly,back,Mathf.Clamp01((Mathf.Sin(a)+.35f)*1.05f));
                if(j%7==0)color=Color.Lerp(color,new Color(.87f,.66f,.31f),.4f);
                if(k%2==0&&j<7)color*=.91f;
                color.a=1;m.Quad(aa,ab,bb,ba,color);
            }
            var body=m.Build("Faceted scales",root);var col=body.AddComponent<CapsuleCollider>();col.direction=2;col.radius=.49f;col.height=2.4f;col.center=new Vector3(0,0,-.1f);
            var tail=new GameObject("Tail fin").transform;tail.SetParent(root,false);tail.localPosition=new Vector3(0,0,-1.22f);
            Fin(tail,new[]{Vector3.zero,new Vector3(0,.66f,-.83f),new Vector3(0,.15f,-.71f),new Vector3(0,0,-.53f),new Vector3(0,-.53f,-.84f),new Vector3(0,-.09f,-.36f)},fin);
            Fin(root,new[]{new Vector3(0,.3f,-.96f),new Vector3(0,.68f,-.83f),new Vector3(0,.99f,-.31f),new Vector3(0,.75f,.15f),new Vector3(0,.45f,.48f)},fin);
            for(int s=-1;s<=1;s+=2) {
                Fin(root,new[]{new Vector3(s*.36f,-.08f,.38f),new Vector3(s*.94f,-.32f,-.22f),new Vector3(s*.84f,-.39f,-.66f),new Vector3(s*.37f,-.19f,-.23f)},fin);
                Shape.Part("Silver eye",PrimitiveType.Sphere,root,new Vector3(s*.32f,.14f,.78f),new Vector3(.19f,.2f,.13f),new Color(.91f,.83f,.55f));
                Shape.Part("Dark pupil",PrimitiveType.Sphere,root,new Vector3(s*.38f,.14f,.824f),new Vector3(.09f,.115f,.068f),new Color(.024f,.035f,.028f));
                var gills=new Vector3[]{new Vector3(s*.4f,.21f,.56f),new Vector3(s*.45f,.01f,.48f),new Vector3(s*.38f,-.24f,.48f)};
                CoastalMesh.Tube("Gill slit",root,gills,new[]{.012f,.017f,.008f},fin,fin,5);
            }
            Shape.Part("Mouth",PrimitiveType.Sphere,root,new Vector3(0,-.055f,1.1f),new Vector3(.26f,.16f,.05f),new Color(.1f,.09f,.075f));
            if(puffer)for(int i=0;i<22;i++){float a=i*2.39996f,y=-.7f+1.4f*i/22;Vector3 dir=new Vector3(Mathf.Cos(a)*Mathf.Sqrt(1-y*y),y,Mathf.Sin(a)*Mathf.Sqrt(1-y*y));CoastalMesh.Tube("Puffer spine",root,new[]{dir*.46f,dir*.71f},new[]{.043f,0},belly,back,5);}
            if(kind==CreatureKind.Angler){var pts=new[]{new Vector3(0,.42f,.2f),new Vector3(0,1.02f,.38f),new Vector3(0,1.35f,.94f),new Vector3(0,1.15f,1.36f)};CoastalMesh.Tube("Angler lure",root,pts,new[]{.045f,.037f,.026f,.015f},back,back,7);Shape.Part("Bioluminescent lure",PrimitiveType.Sphere,root,pts[3],Vector3.one*.25f,new Color(.4f,.95f,.62f),false,true);}
            if(kind==CreatureKind.Leviathan)for(int i=0;i<6;i++)CoastalMesh.Tube("Storm spine",root,new[]{new Vector3(0,.4f,-.9f+i*.27f),new Vector3(0,.94f,-1.1f+i*.27f)},new[]{.12f,0},new Color(.37f,.75f,.75f),fin,5);
            if(kind==CreatureKind.WhiteWhale)Fin(root,new[]{new Vector3(0,0,-1.25f),new Vector3(-1.25f,.05f,-1.94f),new Vector3(-.42f,0,-2),new Vector3(0,0,-1.5f),new Vector3(.42f,0,-2),new Vector3(1.25f,.05f,-1.94f)},back);
            WeakPoint(root,new Vector3(0,.17f,1.075f),boss?.17f:.095f);
        }
        static void Fin(Transform p,Vector3[] outline,Color c)
        {
            var m=new CoastalMesh();for(int i=1;i<outline.Length-1;i++){m.Tri(outline[0],outline[i],outline[i+1],c);m.Tri(outline[0],outline[i+1],outline[i],c*.85f);}m.Build("Webbed fin",p);
            for(int i=1;i<outline.Length;i++)Shape.Beam(p,outline[0],outline[i],.007f,Color.Lerp(c,Color.white,.15f));
        }
        static void Kraken(Transform rig)
        {
            Color skin=new Color(.31f,.18f,.32f),light=new Color(.6f,.37f,.39f);
            CoastalMesh.Tube("Tapered mantle",rig,new[]{new Vector3(0,-1,0),Vector3.zero,new Vector3(0,1.5f,-.3f),new Vector3(0,3.7f,-.8f),new Vector3(0,5,-1.2f)},new[]{.8f,2.1f,2.35f,1.5f,0},skin,light,16);
            var body=rig.gameObject.AddComponent<SphereCollider>();body.radius=2.3f;body.center=Vector3.up*1.3f;
            for(int s=-1;s<=1;s+=2){Shape.Part("Old gold eye",PrimitiveType.Sphere,rig,new Vector3(s*1.4f,.72f,1.6f),new Vector3(.86f,.68f,.28f),new Color(.83f,.59f,.19f));Shape.Part("Slit pupil",PrimitiveType.Sphere,rig,new Vector3(s*1.4f,.72f,1.75f),new Vector3(.2f,.58f,.09f),new Color(.04f,.02f,.04f));}
            for(int i=0;i<8;i++) {
                var arm=new GameObject("Tentacle "+i).transform;arm.SetParent(rig,false);arm.localRotation=Quaternion.Euler(0,i*45,0);
                var points=new Vector3[12];var radii=new float[12];
                for(int j=0;j<12;j++){float t=j/11f;points[j]=new Vector3(1.2f+t*6.6f,-.7f+Mathf.Sin(t*4)*1.5f,t*t*1.6f);radii[j]=Mathf.Lerp(.65f,.035f,t);}
                CoastalMesh.Tube("Continuous curling arm",arm,points,radii,skin,light,9);
                for(int j=1;j<10;j++)for(int s=-1;s<=1;s+=2){float t=j/11f;CoastalMesh.Ring(arm,points[j]+new Vector3(0,.12f,s*radii[j]*.7f),radii[j]*.37f,.045f,light,Quaternion.Euler(70,0,0));}
            }
            WeakPoint(rig,new Vector3(0,.1f,1.85f),.48f);
        }
        static void Crab(Transform rig)
        {
            Color shell=new Color(.56f,.25f,.13f),rim=new Color(.83f,.45f,.21f);
            Shape.Rock(rig,new Vector3(0,.25f,0),new Vector3(2.25f,1.35f,1.65f),shell,12);
            var c=rig.gameObject.AddComponent<BoxCollider>();c.size=new Vector3(4.2f,1.6f,3.1f);c.center=Vector3.up*.35f;
            for(int s=-1;s<=1;s+=2) {
                for(int i=0;i<4;i++){Vector3 a=new Vector3(s*1.5f,0,-1.1f+i*.6f),b=new Vector3(s*(2.7f+i*.1f),.3f,-2+i*1.1f),d=b+new Vector3(s*.55f,-1.1f,.2f);CoastalMesh.Tube("Articulated leg",rig,new[]{a,b,d},new[]{.25f,.17f,.035f},shell,rim,7);}
                CoastalMesh.Tube("Claw arm",rig,new[]{new Vector3(s*1.6f,.15f,.8f),new Vector3(s*2.7f,.2f,2),new Vector3(s*2.4f,.55f,3.05f)},new[]{.34f,.38f,.44f},shell,rim,9);
                CoastalMesh.Tube("Upper pincer",rig,new[]{new Vector3(s*2.4f,.55f,2.9f),new Vector3(s*2.8f,.65f,3.7f),new Vector3(s*2.45f,.6f,4.2f)},new[]{.46f,.3f,.015f},rim,shell,8);
                CoastalMesh.Tube("Lower pincer",rig,new[]{new Vector3(s*2.4f,.4f,2.9f),new Vector3(s*2,.32f,3.5f),new Vector3(s*2.3f,.42f,3.9f)},new[]{.3f,.2f,0},rim,shell,7);
                Shape.Beam(rig,new Vector3(s*.85f,.75f,1),new Vector3(s*.85f,1.5f,1.1f),.12f,shell);
                Shape.Part("Eye",PrimitiveType.Sphere,rig,new Vector3(s*.85f,1.5f,1.1f),Vector3.one*.36f,new Color(.93f,.76f,.35f));Shape.Part("Pupil",PrimitiveType.Sphere,rig,new Vector3(s*.85f,1.5f,1.27f),Vector3.one*.17f,new Color(.06f,.07f,.035f));
            }
            WeakPoint(rig,new Vector3(0,.3f,1.62f),.38f);
        }
        static void WeakPoint(Transform p,Vector3 pos,float radius){var g=Shape.Part("Weak point",PrimitiveType.Sphere,p,pos,Vector3.one*radius*2,new Color(.6f,.9f,.61f),true,true);g.AddComponent<HitRegion>();}
    }
    public class HitRegion : MonoBehaviour { }
}

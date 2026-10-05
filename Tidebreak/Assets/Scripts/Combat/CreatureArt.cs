using UnityEngine;

namespace Tidebreak
{
    public static class CreatureArt
    {
        public static Transform Build(Transform parent,CreatureKind kind,bool elite,Color? palette=null)
        {
            var rig=new GameObject("Creature anatomy").transform;rig.SetParent(parent,false);
            if(kind==CreatureKind.Kraken)Kraken(rig);
            else if(kind==CreatureKind.Crab)Crab(rig,palette);
            else Fish(rig,kind,elite,palette);
            return rig;
        }
        static void Fish(Transform rig,CreatureKind kind,bool elite,Color? palette)
        {
            Color back=kind==CreatureKind.Snapper?new Color(.66f,.27f,.14f):kind==CreatureKind.Puffer?new Color(.43f,.48f,.2f):new Color(.17f,.34f,.37f);
            if(kind==CreatureKind.Angler)back=new Color(.19f,.27f,.29f);if(kind==CreatureKind.WhiteWhale)back=new Color(.7f,.76f,.73f);if(elite)back=new Color(.42f,.24f,.49f);
            if(palette.HasValue)back=palette.Value;
            Color belly=Color.Lerp(back,new Color(.92f,.87f,.64f),.65f),fin=back*.72f;fin.a=1;
            var root=new GameObject("Fish body").transform;root.SetParent(rig,false);
            bool boss=kind>=CreatureKind.Angler;root.localScale=Vector3.one*(boss?3.1f:.8f);
            if(kind==CreatureKind.WhiteWhale)root.localScale=new Vector3(3.7f,3.1f,4.3f);
            bool puffer=kind==CreatureKind.Puffer;
            float[] z={-1.43f,-1.16f,-.87f,-.38f,.15f,.58f,.9f,1.1f,1.17f};
            float[] radius={.045f,.12f,.31f,.48f,.51f,.46f,.31f,.17f,.015f};
            var m=new CoastalMesh();const int sides=22;
            for(int k=0;k<z.Length-1;k++)for(int j=0;j<sides;j++) {
                float a=j*Mathf.PI*2/sides,b=(j+1)*Mathf.PI*2/sides;
                float squash=puffer?1.3f:1;
                var aa=new Vector3(Mathf.Cos(a)*radius[k]*squash,Mathf.Sin(a)*radius[k],z[k]);
                var ab=new Vector3(Mathf.Cos(b)*radius[k]*squash,Mathf.Sin(b)*radius[k],z[k]);
                var ba=new Vector3(Mathf.Cos(a)*radius[k+1]*squash,Mathf.Sin(a)*radius[k+1],z[k+1]);
                var bb=new Vector3(Mathf.Cos(b)*radius[k+1]*squash,Mathf.Sin(b)*radius[k+1],z[k+1]);
                Color color=Color.Lerp(belly,back,Mathf.Clamp01((Mathf.Sin(a)+.35f)*1.05f));
                if(j==0||j==10||j==11||j==21)color=Color.Lerp(color,new Color(.87f,.74f,.42f),.35f);
                if(k%2==0&&j<11)color*=.95f;
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
            CoastalMesh.Tube("Defined lower jaw",root,new[]{new Vector3(-.17f,-.08f,1.03f),new Vector3(0,-.15f,1.13f),new Vector3(.17f,-.08f,1.03f)},new[]{.022f,.033f,.022f},belly,back,7);
            for(int side=-1;side<=1;side+=2)for(int j=0;j<4;j++){
                float at=-.54f+j*.25f;var curve=new[]{new Vector3(side*.36f,.24f,at),new Vector3(side*.48f,.02f,at-.08f),new Vector3(side*.38f,-.19f,at-.03f)};
                CoastalMesh.Tube("Scale brush stroke",root,curve,new[]{.01f,.015f,.007f},Color.Lerp(back,belly,.22f),back,5);
            }
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
            Color skin=new Color(.28f,.18f,.30f),light=new Color(.72f,.46f,.40f),ridge=new Color(.43f,.28f,.37f);
            CreatureSculpt.Curve(rig,"Sculpted ancient mantle",new[]{new Vector3(0,-.72f,.05f),new Vector3(0,.05f,0),new Vector3(0,1.6f,-.15f),new Vector3(0,3.4f,-.57f),new Vector3(0,4.7f,-1.0f),new Vector3(0,5.05f,-1.2f)},new[]{.9f,1.9f,2.14f,1.69f,.72f,.01f},skin,light,28,5,.86f);
            var body=rig.gameObject.AddComponent<SphereCollider>();body.radius=2.3f;body.center=Vector3.up*1.3f;
            for(int side=-1;side<=1;side+=2){
                CreatureSculpt.Eye(rig,new Vector3(side*1.4f,.72f,1.6f),.69f,ridge,new Color(.88f,.64f,.24f),side);
                CreatureSculpt.Curve(rig,"Heavy mantle brow",new[]{new Vector3(side*.34f,1.36f,1.72f),new Vector3(side*1.14f,1.62f,1.72f),new Vector3(side*1.91f,1.14f,1.34f)},new[]{.06f,.25f,.11f},ridge,skin,12,5);
                CreatureSculpt.Fin(rig,"Mantle swimming fin",new Vector3(side*1.43f,2.1f,-.64f),new[]{new Vector3(side*.37f,4.4f,-1.1f),new Vector3(side*1.62f,3.91f,-.9f),new Vector3(side*2.77f,2.62f,-.81f),new Vector3(side*2.16f,1.24f,-.4f),new Vector3(side*1.57f,.69f,-.38f)},skin,light,.15f,.08f);
                CreatureSculpt.Curve(rig,"Frontal mantle fold",new[]{new Vector3(side*.55f,-.65f,1.37f),new Vector3(side*.71f,-.21f,1.71f),new Vector3(side*.57f,.12f,1.75f)},new[]{.16f,.2f,.045f},ridge,light,10,4);
            }
            for(int i=0;i<8;i++) {
                var arm=new GameObject("Tentacle "+i).transform;arm.SetParent(rig,false);arm.localRotation=Quaternion.Euler(0,i*45,0);
                var points=new Vector3[10];var radii=new float[10];
                for(int j=0;j<10;j++){float t=j/9f;points[j]=new Vector3(1.2f+t*6.6f,-.7f+Mathf.Sin(t*4)*1.5f,t*t*1.6f);radii[j]=Mathf.Lerp(.64f,.022f,t);}
                CreatureSculpt.Curve(arm,"Continuous curling arm",points,radii,skin,light,12,4,.91f);
                for(int j=1;j<9;j++)for(int side=-1;side<=1;side+=2){
                    float width=radii[j]*.29f;Vector3 at=points[j]+new Vector3(0,radii[j]*.76f,side*radii[j]*.45f);
                    CreatureSculpt.Curve(arm,"Sculpted concave sucker",new[]{at,at+Vector3.up*width*.40f,at+Vector3.up*width*.54f},new[]{width*.61f,width,width*.7f},light,ridge,10,1);
                }
            }
            // The beak sits below the shootable organ; it never contributes a collider.
            CreatureSculpt.Curve(rig,"Hooked obsidian beak",new[]{new Vector3(0,-.75f,1.43f),new Vector3(0,-.4f,1.8f),new Vector3(0,-.53f,2.01f)},new[]{.24f,.27f,.002f},new Color(.1f,.075f,.13f),ridge,12,5,.72f);
            WeakPoint(rig,new Vector3(0,.1f,1.85f),.48f);
        }
        static void Crab(Transform rig,Color? palette)
        {
            Color shell=palette??new Color(.56f,.25f,.13f),rim=Color.Lerp(shell,new Color(.96f,.71f,.38f),.46f),seam=Color.Lerp(shell,new Color(.1f,.13f,.14f),.65f);
            var z=new[]{-1.54f,-1.3f,-.82f,-.15f,.58f,1.11f,1.48f};var x=new[]{.04f,1.12f,1.83f,2.15f,1.96f,1.25f,.13f};var y=new[]{.015f,.32f,.52f,.59f,.49f,.28f,.01f};var centres=new Vector3[z.Length];var widths=new Vector2[z.Length];for(int i=0;i<z.Length;i++){centres[i]=new Vector3(0,.27f,z[i]);widths[i]=new Vector2(x[i],y[i]);}
            CreatureSculpt.Sections(rig,"Crab carved carapace",centres,widths,shell,rim,32,2);
            CreatureSurfaceArt.Form(rig,"Crab underside shell",new Vector3(0,-.11f,.04f),new Vector3(3.53f,.66f,2.7f),rim);
            for(int side=-1;side<=1;side+=2){
                CreatureSculpt.Curve(rig,"Carapace raised ridge",new[]{new Vector3(side*.19f,.76f,-1.12f),new Vector3(side*.77f,.85f,-.58f),new Vector3(side*1.15f,.79f,.2f),new Vector3(side*.63f,.64f,1.01f)},new[]{.013f,.053f,.059f,.012f},rim,shell,8,4);
                for(int j=0;j<4;j++)CreatureSculpt.Curve(rig,"Carapace edge tooth",new[]{new Vector3(side*(1.5f+Mathf.Sin(j*.8f)*.34f),.38f,-1.04f+j*.54f),new Vector3(side*(2.03f+Mathf.Sin(j*.8f)*.25f),.47f,-1.20f+j*.55f)},new[]{.18f,.005f},shell,rim,8,3);
                CreatureSculpt.Curve(rig,"Shell lateral suture",new[]{new Vector3(side*1.47f,.66f,-.63f),new Vector3(side*1.82f,.61f,-.1f),new Vector3(side*1.65f,.56f,.59f)},new[]{.02f,.027f,.009f},seam,seam,6,4);
            }
            var c=rig.gameObject.AddComponent<BoxCollider>();c.size=new Vector3(4.2f,1.6f,3.1f);c.center=Vector3.up*.35f;
            for(int side=-1;side<=1;side+=2) {
                for(int i=0;i<4;i++){
                    Vector3 a=new Vector3(side*1.5f,0,-1.1f+i*.6f),b=new Vector3(side*(2.7f+i*.1f),.3f,-2+i*1.1f),foot=b+new Vector3(side*.55f,-1.1f,.2f);
                    CreatureSculpt.Curve(rig,"Articulated leg",new[]{a,Vector3.Lerp(a,b,.45f),b,Vector3.Lerp(b,foot,.63f),foot},new[]{.21f,.22f,.135f,.1f,.009f},shell,rim,10,3,.73f);
                }
                CreatureSculpt.Curve(rig,"Claw arm",new[]{new Vector3(side*1.45f,.03f,.72f),new Vector3(side*2.28f,.01f,1.34f),new Vector3(side*2.68f,.19f,1.94f),new Vector3(side*2.4f,.51f,2.87f)},new[]{.27f,.26f,.33f,.44f},shell,rim,12,4,.80f);
                CreatureSculpt.Curve(rig,"Upper pincer",new[]{new Vector3(side*2.41f,.55f,2.77f),new Vector3(side*2.67f,.63f,3.13f),new Vector3(side*2.82f,.63f,3.68f),new Vector3(side*2.47f,.58f,4.18f)},new[]{.43f,.41f,.23f,.006f},shell,rim,14,4,.72f);
                CreatureSculpt.Curve(rig,"Lower pincer",new[]{new Vector3(side*2.35f,.38f,2.86f),new Vector3(side*2.02f,.32f,3.28f),new Vector3(side*2.04f,.39f,3.64f),new Vector3(side*2.31f,.43f,3.91f)},new[]{.31f,.25f,.15f,.005f},rim,shell,12,4,.77f);
                for(int tooth=0;tooth<3;tooth++)CreatureSculpt.Curve(rig,"Pincer serrated tooth",new[]{new Vector3(side*(2.41f+tooth*.08f),.49f,3.22f+tooth*.2f),new Vector3(side*(2.23f+tooth*.09f),.47f,3.30f+tooth*.2f)},new[]{.078f,.003f},rim,shell,7,2);
                CreatureSculpt.Curve(rig,"Armoured eye stalk",new[]{new Vector3(side*.73f,.63f,.91f),new Vector3(side*.85f,1.12f,1.1f),new Vector3(side*.85f,1.46f,1.14f)},new[]{.14f,.11f,.095f},shell,rim,10,4);
                CreatureSculpt.Eye(rig,new Vector3(side*.85f,1.48f,1.15f),.34f,shell,new Color(.92f,.72f,.31f),side);
                CreatureSculpt.Curve(rig,"Mandible mouth plate",new[]{new Vector3(side*.43f,.09f,1.34f),new Vector3(side*.3f,-.11f,1.51f),new Vector3(side*.12f,-.14f,1.49f)},new[]{.17f,.11f,.025f},rim,shell,10,4,.62f);
            }
            WeakPoint(rig,new Vector3(0,.3f,1.62f),.38f);
        }
        static void WeakPoint(Transform p,Vector3 pos,float radius){var g=Shape.Part("Weak point",PrimitiveType.Sphere,p,pos,Vector3.one*radius*2,new Color(.6f,.9f,.61f),true,true);g.AddComponent<HitRegion>();CreatureSculpt.WeakOrgan(g);}
    }
    public class HitRegion : MonoBehaviour { }
}

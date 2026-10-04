using UnityEngine;
namespace Tidebreak
{
    // Twelve anatomical rigs; species also vary in proportions, appendages, palette and crest.
    public static partial class SpeciesArt
    {
        static Color skin,belly,dark;
        static Transform Part(Transform p,string name,Vector3 at,Vector3 scale,Color c,PrimitiveType shape=PrimitiveType.Sphere){
            if(shape==PrimitiveType.Sphere&&(name.Contains("disc")||name.Contains("shell")||name.Contains("bell")||name.Contains("mantle")||name.Contains("head")))return CreatureSurfaceArt.Form(p,name,at,scale,c);
            if(shape==PrimitiveType.Cube&&!name.Contains("Wreckage")&&(name.Contains("armor")||name.Contains("shard")||name.Contains("shell")||name.Contains("scale")))return CreatureSurfaceArt.Facet(p,name,at,scale,c);
            return Shape.Part(name,shape,p,at,scale,c).transform;}
        static void Limb(Transform p,string name,Vector3[] points,float width){float[] radii=new float[points.Length];for(int i=0;i<radii.Length;i++)radii[i]=Mathf.Lerp(width,.012f,i/(float)(radii.Length-1));CoastalMesh.Tube(name,p,points,radii,skin,belly,7);}
        static void Fin(Transform p,Vector3 a,Vector3 b,Vector3 c){CreatureSurfaceArt.Membrane(p,a,b,c,skin,belly);}
        public static Transform Build(Transform parent,SpeciesDefinition s,bool elite)
        {
            if(s.id==117){var kraken=CreatureArt.Build(parent,CreatureKind.Kraken,false);kraken.localScale=Vector3.one*1.7f;BossIdentityArt.BuildSignature(kraken,s);return kraken;}
            if(s.id==118){var whale=WhiteWhale(parent);BossIdentityArt.BuildSignature(whale,s);return whale;}
            var root=new GameObject("Anatomy - "+s.body).transform;root.SetParent(parent,false);root.localScale=Vector3.one*s.size*(elite?1.13f:1);
            skin=elite?Color.Lerp(s.color,new Color(.31f,.16f,.13f),.16f):s.color;belly=Color.Lerp(skin,new Color(.92f,.87f,.68f),.6f);dark=Color.Lerp(skin,Color.black,.65f);
            float variation=1+(s.island%3)*.12f;Vector3 colliderSize=new Vector3(1.25f,1.1f,2.6f),eyeAt=new Vector3(.36f,.25f,.7f),weak=new Vector3(0,.2f,1.05f);
            switch(s.body){
                case BodyFamily.Perch:case BodyFamily.Puffer:case BodyFamily.Shark:case BodyFamily.Swordfish:
                    var f=CreatureArt.Build(root,s.body==BodyFamily.Puffer?CreatureKind.Puffer:s.body==BodyFamily.Shark?CreatureKind.Razorfin:CreatureKind.Snapper,false,skin);f.localScale=new Vector3(s.body==BodyFamily.Puffer?1.35f:.95f,variation,s.body==BodyFamily.Shark?1.7f:1.25f);
                    foreach(var c in f.GetComponentsInChildren<Collider>()){c.enabled=false;Object.Destroy(c);}foreach(var h in f.GetComponentsInChildren<HitRegion>())Object.Destroy(h);
                    if(s.body==BodyFamily.Swordfish)Limb(root,"Sword",new[]{new Vector3(0,0,.9f),new Vector3(0,0,2.1f+variation*.2f)},.075f);
                    if(s.body==BodyFamily.Shark){Fin(root,new Vector3(0,.4f,-.7f),new Vector3(0,1.35f,-.8f),new Vector3(0,.4f,.2f));colliderSize.z=3.4f;}
                    break;
                case BodyFamily.Eel:
                    var points=new Vector3[14];var radii=new float[14];for(int i=0;i<14;i++){float t=i/13f;points[i]=new Vector3(Mathf.Sin(t*5)*.19f,Mathf.Sin(t*3)*.14f,(t-.5f)*4);radii[i]=Mathf.Sin(t*Mathf.PI)*.27f+.04f;}CoastalMesh.Tube("Long flexible body",root,points,radii,skin,belly,10);colliderSize=new Vector3(.65f,.65f,4);eyeAt=new Vector3(.19f,.11f,1.64f);weak=new Vector3(0,.08f,1.83f);break;
                case BodyFamily.Ray:
                    Part(root,"Ray disc",Vector3.zero,new Vector3(1.5f,.38f,1.8f),skin);for(int side=-1;side<=1;side+=2)Fin(root,new Vector3(side*.4f,0,.7f),new Vector3(side*(1.9f+s.island*.07f),.12f,-.55f),new Vector3(side*.3f,-.06f,-.8f));Limb(root,"Whip tail",new[]{new Vector3(0,0,-.6f),new Vector3(0,-.07f,-2),new Vector3(.3f,.15f,-3)},.1f);colliderSize=new Vector3(3,.5f,2);eyeAt=new Vector3(.32f,.21f,.47f);weak=new Vector3(0,.16f,.82f);break;
                case BodyFamily.Crab:
                    var crab=CreatureArt.Build(root,CreatureKind.Crab,false,skin);crab.localScale=Vector3.one*.33f;foreach(var c in crab.GetComponentsInChildren<Collider>()){c.enabled=false;Object.Destroy(c);}foreach(var h in crab.GetComponentsInChildren<HitRegion>())Object.Destroy(h);colliderSize=new Vector3(1.7f,.65f,1.4f);weak=new Vector3(0,.16f,.62f);eyeAt=new Vector3(.28f,.5f,.36f);break;
                case BodyFamily.Jelly:
                    Part(root,"Jelly bell",new Vector3(0,.4f,0),new Vector3(1.55f,.9f,1.4f),skin);Part(root,"Luminous core",new Vector3(0,.1f,0),Vector3.one*.5f,belly);for(int j=0;j<8;j++){float a=j*Mathf.PI/4;Limb(root,"Tentacle "+j,new[]{new Vector3(Mathf.Cos(a)*.48f,.1f,Mathf.Sin(a)*.48f),new Vector3(Mathf.Cos(a)*.55f,-.6f,Mathf.Sin(a)*.55f),new Vector3(Mathf.Cos(a)*.2f,-1.5f-j%3*.12f,Mathf.Sin(a)*.2f)},.055f);}colliderSize=new Vector3(1.4f,1.1f,1.4f);eyeAt=new Vector3(.28f,.48f,.55f);weak=new Vector3(0,.4f,.69f);break;
                case BodyFamily.Turtle:
                    Part(root,"Domed shell",Vector3.zero,new Vector3(1.8f,.95f,2),skin);CoastalMesh.Ring(root,new Vector3(0,-.13f,0),.84f,.08f,belly,Quaternion.Euler(90,0,0));Part(root,"Turtle head",new Vector3(0,-.02f,1.05f),new Vector3(.48f,.43f,.65f),belly);for(int j=0;j<4;j++){float x=j%2==0?-1:1,z=j<2?.65f:-.65f;Fin(root,new Vector3(x*.6f,-.1f,z),new Vector3(x*1.35f,-.1f,z-.2f),new Vector3(x*.85f,-.3f,z-.65f));}colliderSize=new Vector3(1.75f,1,2.5f);eyeAt=new Vector3(.19f,.06f,1.17f);weak=new Vector3(0,0,1.4f);break;
                case BodyFamily.Squid:
                    Part(root,"Torpedo mantle",new Vector3(0,.05f,-.35f),new Vector3(.9f,1.2f,2),skin);for(int j=0;j<8;j++){float a=j*Mathf.PI/4;Limb(root,"Tentacle "+j,new[]{new Vector3(Mathf.Cos(a)*.32f,Mathf.Sin(a)*.4f,.2f),new Vector3(Mathf.Cos(a)*.62f,Mathf.Sin(a)*.6f,1.15f),new Vector3(Mathf.Cos(a)*.2f,Mathf.Sin(a)*.3f,1.65f+j%2*.4f)},.115f);}eyeAt=new Vector3(.4f,.15f,.35f);weak=new Vector3(0,.15f,.52f);break;
                case BodyFamily.Seahorse:
                    Limb(root,"Curled horse body",new[]{new Vector3(.3f,-.9f,-.2f),new Vector3(.5f,-1.1f,-.3f),new Vector3(0,-1.3f,-.3f),new Vector3(-.25f,-.9f,-.2f),new Vector3(0,0,0),new Vector3(0,.65f,.05f),new Vector3(0,.85f,.5f)},.23f);Part(root,"Horse head",new Vector3(0,.78f,.26f),new Vector3(.5f,.6f,.6f),skin);Limb(root,"Snout",new[]{new Vector3(0,.75f,.4f),new Vector3(0,.69f,1.03f)},.13f);Fin(root,new Vector3(0,.1f,-.2f),new Vector3(0,.3f,-.9f),new Vector3(0,-.6f,-.3f));colliderSize=new Vector3(.65f,2.3f,1.4f);eyeAt=new Vector3(.23f,.9f,.4f);weak=new Vector3(0,.8f,.6f);break;
                default:
                    Part(root,"Urchin shell",Vector3.zero,Vector3.one*1.1f,skin);for(int j=0;j<32;j++){float y=-.9f+1.8f*j/31f,a=j*2.39996f;Vector3 dir=new Vector3(Mathf.Cos(a)*Mathf.Sqrt(1-y*y),y,Mathf.Sin(a)*Mathf.Sqrt(1-y*y));Limb(root,"Spine",new[]{dir*.45f,dir*(.95f+(j%3)*.14f)},.055f);}colliderSize=Vector3.one*1.3f;eyeAt=new Vector3(.25f,.12f,.48f);weak=new Vector3(0,.12f,.58f);break;
            }
            for(int side=-1;side<=1;side+=2){Vector3 at=eyeAt;at.x*=side;Part(root,"Eye",at,Vector3.one*.15f,belly);Part(root,"Pupil",at+Vector3.forward*.07f,Vector3.one*.075f,dark);}
            // Island-specific sensory crests change the silhouette, including when seen at a distance.
            for(int j=0;j<s.island%4;j++)Limb(root,"Sensory crest",new[]{new Vector3((j-1)*.16f,.48f,.18f),new Vector3((j-1)*.23f,.9f+j*.12f,-.1f)},.045f);
            BuildEcology(root,s);
            CreatureSurfaceArt.Finish(root,s,elite,skin,belly);
            // Keep the luminous frontal target outside the solid hull, so a head-on ray can hit it.
            var col=root.gameObject.AddComponent<BoxCollider>();col.center=Vector3.back*.12f+Vector3.up*(s.body==BodyFamily.Jelly?.25f:0);col.size=new Vector3(colliderSize.x,colliderSize.y,Mathf.Min(colliderSize.z,(weak.z+.02f)*2));
            var point=Shape.Part("Weak point",PrimitiveType.Sphere,root,weak,Vector3.one*.24f,new Color(.5f,1,.8f),true,true);point.AddComponent<HitRegion>();
            BossIdentityArt.BuildSignature(root,s);
            return root;
        }
        static Transform WhiteWhale(Transform parent)
        {
            var root=new GameObject("White whale anatomy").transform;root.SetParent(parent,false);root.localScale=Vector3.one*5.2f;
            skin=new Color(.72f,.83f,.85f);belly=new Color(.91f,.94f,.86f);dark=new Color(.08f,.18f,.23f);
            CoastalMesh.Tube("Streamlined whale",root,new[]{new Vector3(0,0,-2),new Vector3(0,0,-1.3f),new Vector3(0,0,-.4f),new Vector3(0,0,.55f),new Vector3(0,0,1.12f),new Vector3(0,0,1.38f)},new[]{.07f,.3f,.57f,.66f,.48f,.22f},skin,belly,16);
            Part(root,"Rounded whale forehead",new Vector3(0,.1f,.83f),new Vector3(.97f,.96f,1.04f),skin);
            // Thick horizontal flukes stay legible against the water from a low
            // captain camera; paper-thin fins disappear almost entirely edge-on.
            for(int side=-1;side<=1;side+=2){
                var fluke=Part(root,"Whale tail fluke",new Vector3(side*.51f,.02f,-2.06f),new Vector3(1.23f,.16f,.72f),skin);
                fluke.localRotation=Quaternion.Euler(0,side*-18,side*5);
                var paddle=Part(root,"Whale pectoral paddle",new Vector3(side*.77f,-.08f,.06f),new Vector3(1.02f,.16f,.52f),belly);
                paddle.localRotation=Quaternion.Euler(0,side*32,side*-12);
            }
            for(int side=-1;side<=1;side+=2){Fin(root,new Vector3(side*.38f,-.13f,.52f),new Vector3(side*1.4f,-.29f,-.4f),new Vector3(side*.45f,-.26f,-.45f));Fin(root,new Vector3(0,0,-1.8f),new Vector3(side*1.25f,.08f,-2.15f),new Vector3(side*.46f,0,-2.55f));Part(root,"Whale eye",new Vector3(side*.42f,.14f,1.04f),Vector3.one*.115f,dark);}
            Limb(root,"Mouth seam",new[]{new Vector3(-.3f,-.18f,1.19f),new Vector3(0,-.21f,1.42f),new Vector3(.3f,-.18f,1.19f)},.013f);
            Part(root,"Blowhole",new Vector3(0,.58f,.34f),new Vector3(.18f,.04f,.23f),dark);
            var collider=root.gameObject.AddComponent<CapsuleCollider>();collider.direction=2;collider.center=Vector3.back*.34f;collider.height=3.05f;collider.radius=.56f;
            var weak=Shape.Part("Whale sonar organ",PrimitiveType.Sphere,root,new Vector3(0,.15f,1.42f),Vector3.one*.28f,new Color(.5f,1,.8f),true,true);weak.AddComponent<HitRegion>();return root;
        }
    }
}

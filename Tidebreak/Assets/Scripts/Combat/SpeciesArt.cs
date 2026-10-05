using UnityEngine;
namespace Tidebreak
{
    // Twelve sculpted anatomies share a palette language, never a single scaled base primitive.
    public static partial class SpeciesArt
    {
        static Color skin,belly,dark;
        static Transform Part(Transform p,string name,Vector3 at,Vector3 scale,Color c,PrimitiveType shape=PrimitiveType.Sphere){
            if(shape==PrimitiveType.Sphere)return CreatureSurfaceArt.Form(p,name,at,scale,c);
            if(shape==PrimitiveType.Cube&&!name.Contains("Wreckage"))return CreatureSurfaceArt.Facet(p,name,at,scale,c);
            return Shape.Part(name,shape,p,at,scale,c).transform;
        }
        static void Limb(Transform p,string name,Vector3[] points,float width){float[] radii=new float[points.Length];for(int i=0;i<radii.Length;i++)radii[i]=Mathf.Lerp(width,.008f,i/(float)(radii.Length-1));CreatureSculpt.Curve(p,name,points,radii,skin,belly,9,4);}
        static void Fin(Transform p,Vector3 a,Vector3 b,Vector3 c){CreatureSculpt.Fin(p,"Fin membrane",a,new[]{a,Vector3.Lerp(a,b,.55f),b,Vector3.Lerp(b,c,.55f),c},skin,belly);}
        static Transform Sections(Transform p,string name,float[] z,float[] x,float[] y,float[] lift=null,int pattern=0){var centres=new Vector3[z.Length];var radii=new Vector2[z.Length];for(int i=0;i<z.Length;i++){centres[i]=new Vector3(0,lift==null?0:lift[i],z[i]);radii[i]=new Vector2(x[i],y[i]);}return CreatureSculpt.Sections(p,name,centres,radii,skin,belly,28,pattern);}
        static void Stroke(Transform p,string name,Vector3[] points,float width,Color color){var radii=new float[points.Length];for(int i=0;i<radii.Length;i++)radii[i]=width*(i==0||i==radii.Length-1?.45f:1);CreatureSculpt.Curve(p,name,points,radii,color,color,6,3);}
        public static Transform Build(Transform parent,SpeciesDefinition s,bool elite)
        {
            if(s.id==117){var kraken=CreatureArt.Build(parent,CreatureKind.Kraken,false);kraken.localScale=Vector3.one*1.7f;BossIdentityArt.BuildSignature(kraken,s);return kraken;}
            if(s.id==118){var whale=WhiteWhale(parent);BossIdentityArt.BuildSignature(whale,s);return whale;}
            var root=new GameObject("Anatomy - "+s.body).transform;root.SetParent(parent,false);root.localScale=Vector3.one*s.size*(elite?1.13f:1);
            skin=elite?Color.Lerp(s.color,new Color(.31f,.16f,.13f),.16f):s.color;belly=Color.Lerp(skin,new Color(.92f,.87f,.68f),.6f);dark=Color.Lerp(skin,new Color(.02f,.08f,.1f),.7f);
            Vector3 colliderSize=new Vector3(1.25f,1.1f,2.6f),eyeAt=new Vector3(.36f,.25f,.7f),weak=new Vector3(0,.2f,1.05f);bool eyes=true;
            switch(s.body){
                case BodyFamily.Perch:case BodyFamily.Puffer:case BodyFamily.Shark:case BodyFamily.Swordfish:
                    Fish(root,s.body);if(s.body==BodyFamily.Shark)colliderSize.z=3.4f;break;
                case BodyFamily.Eel:
                    Eel(root);colliderSize=new Vector3(.65f,.65f,4);eyeAt=new Vector3(.19f,.11f,1.64f);weak=new Vector3(0,.08f,1.83f);break;
                case BodyFamily.Ray:
                    Ray(root,s.island);colliderSize=new Vector3(3,.5f,2);eyeAt=new Vector3(.32f,.21f,.47f);weak=new Vector3(0,.16f,.82f);break;
                case BodyFamily.Crab:
                    var crab=CreatureArt.Build(root,CreatureKind.Crab,false,skin);crab.localScale=Vector3.one*.33f;foreach(var c in crab.GetComponentsInChildren<Collider>()){c.enabled=false;Object.Destroy(c);}foreach(var h in crab.GetComponentsInChildren<HitRegion>())Object.Destroy(h);colliderSize=new Vector3(1.7f,.65f,1.4f);weak=new Vector3(0,.16f,.62f);eyes=false;break;
                case BodyFamily.Jelly:
                    Jelly(root);colliderSize=new Vector3(1.4f,1.1f,1.4f);eyeAt=new Vector3(.28f,.48f,.55f);weak=new Vector3(0,.4f,.69f);eyes=false;break;
                case BodyFamily.Turtle:
                    Turtle(root);colliderSize=new Vector3(1.75f,1,2.5f);eyeAt=new Vector3(.19f,.06f,1.17f);weak=new Vector3(0,0,1.4f);break;
                case BodyFamily.Squid:
                    Squid(root);eyeAt=new Vector3(.4f,.15f,.35f);weak=new Vector3(0,.15f,.52f);break;
                case BodyFamily.Seahorse:
                    Seahorse(root);colliderSize=new Vector3(.65f,2.3f,1.4f);eyeAt=new Vector3(.23f,.9f,.4f);weak=new Vector3(0,.8f,.6f);break;
                default:
                    Urchin(root);colliderSize=Vector3.one*1.3f;eyeAt=new Vector3(.25f,.12f,.48f);weak=new Vector3(0,.12f,.58f);eyes=false;break;
            }
            if(eyes)for(int side=-1;side<=1;side+=2){Vector3 at=eyeAt;at.x*=side;CreatureSculpt.Eye(root,at,s.body==BodyFamily.Squid?.24f:.155f,skin,new Color(.89f,.68f,.32f),side);}
            for(int j=0;j<s.island%4;j++){
                Vector3 crest=CreatureSurfaceArt.CrestAnchor(s.body,(j-1)*.38f,.78f);float crestScale=CreatureSurfaceArt.CrestScale(s.body);
                Limb(root,"Sensory crest",new[]{crest,crest+new Vector3((j-1)*.07f,.42f+j*.12f,-.28f)*crestScale},.045f*crestScale);
            }
            BuildEcology(root,s);CreatureSurfaceArt.Finish(root,s,elite,skin,belly);
            // Sculpture preserves the authored gameplay hull and weak-organ hit transform.
            var col=root.gameObject.AddComponent<BoxCollider>();col.center=Vector3.back*.12f+Vector3.up*(s.body==BodyFamily.Jelly?.25f:0);col.size=new Vector3(colliderSize.x,colliderSize.y,Mathf.Min(colliderSize.z,(weak.z+.02f)*2));
            var point=Shape.Part("Weak point",PrimitiveType.Sphere,root,weak,Vector3.one*.24f,new Color(.5f,1,.8f),true,true);point.AddComponent<HitRegion>();CreatureSculpt.WeakOrgan(point);BossIdentityArt.BuildSignature(root,s);return root;
        }
        static void Fish(Transform root,BodyFamily family)
        {
            bool puffer=family==BodyFamily.Puffer,shark=family==BodyFamily.Shark,sword=family==BodyFamily.Swordfish;
            float tail=shark?-1.85f:-1.23f;
            Sections(root,"Faceted scales",new[]{tail,-1.05f,-.78f,-.38f,.03f,.39f,.68f,.9f,1.01f},
                puffer?new[]{.012f,.16f,.45f,.68f,.74f,.67f,.49f,.23f,.07f}:shark?new[]{.009f,.15f,.29f,.42f,.47f,.43f,.32f,.19f,.04f}:new[]{.009f,.12f,.29f,.43f,.46f,.4f,.29f,.19f,.06f},
                puffer?new[]{.015f,.17f,.46f,.63f,.68f,.62f,.44f,.2f,.05f}:shark?new[]{.014f,.19f,.28f,.32f,.34f,.29f,.21f,.12f,.025f}:new[]{.012f,.19f,.4f,.52f,.56f,.49f,.31f,.19f,.08f},null,puffer?2:1);
            var tailJoint=new GameObject("Tail fin").transform;tailJoint.SetParent(root,false);tailJoint.localPosition=new Vector3(0,0,tail+.14f);
            CreatureSculpt.Fin(tailJoint,"Forked caudal fin",Vector3.zero,new[]{new Vector3(0,.05f,0),new Vector3(0,shark?1.05f:.6f,-.72f),new Vector3(0,.24f,-.66f),new Vector3(0,0,-.35f),new Vector3(0,-.3f,-.72f),new Vector3(0,-.54f,-.64f),new Vector3(0,-.08f,0)},skin,belly,0,.016f);
            if(!puffer)CreatureSculpt.Fin(root,"Dorsal sail",new Vector3(0,shark?.25f:.3f,-.52f),new[]{new Vector3(0,.16f,-1.04f),new Vector3(0,.65f,-.92f),new Vector3(0,shark?1.15f:.85f,-.61f),new Vector3(0,.73f,-.35f),new Vector3(0,shark?.27f:.51f,.18f)},skin,belly,0,.008f);
            for(int side=-1;side<=1;side+=2){
                CreatureSculpt.Fin(root,"Pectoral fin membrane",new Vector3(side*.34f,-.13f,.34f),new[]{new Vector3(side*.38f,-.13f,.42f),new Vector3(side*(shark?1.15f:.91f),-.28f,-.2f),new Vector3(side*.79f,-.36f,-.54f),new Vector3(side*.48f,-.25f,-.37f),new Vector3(side*.32f,-.19f,-.13f)},skin,belly,.05f,.018f);
                int gills=shark?4:2;for(int g=0;g<gills;g++)Stroke(root,"Carved gill slit",new[]{new Vector3(side*(.34f+g*.025f),.22f,.57f-g*.11f),new Vector3(side*(.4f+g*.025f),-.01f,.53f-g*.11f),new Vector3(side*.32f,-.25f,.56f-g*.1f)},.017f,dark);
            }
            var jaw=Sections(root,"Sculpted lower jaw",new[]{.49f,.69f,.91f,1.035f},new[]{.24f,.25f,.16f,.012f},new[]{.05f,.085f,.07f,.005f},new[]{-.23f,-.21f,-.15f,-.11f});
            Stroke(root,"Curved mouth opening",new[]{new Vector3(-.21f,-.13f,.88f),new Vector3(-.12f,-.12f,1.01f),new Vector3(0,-.1f,1.045f),new Vector3(.12f,-.12f,1.01f),new Vector3(.21f,-.13f,.88f)},.021f,dark);
            if(sword)CreatureSculpt.Curve(root,"Sword",new[]{new Vector3(0,-.015f,.87f),new Vector3(0,-.02f,1.35f),new Vector3(0,.01f,2.25f)},new[]{.095f,.065f,.002f},skin,belly,10,4,.65f);
            if(puffer)for(int i=0;i<38;i++){float y=-.8f+1.6f*i/37,a=i*2.39996f;Vector3 d=new Vector3(Mathf.Cos(a)*Mathf.Sqrt(1-y*y),y,Mathf.Sin(a)*Mathf.Sqrt(1-y*y));Vector3 basePoint=Vector3.Scale(d,new Vector3(.68f,.62f,.86f))+Vector3.back*.11f;CreatureSculpt.Curve(root,"Ivory puffer thorn",new[]{basePoint,basePoint+d*.09f,basePoint+d*(.2f+i%3*.018f)},new[]{.045f,.028f,.001f},belly,skin,6,2);}
        }
        static void Eel(Transform root)
        {
            var p=new Vector3[15];var w=new float[15];for(int i=0;i<15;i++){float u=i/14f;p[i]=new Vector3(Mathf.Sin(u*5)*.16f,Mathf.Sin(u*3)*.08f,-2+u*3.8f);w[i]=i==14?.06f:Mathf.Sin(u*Mathf.PI*.86f)*.245f+.017f;}CreatureSculpt.Curve(root,"Long flexible body",p,w,skin,belly,18,3,1.03f);
            Sections(root,"Predatory eel head",new[]{1.12f,1.38f,1.61f,1.79f},new[]{.21f,.25f,.2f,.055f},new[]{.2f,.22f,.15f,.03f},new[]{.04f,.04f,.025f,0});
            Stroke(root,"Eel mouth seam",new[]{new Vector3(-.21f,-.025f,1.4f),new Vector3(-.17f,-.075f,1.68f),new Vector3(0,-.03f,1.82f),new Vector3(.17f,-.075f,1.68f),new Vector3(.21f,-.025f,1.4f)},.022f,dark);
            for(int side=-1;side<=1;side+=2)for(int n=0;n<4;n++){float z=1.37f+n*.075f;CreatureSculpt.Curve(root,"Recurved needle tooth",new[]{new Vector3(side*.19f,-.05f,z),new Vector3(side*.17f,-.12f,z+.035f)},new[]{.022f,.001f},belly,belly,5,2);}
        }
        static void Ray(Transform root,int island)
        {
            Sections(root,"Sculpted ray keel",new[]{-.94f,-.62f,-.15f,.3f,.66f,.82f},new[]{.025f,.34f,.55f,.54f,.31f,.035f},new[]{.018f,.1f,.16f,.19f,.12f,.015f},null,2);
            for(int side=-1;side<=1;side+=2){float span=1.9f+island*.07f;CreatureSculpt.Fin(root,"Ray wing fin membrane",new Vector3(side*.15f,-.025f,.1f),new[]{new Vector3(side*.23f,.025f,.74f),new Vector3(side*.79f,.07f,.63f),new Vector3(side*1.35f,.12f,.11f),new Vector3(side*span,.07f,-.52f),new Vector3(side*1.4f,-.04f,-.4f),new Vector3(side*.83f,-.08f,-.83f),new Vector3(side*.25f,-.055f,-.81f)},skin,belly,.12f,.043f);
                CreatureSculpt.Curve(root,"Cephalic feeding horn",new[]{new Vector3(side*.3f,.025f,.56f),new Vector3(side*.45f,.02f,.85f),new Vector3(side*.42f,.01f,1.05f)},new[]{.085f,.06f,.009f},skin,belly,9,4);
                for(int g=0;g<3;g++)Stroke(root,"Ray ventral gill",new[]{new Vector3(side*.13f,-.145f,.26f-g*.13f),new Vector3(side*.34f,-.17f,.21f-g*.13f)},.012f,dark);
            }
            CreatureSculpt.Curve(root,"Whip tail",new[]{new Vector3(0,0,-.69f),new Vector3(.07f,-.025f,-1.34f),new Vector3(.27f,.04f,-2.2f),new Vector3(.2f,.16f,-3)},new[]{.13f,.095f,.042f,.001f},skin,belly,10,4);
        }
        static void Jelly(Transform root)
        {
            CreatureSculpt.Bell(root,skin,belly);
            for(int arm=0;arm<4;arm++){float a=arm*Mathf.PI/2+.4f;var d=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));CreatureSculpt.Curve(root,"Frilled oral arm",new[]{d*.16f+Vector3.up*.48f,d*.25f+Vector3.down*.1f,d*.36f+Vector3.down*.65f,d*.13f+Vector3.down*1.17f},new[]{.16f,.17f,.10f,.005f},belly,skin,9,5,.45f);}
            for(int j=0;j<12;j++){float a=j*Mathf.PI/6;var d=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));CreatureSculpt.Curve(root,"Tentacle "+j,new[]{d*.68f+Vector3.up*.32f,d*.57f+Vector3.down*.29f,d*.67f+Vector3.down*.84f,d*.29f+Vector3.down*(1.42f+j%3*.14f)},new[]{.032f,.026f,.019f,.002f},skin,belly,7,5);}
        }
        static void Turtle(Transform root)
        {
            Part(root,"Domed shell",Vector3.zero,new Vector3(1.8f,.95f,2),skin);
            Sections(root,"Leather turtle neck",new[]{.57f,.84f,1.12f,1.35f},new[]{.20f,.21f,.25f,.06f},new[]{.14f,.19f,.20f,.04f},new[]{-.11f,-.08f,-.02f,-.02f});
            Stroke(root,"Turtle beak jaw",new[]{new Vector3(-.2f,-.07f,1.12f),new Vector3(-.12f,-.11f,1.31f),new Vector3(0,-.09f,1.39f),new Vector3(.12f,-.11f,1.31f),new Vector3(.2f,-.07f,1.12f)},.016f,dark);
            for(int j=0;j<4;j++){float side=j%2==0?-1:1,z=j<2?.62f:-.62f;CreatureSculpt.Fin(root,"Turtle flipper",new Vector3(side*.55f,-.13f,z),new[]{new Vector3(side*.6f,-.08f,z+.1f),new Vector3(side*1.27f,-.16f,z-.07f),new Vector3(side*1.48f,-.27f,z-.43f),new Vector3(side*1.17f,-.32f,z-.48f),new Vector3(side*.68f,-.24f,z-.28f)},skin,belly,.015f,.07f,false);}
            CreatureSculpt.Curve(root,"Turtle tapered tail",new[]{new Vector3(0,-.11f,-.7f),new Vector3(0,-.14f,-1.28f)},new[]{.13f,.005f},skin,belly,8,3);
        }
        static void Squid(Transform root)
        {
            Sections(root,"Torpedo mantle",new[]{-1.48f,-1.28f,-.98f,-.62f,-.19f,.12f,.28f},new[]{.008f,.19f,.35f,.43f,.43f,.33f,.24f},new[]{.01f,.27f,.44f,.51f,.45f,.32f,.24f},null,2);
            for(int j=0;j<8;j++){float a=j*Mathf.PI/4;var d=new Vector3(Mathf.Cos(a),Mathf.Sin(a)*1.1f,0);var controls=new[]{d*.25f+Vector3.forward*.2f,d*.55f+Vector3.forward*.72f,d*.64f+Vector3.forward*1.22f,d*.3f+Vector3.forward*(1.68f+j%2*.28f)};var arm=CreatureSculpt.Curve(root,"Tentacle "+j,controls,new[]{.11f,.092f,.058f,.004f},skin,belly,10,5);for(int cup=1;cup<4;cup++){var pos=Vector3.Lerp(controls[1],controls[2],cup/4f)-d*.05f;Part(arm,"Arm sucker",pos,new Vector3(.065f,.035f,.075f),belly);}}
            for(int side=-1;side<=1;side+=2)CreatureSculpt.Curve(root,"Long feeding tentacle",new[]{new Vector3(side*.26f,-.1f,.19f),new Vector3(side*.72f,-.23f,.99f),new Vector3(side*.74f,-.12f,1.91f),new Vector3(side*.48f,.07f,2.38f)},new[]{.078f,.05f,.09f,.002f},skin,belly,10,5);
            Part(root,"Siphon funnel",new Vector3(0,-.3f,.23f),new Vector3(.22f,.17f,.38f),belly);
        }
        static void Seahorse(Transform root)
        {
            CreatureSculpt.Curve(root,"Curled horse body",new[]{new Vector3(.28f,-.98f,-.24f),new Vector3(.42f,-1.18f,-.24f),new Vector3(.04f,-1.3f,-.28f),new Vector3(-.24f,-1.07f,-.29f),new Vector3(-.11f,-.49f,-.09f),new Vector3(0,.04f,.01f),new Vector3(-.055f,.55f,.09f),new Vector3(0,.79f,.32f)},new[]{.022f,.048f,.075f,.1f,.20f,.31f,.2f,.15f},skin,belly,16,5,.86f);
            Sections(root,"Horse head",new[]{-.02f,.17f,.39f,.56f},new[]{.06f,.23f,.25f,.09f},new[]{.1f,.24f,.22f,.10f},new[]{.84f,.86f,.83f,.76f});
            CreatureSculpt.Curve(root,"Trumpet snout",new[]{new Vector3(0,.75f,.39f),new Vector3(0,.68f,.70f),new Vector3(0,.68f,1.03f)},new[]{.13f,.085f,.082f},skin,belly,12,4);
            Stroke(root,"Snout aperture",new[]{new Vector3(-.063f,.69f,1.035f),new Vector3(0,.73f,1.043f),new Vector3(.063f,.69f,1.035f)},.018f,dark);
            for(int i=0;i<7;i++){float y=-.68f+i*.165f,w=.2f+Mathf.Sin(i/6f*Mathf.PI)*.1f;Stroke(root,"Bony abdomen plate",new[]{new Vector3(-w*.6f,y,-.09f),new Vector3(-w,y,.02f),new Vector3(0,y-.025f,.17f),new Vector3(w,y,.02f),new Vector3(w*.6f,y,-.09f)},.022f,Color.Lerp(skin,belly,.45f));}
            Fin(root,new Vector3(0,.25f,-.13f),new Vector3(0,.2f,-.91f),new Vector3(0,-.59f,-.26f));
            for(int i=0;i<4;i++)Limb(root,"Seahorse coronet",new[]{new Vector3((i-1.5f)*.09f,1.0f,.17f),new Vector3((i-1.5f)*.12f,1.29f-Mathf.Abs(i-1.5f)*.05f,.08f)},.044f);
        }
        static void Urchin(Transform root)
        {
            Part(root,"Urchin shell",Vector3.zero,Vector3.one*1.1f,skin);
            for(int j=0;j<40;j++){float y=-.88f+1.76f*j/39,a=j*2.39996f;Vector3 d=new Vector3(Mathf.Cos(a)*Mathf.Sqrt(1-y*y),y,Mathf.Sin(a)*Mathf.Sqrt(1-y*y));CreatureSculpt.Curve(root,"Tapered fluted spine",new[]{d*.45f,d*.64f,d*(.97f+j%3*.14f)},new[]{.065f,.04f,.002f},skin,belly,7,3);}
            for(int arc=0;arc<5;arc++){var pts=new Vector3[9];float a=arc*Mathf.PI*2/5;for(int i=0;i<9;i++){float b=i*Mathf.PI/8;pts[i]=new Vector3(Mathf.Cos(a)*Mathf.Sin(b),Mathf.Cos(b),Mathf.Sin(a)*Mathf.Sin(b))*.55f;}Stroke(root,"Ambulacral shell ridge",pts,.018f,belly);}
        }
        static Transform WhiteWhale(Transform parent)
        {
            var root=new GameObject("White whale anatomy").transform;root.SetParent(parent,false);root.localScale=Vector3.one*5.2f;
            skin=new Color(.63f,.78f,.82f);belly=new Color(.94f,.91f,.78f);dark=new Color(.08f,.18f,.23f);
            Sections(root,"Streamlined whale",new[]{-2.02f,-1.75f,-1.32f,-.83f,-.31f,.19f,.61f,.96f,1.23f,1.40f},new[]{.025f,.13f,.3f,.45f,.57f,.64f,.61f,.48f,.31f,.025f},new[]{.04f,.16f,.31f,.43f,.53f,.59f,.58f,.48f,.28f,.07f},new[]{.02f,.01f,0,0,0,.025f,.055f,.045f,-.03f,-.07f});
            Sections(root,"Whale powerful lower jaw",new[]{.42f,.76f,1.03f,1.3f,1.415f},new[]{.33f,.37f,.32f,.19f,.01f},new[]{.1f,.15f,.14f,.08f,.005f},new[]{-.34f,-.30f,-.25f,-.18f,-.13f});
            for(int side=-1;side<=1;side+=2){
                CreatureSculpt.Fin(root,"Whale tail fluke",new Vector3(0,.015f,-1.72f),new[]{new Vector3(side*.13f,.015f,-1.82f),new Vector3(side*.62f,.06f,-1.91f),new Vector3(side*1.3f,.11f,-2.22f),new Vector3(side*1.13f,.04f,-2.43f),new Vector3(side*.54f,-.01f,-2.48f),new Vector3(side*.09f,0,-2.08f)},skin,belly,.025f,.073f,false);
                CreatureSculpt.Fin(root,"Whale pectoral paddle",new Vector3(side*.44f,-.14f,.44f),new[]{new Vector3(side*.44f,-.1f,.48f),new Vector3(side*.88f,-.12f,.17f),new Vector3(side*1.41f,-.25f,-.31f),new Vector3(side*1.38f,-.29f,-.46f),new Vector3(side*.95f,-.26f,-.42f),new Vector3(side*.45f,-.23f,-.16f)},skin,belly,.025f,.067f,false);
                CreatureSculpt.Eye(root,new Vector3(side*.42f,.14f,1.04f),.11f,skin,new Color(.58f,.73f,.73f),side);
                for(int g=0;g<4;g++)Stroke(root,"Ventral throat pleat",new[]{new Vector3(side*(.06f+g*.045f),-.22f,1.18f),new Vector3(side*(.08f+g*.065f),-.46f,.64f),new Vector3(side*(.09f+g*.08f),-.5f,.1f)},.008f,Color.Lerp(skin,dark,.21f));
            }
            Stroke(root,"Mouth seam",new[]{new Vector3(-.4f,-.12f,.79f),new Vector3(-.3f,-.15f,1.19f),new Vector3(0,-.12f,1.43f),new Vector3(.3f,-.15f,1.19f),new Vector3(.4f,-.12f,.79f)},.014f,dark);
            Part(root,"Blowhole",new Vector3(0,.615f,.32f),new Vector3(.18f,.022f,.23f),dark);
            var collider=root.gameObject.AddComponent<CapsuleCollider>();collider.direction=2;collider.center=Vector3.back*.34f;collider.height=3.05f;collider.radius=.56f;
            var weak=Shape.Part("Whale sonar organ",PrimitiveType.Sphere,root,new Vector3(0,.15f,1.42f),Vector3.one*.28f,new Color(.5f,1,.8f),true,true);weak.AddComponent<HitRegion>();CreatureSculpt.WeakOrgan(weak);return root;
        }
    }
}

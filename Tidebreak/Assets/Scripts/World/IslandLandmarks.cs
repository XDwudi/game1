using UnityEngine;
namespace Tidebreak
{
    public partial class SeaWorld
    {
        bool ClearTrail(float x,float z){return Layout.IsTrail(x,z,1.2f)||z>2&&Mathf.Abs(x)<29;}
        void BuildGuide()
        {
            Vector3 p=QuestPoint;p.y=Height(p.x,p.z);
            // Animated joints cannot be children of Scenery: that hierarchy is statically batched.
            var root=new GameObject(Definition.npc).transform;root.SetParent(transform);root.position=p;
            var motion=root.gameObject.AddComponent<IslandActorMotion>();
            Color skin=new Color(.7f,.49f,.32f);
            for(int s=-1;s<=1;s+=2){
                Shape.Beam(root,new Vector3(s*.17f,.14f,0),new Vector3(s*.17f,.85f,0),.2f,iron);
                Shape.Part("Leather boot",PrimitiveType.Cube,root,new Vector3(s*.17f,.1f,.07f),new Vector3(.24f,.2f,.37f),wood);
            }
            motion.Chest=Shape.Part("Guide coat",PrimitiveType.Capsule,root,Vector3.up*1.1f,new Vector3(.7f,.5f,.45f),Definition.accent).transform;
            Shape.Part("Collar",PrimitiveType.Cube,root,new Vector3(0,1.5f,.09f),new Vector3(.53f,.16f,.43f),ivory);
            var head=new GameObject("Guide head joint").transform;head.SetParent(root,false);head.localPosition=Vector3.up*1.69f;motion.Head=head;
            Shape.Part("Guide face",PrimitiveType.Sphere,head,new Vector3(0,.11f,0),new Vector3(.46f,.53f,.43f),skin);
            Shape.Part("Hood",PrimitiveType.Sphere,head,new Vector3(0,.2f,-.13f),new Vector3(.58f,.58f,.39f),iron);
            Shape.Part("Nose",PrimitiveType.Sphere,head,new Vector3(0,.12f,.22f),new Vector3(.07f,.11f,.1f),skin*.92f);
            motion.Eyes=new Transform[2];
            for(int s=-1;s<=1;s+=2){
                motion.Eyes[(s+1)/2]=Shape.Part("Eye",PrimitiveType.Sphere,head,new Vector3(s*.09f,.17f,.2f),new Vector3(.066f,.066f,.055f),iron).transform;
                Shape.Part("Brow",PrimitiveType.Cube,head,new Vector3(s*.09f,.235f,.19f),new Vector3(.105f,.028f,.035f),iron);
            }
            motion.Mouth=Shape.Part("Mouth",PrimitiveType.Sphere,head,new Vector3(0,.01f,.2f),new Vector3(.12f,.029f,.03f),new Color(.29f,.12f,.1f)).transform;
            for(int s=-1;s<=1;s+=2){
                var shoulder=new GameObject(s<0?"Left shoulder":"Right shoulder").transform;shoulder.SetParent(root,false);shoulder.localPosition=new Vector3(s*.32f,1.43f,0);
                Shape.Beam(shoulder,Vector3.zero,Vector3.down*.34f,.19f,Definition.accent);
                var elbow=new GameObject("Elbow joint").transform;elbow.SetParent(shoulder,false);elbow.localPosition=Vector3.down*.34f;
                Shape.Beam(elbow,Vector3.zero,Vector3.down*.29f,.16f,Definition.accent);
                Shape.Part("Hand",PrimitiveType.Sphere,elbow,new Vector3(0,-.33f,0),new Vector3(.17f,.22f,.13f),skin);
                if(s<0){motion.LeftArm=shoulder;motion.LeftForearm=elbow;}else{
                    motion.RightArm=shoulder;motion.RightForearm=elbow;
                    var book=new GameObject("Notebook joint").transform;book.SetParent(elbow,false);book.localPosition=new Vector3(0,-.36f,.08f);motion.Book=book;
                    Box("Field notebook cover",book,Vector3.zero,new Vector3(.3f,.035f,.39f),wood);
                    Box("Field notebook paper",book,new Vector3(0,.024f,0),new Vector3(.27f,.03f,.36f),ivory);
                    for(int line=0;line<5;line++)Box("Notebook ink",book,new Vector3(0,.041f,-.12f+line*.05f),new Vector3(.2f,.002f,.008f),iron);
                }
            }
            if(Region==6){Shape.Part("Engineer goggles",PrimitiveType.Cube,head,new Vector3(0,.23f,.22f),new Vector3(.34f,.13f,.06f),new Color(.43f,.73f,.8f));}
            if(Region==5)Shape.Part("Fur hat",PrimitiveType.Cylinder,head,new Vector3(0,.41f,-.02f),new Vector3(.57f,.11f,.51f),ivory);
            if(Region==4)Shape.Part("Diver's cap",PrimitiveType.Cylinder,head,new Vector3(0,.39f,-.01f),new Vector3(.51f,.075f,.47f),new Color(.78f,.36f,.16f));
            motion.Initialize(Region);
            Sign(Scenery,p+new Vector3(0,3.1f,-.25f),Definition.npc,"STORY / E",0,4.8f);
            for(int i=0;i<5;i++){float x=p.x+(i-2)*1.2f,z=p.z+2.6f;Box("Guide approach",Scenery,new Vector3(x,Height(x,z)+.025f,z),new Vector3(1.1f,.07f,.65f),wood);}
        }
        void BuildExplorationSites()
        {
            for(int i=0;i<3;i++){
                var p=SitePoints[i];p.y=Height(p.x,p.z);var root=new GameObject("Exploration site "+i).transform;root.SetParent(transform);root.position=p;
                var display=new GameObject("Investigation equipment alcove").transform;display.SetParent(root,false);display.localPosition=Layout.DecorationOffset(5+i);
                Transform item=BuildInvestigationProp(display,i);
                if(Region==5&&i==1)root.gameObject.AddComponent<RescuePodVisibility>().ClosedShell=display.gameObject;
                var chapter=NarrativeContent.Chapter(Region+1);
                Sign(root,p+new Vector3(0,3.8f,-.3f),i==0?chapter.siteA:i==1?chapter.siteB:Definition.secret,i==2?"SECRET / E":"INVESTIGATE / E",0,3.3f);
                var marker=root.gameObject.AddComponent<IslandMarker>();marker.Index=i;marker.Object=item;
                var gl=item.gameObject.AddComponent<Light>();gl.color=Definition.accent;gl.range=4;gl.intensity=.65f;
                // Sparse stepping stones expose a walkable route to each objective.
                // Branches and bridges are authored by IslandLayouts; no radial trail crosses a lagoon.
            }
        }
        void ThemePlant(Transform parent,Vector3 p,float height)
        {
            Color trunk=new Color(.35f,.26f,.17f);var m=new CoastalMesh();
            if(Region==1){CoastalVegetation.Palm(parent,p,height,parent==Scenery);return;}
            if(Region==2){m.Cone(p,.25f,height*.7f,trunk,8,.13f);for(int i=0;i<5;i++){float a=i*Mathf.PI*.4f;Shape.Beam(parent,p+Vector3.up*1.4f,p+new Vector3(Mathf.Cos(a)*1.3f,0,Mathf.Sin(a)*1.3f),.13f,trunk);}m.Build("Mangrove trunk",parent);for(int i=0;i<3;i++)Shape.Rock(parent,p+new Vector3((i-1)*.9f,height*.63f,0),new Vector3(height*.3f,height*.2f,height*.28f),new Color(.19f,.36f,.26f),9);return;}
            if(Region==3){m.Cone(p,.3f,height*.5f,new Color(.34f,.43f,.27f),7,.23f);m.Build("Salt cactus",parent);Shape.Beam(parent,p+Vector3.up*height*.23f,p+new Vector3(.9f,height*.3f,0),.25f,new Color(.34f,.43f,.27f));Shape.Beam(parent,p+new Vector3(.9f,height*.3f,0),p+new Vector3(.9f,height*.48f,0),.25f,new Color(.34f,.43f,.27f));return;}
            if(Region==4){m.Cone(p,.2f,height*.8f,trunk*.7f,7,.045f);m.Build("Dead mast tree",parent);for(int i=0;i<3;i++)Shape.Beam(parent,p+Vector3.up*height*(.35f+i*.15f),p+new Vector3((i%2==0?-1:1)*1.3f,height*(.65f+i*.1f),.4f),.12f,trunk*.7f);return;}
            Color c=Region==7?new Color(.22f,.19f,.2f):Color.Lerp(Definition.accent,iron,.45f);
            if(Region==6){
                // Wind-sheared trees tell the prevailing storm direction.
                var stem=new[]{p,p+new Vector3(-.18f,height*.3f,0),p+new Vector3(.8f,height*.62f,0),p+new Vector3(2,height*.77f,.2f)};
                CoastalMesh.Tube("Storm-bent ironwood",parent,stem,new[]{.22f,.17f,.1f,.02f},trunk*.7f,trunk*.55f,7);
                for(int i=0;i<3;i++){Vector3 a=p+new Vector3(.4f,height*(.38f+i*.12f),0);Shape.Beam(parent,a,a+new Vector3(1.8f+i*.25f,.6f,.4f*(i-1)),.075f,trunk*.6f);}
                return;
            }
            if(Region==7){
                for(int n=0;n<3;n++){
                    Vector3 at=p+new Vector3((n-1)*.6f,0,Mathf.Sin(n)*.35f);float top=height*(.35f+n*.13f);
                    m.Cone(at,.42f,top,c*(.87f+n*.06f),6,.36f);
                    for(int seam=1;seam<4;seam++)CoastalMesh.Ring(parent,at+Vector3.up*(top*seam/4),.385f,.02f,Definition.accent*.7f,Quaternion.Euler(90,0,0));
                }
                m.Build("Hexagonal cooled basalt organ",parent);return;
            }
            // Mirror-reed fans have broad reflective faces rather than identical
            // pine silhouettes. A fractured crown points toward the temple.
            for(int n=0;n<5;n++){
                float angle=(n-2)*23;Quaternion q=Quaternion.Euler(0,n*37,angle);
                Vector3 a=p+q*new Vector3(-.22f,0,0),b=p+q*new Vector3(.22f,0,0),tip=p+q*Vector3.up*height*(.47f+Mathf.Sin(n)*.09f);
                Color face=Color.Lerp(c,Definition.accent,.25f+n*.08f);m.Tri(a,tip,b,face);m.Tri(b,tip,a,face*.7f);
                Shape.Beam(parent,p,tip,.035f,ivory*.65f);
            }
            m.Build("Fractured mirror-reed fan",parent);
        }
        void BuildLandmark()
        {
            var root=new GameObject(Definition.name+" landmark").transform;root.SetParent(Scenery);root.position=Layout.Landmark;root.position=new Vector3(root.position.x,Height(root.position.x,root.position.z),root.position.z);
            Color stone=Color.Lerp(Definition.ground,iron,.4f),accent=Definition.accent;
            switch(Region){
                case 0:
                    for(int i=0;i<7;i++)Shape.Part("Lighthouse masonry",PrimitiveType.Cylinder,root,new Vector3(0,1+i*1.5f,0),new Vector3(3.5f-i*.18f,.75f,3.5f-i*.18f),i%2==0?ivory:orange,true).layer=8;
                    Shape.Part("Lantern",PrimitiveType.Cylinder,root,new Vector3(0,11.4f,0),new Vector3(2.4f,.9f,2.4f),accent,false,true);Shape.Rock(root,new Vector3(0,12.3f,0),new Vector3(2,1.5f,2),iron);break;
                case 1:
                    for(int side=-1;side<=1;side+=2)for(int j=0;j<5;j++){Vector3 at=new Vector3(side*(4-j*.65f),j*1.4f,0);Shape.Rock(root,at,new Vector3(1.1f,1.6f,1.1f),accent,7);CoastalMesh.Ring(root,at+Vector3.forward*.5f,.55f,.18f,new Color(.85f,.69f,.51f),Quaternion.identity);}Shape.Rock(root,new Vector3(0,7.4f,0),new Vector3(2.8f,.85f,1.5f),accent);break;
                case 2:
                    for(int side=-1;side<=1;side+=2)Shape.Beam(root,new Vector3(side*3,0,0),new Vector3(side*3,7,0),.8f,wood);Box("Treehouse deck",root,new Vector3(0,5,0),new Vector3(8,.25f,6),wood,true);Shack(root.position+Vector3.up*5,0,false);break;
                case 3:
                    for(int j=0;j<5;j++)Box("Temple step",root,new Vector3(0,j*.5f,0),new Vector3(10-j*1.3f,.5f,8-j),stone,true);for(int side=-1;side<=1;side+=2){Shape.Part("Temple column",PrimitiveType.Cylinder,root,new Vector3(side*3,5,0),new Vector3(1.1f,3,1.1f),ivory,true).layer=8;Shape.Rock(root,new Vector3(side*7,1,2),new Vector3(1,6,1),accent,4);}Box("Temple lintel",root,new Vector3(0,8,0),new Vector3(8,1.2f,2),stone,true);break;
                case 4:
                    for(int j=0;j<12;j++){float z=j-6;for(int side=-1;side<=1;side+=2){Shape.Beam(root,new Vector3(0,-.2f,z),new Vector3(side*4,3,z),.3f,wood);Shape.Beam(root,new Vector3(side*4,3,z),new Vector3(side*4.3f,5,z),.25f,wood);}}Shape.Beam(root,new Vector3(0,0,-1),new Vector3(3,11,-3),.5f,wood);Box("Torn sail",root,new Vector3(2,7,-2),new Vector3(5,4,.1f),ivory*.7f);break;
                case 5:
                    for(int side=-1;side<=1;side+=2){Shape.Rock(root,new Vector3(side*6,0,0),new Vector3(4,10,5),new Color(.62f,.81f,.85f),7);Shape.Rock(root,new Vector3(side*2.6f,8,0),new Vector3(4,2,3),new Color(.78f,.9f,.91f),6);}for(int j=0;j<5;j++)Box("Ice stepping platform",root,new Vector3((j-2)*2,0,6),new Vector3(1.8f,.4f,2),new Color(.7f,.85f,.88f),true);break;
                case 6:
                    for(int side=-1;side<=1;side+=2){var p=new Vector3(side*4,0,0);for(int i=0;i<4;i++)Shape.Beam(root,p+new Vector3(Mathf.Cos(i*Mathf.PI/2)*1.6f,0,Mathf.Sin(i*Mathf.PI/2)*1.6f),p+Vector3.up*12,.15f,iron);CoastalMesh.Ring(root,p+Vector3.up*10,1.7f,.13f,accent,Quaternion.Euler(90,0,0));Shape.Part("Lightning core",PrimitiveType.Sphere,root,p+Vector3.up*11,Vector3.one*.8f,accent,false,true);}break;
                case 7:
                    for(int i=0;i<14;i++){float a=i*Mathf.PI/7;Shape.Rock(root,new Vector3(Mathf.Cos(a)*7,0,Mathf.Sin(a)*5),new Vector3(2.5f,6+Mathf.Sin(a)*2,2.5f),stone,6);}Shape.Part("Lava basin",PrimitiveType.Cylinder,root,Vector3.up*2.3f,new Vector3(10,.15f,8),accent,false,true);for(int j=0;j<12;j++)Box("Obsidian bridge",root,new Vector3(0,2.6f,8-j),new Vector3(2.2f,.25f,.9f),iron,true);break;
                case 8:
                    for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Shape.Rock(root,new Vector3(Mathf.Cos(a)*6,0,Mathf.Sin(a)*6),new Vector3(.7f,7,1),stone,5);Shape.Part("Temple rune",PrimitiveType.Cube,root,new Vector3(Mathf.Cos(a)*6,5.5f,Mathf.Sin(a)*6+.4f),new Vector3(.25f,.8f,.08f),accent,false,true);}CoastalMesh.Ring(root,new Vector3(0,6,0),4,.25f,accent,Quaternion.identity);Shape.Part("Tidal machine core",PrimitiveType.Sphere,root,new Vector3(0,6,0),Vector3.one*2,accent,false,true);break;
            }
        }
    }
    public sealed class RescuePodVisibility : MonoBehaviour
    {
        public GameObject ClosedShell;
        void Update(){var g=GameDirector.Instance;if(!g||!ClosedShell||g.Run==null)return;bool opened=g.Run.stage==6&&g.Run.questStep>=3;ClosedShell.SetActive(!opened);}
    }
    public class IslandMarker : MonoBehaviour
    {
        public int Index;public Transform Object;
        void Update(){if(!Object)return;var g=GameDirector.Instance;if(!g)return;bool complete=(g.Run.exploredMask&(1<<Index))!=0;Object.gameObject.SetActive(!complete);Object.localRotation=Quaternion.Euler(0,Time.time*30,0);Object.localPosition=new Vector3(0,2.8f+Mathf.Sin(Time.time*2)*.1f,0);}
    }
}

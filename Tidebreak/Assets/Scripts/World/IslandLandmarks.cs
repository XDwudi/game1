using UnityEngine;
namespace Tidebreak
{
    public partial class SeaWorld
    {
        bool ClearTrail(float x,float z)
        {
            var p=new Vector2(x,z);if(Mathf.Abs(x)<3.5f||Mathf.Abs(x)<11&&z> -12)return true;
            var routeStart=new Vector2(Definition.site1.x,Definition.site1.z);var routeEnd=new Vector2(Definition.site2.x,Definition.site2.z);var routeDelta=routeEnd-routeStart;
            float routeT=Mathf.Clamp01(Vector2.Dot(p-routeStart,routeDelta)/Mathf.Max(.1f,routeDelta.sqrMagnitude));
            if(Vector2.Distance(p,routeStart+routeDelta*routeT)<2.3f)return true;
            foreach(var v in new[]{SellPoint,ShopPoint,QuestPoint,Definition.site1,Definition.site2,Definition.site3}){
                var b=new Vector2(v.x,v.z);if(Vector2.Distance(p,b)<6)return true;
                var a=new Vector2(0,2);var d=b-a;float t=Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude);if(Vector2.Distance(p,a+d*t)<1.8f)return true;
            }return false;
        }
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
                Box("Relic pedestal",root,new Vector3(0,.35f,0),new Vector3(1.3f,.7f,1.3f),Color.Lerp(Definition.accent,iron,.65f),true);
                var item=Shape.Part(i==2?"Secret rune chest":"Navigation relic",i==0?PrimitiveType.Cylinder:PrimitiveType.Cube,root,new Vector3(0,1.1f,0),new Vector3(.48f,.35f,.48f),Definition.accent,false,true);
                CoastalMesh.Ring(root,new Vector3(0,.8f,0),.7f,.035f,Definition.accent,Quaternion.Euler(90,0,0));
                var chapter=NarrativeContent.Chapter(Region+1);
                Sign(root,p+new Vector3(0,2.4f,-.3f),i==0?chapter.siteA:i==1?chapter.siteB:Definition.secret,i==2?"SECRET / E":"INVESTIGATE / E",0,3.3f);
                var marker=root.gameObject.AddComponent<IslandMarker>();marker.Index=i;marker.Object=item.transform;
                var gl=item.AddComponent<Light>();gl.color=Definition.accent;gl.range=4;gl.intensity=.65f;
                // Sparse stepping stones expose a walkable route to each objective.
                Vector3 from=new Vector3(0,0,1);for(int k=3;k<18;k++){Vector3 at=Vector3.Lerp(from,p,k/18f);at.y=Height(at.x,at.z)+.035f;Box("Trail stone",Scenery,at,new Vector3(.4f,.06f,.32f),Color.Lerp(ivory,Definition.ground,.4f));}
            }
        }
        void ThemePlant(Transform parent,Vector3 p,float height)
        {
            Color trunk=new Color(.35f,.26f,.17f);var m=new CoastalMesh();
            if(Region==1){m.Cone(p,.17f,height*.75f,trunk,7,.08f);m.Build("Palm trunk",parent);for(int i=0;i<7;i++){float a=i*Mathf.PI*2/7;Vector3 v=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));var leaves=new CoastalMesh();var top=p+Vector3.up*height*.75f;leaves.Tri(top,top+v*height*.42f+Vector3.down*.5f,top+Quaternion.Euler(0,18,0)*v*height*.35f+Vector3.up*.3f,new Color(.3f,.52f,.3f));leaves.Build("Palm frond",parent);}return;}
            if(Region==2){m.Cone(p,.25f,height*.7f,trunk,8,.13f);for(int i=0;i<5;i++){float a=i*Mathf.PI*.4f;Shape.Beam(parent,p+Vector3.up*1.4f,p+new Vector3(Mathf.Cos(a)*1.3f,0,Mathf.Sin(a)*1.3f),.13f,trunk);}m.Build("Mangrove trunk",parent);for(int i=0;i<3;i++)Shape.Rock(parent,p+new Vector3((i-1)*.9f,height*.63f,0),new Vector3(height*.3f,height*.2f,height*.28f),new Color(.19f,.36f,.26f),9);return;}
            if(Region==3){m.Cone(p,.3f,height*.5f,new Color(.34f,.43f,.27f),7,.23f);m.Build("Salt cactus",parent);Shape.Beam(parent,p+Vector3.up*height*.23f,p+new Vector3(.9f,height*.3f,0),.25f,new Color(.34f,.43f,.27f));Shape.Beam(parent,p+new Vector3(.9f,height*.3f,0),p+new Vector3(.9f,height*.48f,0),.25f,new Color(.34f,.43f,.27f));return;}
            if(Region==4){m.Cone(p,.2f,height*.8f,trunk*.7f,7,.045f);m.Build("Dead mast tree",parent);for(int i=0;i<3;i++)Shape.Beam(parent,p+Vector3.up*height*(.35f+i*.15f),p+new Vector3((i%2==0?-1:1)*1.3f,height*(.65f+i*.1f),.4f),.12f,trunk*.7f);return;}
            Color c=Region==7?new Color(.22f,.19f,.2f):Color.Lerp(Definition.accent,iron,.35f);
            m.Cone(p,height*.15f,height*.65f,c,5,0);m.Cone(p+Vector3.right*.5f,height*.09f,height*.4f,c*.8f,5,0);m.Build(Region==7?"Basalt columns":"Resonant crystal",parent);
        }
        void BuildLandmark()
        {
            var root=new GameObject(Definition.name+" landmark").transform;root.SetParent(Scenery);root.position=new Vector3(0,Height(0,-34),-34);
            Color stone=Color.Lerp(Definition.ground,iron,.4f),accent=Definition.accent;
            switch(Region){
                case 0:
                    for(int i=0;i<7;i++)Shape.Part("Lighthouse masonry",PrimitiveType.Cylinder,root,new Vector3(0,1+i*1.5f,0),new Vector3(3.5f-i*.18f,.75f,3.5f-i*.18f),i%2==0?ivory:orange,true).layer=8;
                    Shape.Part("Lantern",PrimitiveType.Cylinder,root,new Vector3(0,11.4f,0),new Vector3(2.4f,.9f,2.4f),accent,false,true);Shape.Rock(root,new Vector3(0,12.3f,0),new Vector3(2,1.5f,2),iron);break;
                case 1:
                    for(int side=-1;side<=1;side+=2)for(int j=0;j<5;j++){Vector3 at=new Vector3(side*(4-j*.65f),j*1.4f,0);Shape.Rock(root,at,new Vector3(1.1f,1.6f,1.1f),accent,7);CoastalMesh.Ring(root,at+Vector3.forward*.5f,.55f,.18f,new Color(.85f,.69f,.51f),Quaternion.identity);}Shape.Rock(root,new Vector3(0,7.4f,0),new Vector3(2.8f,.85f,1.5f),accent);break;
                case 2:
                    for(int side=-1;side<=1;side+=2)Shape.Beam(root,new Vector3(side*3,0,0),new Vector3(side*3,7,0),.8f,wood);Box("Treehouse deck",root,new Vector3(0,5,0),new Vector3(8,.25f,6),wood,true);for(int i=0;i<7;i++)Box("Suspended bridge plank",root,new Vector3(0,1+i*.5f,9-i),new Vector3(2,.2f,.9f),wood,true);Shack(root.position+Vector3.up*5,0,false);break;
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
    public class IslandMarker : MonoBehaviour
    {
        public int Index;public Transform Object;
        void Update(){if(!Object)return;var g=GameDirector.Instance;if(!g)return;bool complete=(g.Run.exploredMask&(1<<Index))!=0;Object.gameObject.SetActive(!complete);Object.localRotation=Quaternion.Euler(0,Time.time*30,0);Object.localPosition=new Vector3(0,1.1f+Mathf.Sin(Time.time*2)*.1f,0);}
    }
}

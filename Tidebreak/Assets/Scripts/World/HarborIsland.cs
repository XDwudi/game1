using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Tidebreak
{
    public partial class SeaWorld : MonoBehaviour
    {
        public Transform Boat,Scenery;
        public int Region;
        public Material Ocean;
        public IslandDefinition Definition {get{return ExpeditionContent.Islands[Mathf.Clamp(Region,0,8)];}}
        public IslandLayout Layout {get{return IslandLayouts.Get(Region);}}
        public Vector3 SellPoint {get{return Layout.Points[2];}}
        public Vector3 QuestPoint {get{return Layout.Points[4];}}
        public Vector3[] SitePoints {get{return new[]{Layout.Points[5],Layout.Points[6],Layout.Points[7]};}}
        public Vector3 ShopPoint {get{return Layout.Points[3];}}
        public Vector3 ChartPoint {get{var p=Layout.Points[1]+new Vector3(4.4f,0,1.8f);p.y=Height(p.x,p.z);return p;}}
        public Vector3 Spawn {get{return new Vector3(0,3.3f,8);}}
        public Vector3[] NavigationRoute(Vector3 from,Vector3 to)
        {var route=Layout.FindRoute(from,to);for(int i=0;i<route.Length;i++)route[i].y=GroundAt(route[i]);return route;}
        public const int GroundMask=1<<8;
        Light sun;
        Material skyMaterial;
        Transform beacon,boatFloat;
        Transform[] biomeDetails=new Transform[3];
        readonly List<Transform> birds=new List<Transform>();
        readonly Color wood=new Color(.39f,.25f,.15f),ivory=new Color(.86f,.85f,.68f),iron=new Color(.13f,.22f,.24f),orange=new Color(.87f,.35f,.12f);
        public float Height(float x,float z){return Layout.Height(x,z);}
        public float GroundAt(Vector3 p)
        {
            RaycastHit hit;
            if(Physics.Raycast(new Vector3(p.x,25,p.z),Vector3.down,out hit,50,GroundMask))return hit.point.y;
            return -.65f;
        }
        public bool IsWater(Vector3 p){return Height(p.x,p.z)<-.1f;}
        public void Build()
        {
            Random.InitState(91371+Region*217);
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientEquatorColor=new Color(.57f,.66f,.65f);RenderSettings.ambientGroundColor=new Color(.37f,.37f,.3f);
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.0037f;
            var light=new GameObject("Sun");light.transform.SetParent(transform);sun=light.AddComponent<Light>();
            sun.type=LightType.Directional;sun.shadows=LightShadows.Soft;sun.shadowStrength=.7f;sun.shadowBias=.04f;light.transform.rotation=Quaternion.Euler(38,-36,0);RenderSettings.sun=sun;
            skyMaterial=new Material(Resources.Load<Shader>("CoastalSky"));skyMaterial.SetVector("_SunDirection",-sun.transform.forward);RenderSettings.skybox=skyMaterial;
            Ocean=new Material(Resources.Load<Shader>("Ocean"));
            BuildWater();
            Scenery=new GameObject(Layout.Name).transform;Scenery.SetParent(transform);
            BuildTerrain();BuildRouteArchitecture();BuildPier();BuildVillage();BuildWilds();BuildDistantIslands();BuildBoat();BuildBiomes();BuildExplorationSites();
            for(int i=0;i<8;i++) {
                var bird=new GameObject("Gull").transform;bird.SetParent(transform);
                var mesh=new CoastalMesh();
                mesh.Tri(new Vector3(-.95f,.08f,0),new Vector3(0,0,.16f),new Vector3(-.2f,0,-.18f),ivory);
                mesh.Tri(new Vector3(.95f,.08f,0),new Vector3(.2f,0,-.18f),new Vector3(0,0,.16f),ivory);
                mesh.Tri(new Vector3(-.95f,.08f,0),new Vector3(-.2f,0,-.18f),new Vector3(0,0,.16f),ivory);
                mesh.Tri(new Vector3(.95f,.08f,0),new Vector3(0,0,.16f),new Vector3(.2f,0,-.18f),ivory);
                mesh.Build("Wings",bird);birds.Add(bird);
            }
            SetAct(Region);Physics.SyncTransforms();
            StaticBatchingUtility.Combine(Scenery.gameObject);
            CoastalHabitatArt.Build(transform,Region+1);
            gameObject.AddComponent<IslandRestoration>().Init(this);
        }
        void BuildWater()
        {
            var mesh=new Mesh{name="Ocean grid"};var vs=new Vector3[161*161];var ts=new List<int>();
            for(int z=0;z<=160;z++)for(int x=0;x<=160;x++)vs[z*161+x]=new Vector3((x-80)*4,-.6f,(z-80)*4);
            for(int z=0;z<160;z++)for(int x=0;x<160;x++){int n=z*161+x;ts.AddRange(new[]{n,n+161,n+1,n+1,n+161,n+162});}
            mesh.vertices=vs;mesh.triangles=ts.ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();
            var g=new GameObject("Tidal water");g.transform.SetParent(transform);g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<RuntimeMeshOwner>().Owned=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=Ocean;
        }
        void BuildTerrain()
        {
            var mesh=new CoastalMesh();
            for(int z=-74;z<26;z++)for(int x=-50;x<50;x++) {
                var a=new Vector3(x,Height(x,z),z);var b=new Vector3(x+1,Height(x+1,z),z);
                var c=new Vector3(x,Height(x,z+1),z+1);var d=new Vector3(x+1,Height(x+1,z+1),z+1);
                float h=(a.y+b.y+c.y+d.y)*.25f;
                bool path=Layout.IsTrail(x,z,-.6f);
                float slope=Mathf.Max(Mathf.Abs(a.y-b.y),Mathf.Abs(a.y-c.y));
                Color sand=Region==5?new Color(.64f,.78f,.82f):Region>=6?new Color(.38f,.4f,.43f):new Color(.71f,.66f,.48f);
                Color trail=Region==5?new Color(.5f,.65f,.69f):Region>=6?new Color(.46f,.48f,.48f):Region==3?new Color(.67f,.57f,.41f):new Color(.57f,.48f,.33f);
                Color col=h<.65f?sand:path?trail:slope>.65f?Color.Lerp(Definition.ground,iron,.5f):Definition.ground;
                if(!path&&slope>.65f)col*=1+Mathf.Sin(h*5.2f)*.07f;
                col*=Random.Range(.98f,1.02f)*Mathf.Lerp(.96f,1.04f,Mathf.PerlinNoise(x*.045f+Region*3,z*.045f));col.a=1;
                mesh.Tri(a,c,b,col);mesh.Tri(b,c,d,col);
            }
            mesh.Build("Grass, paths and tidal sand",Scenery,true);
            BuildCoastFoam();
        }

        GameObject Box(string name,Transform p,Vector3 pos,Vector3 size,Color c,bool collision=false)
        {var g=Shape.Part(name,PrimitiveType.Cube,p,pos,size,c,collision);if(collision)g.layer=8;string n=name.ToLowerInvariant();if(n.Contains("plank")||n.Contains("siding")||n.Contains("counter")||n=="crate"||n.Contains("bench")||n.Contains("deck")||n.Contains("shelf")||n=="foundation")g.GetComponent<Renderer>().sharedMaterial=Shape.Wood(c);return g;}
        void BuildPier()
        {
            Boat=new GameObject("Old timber pier").transform;Boat.SetParent(Scenery);
            for(int z=8;z<30;z++) {
                Box("Weathered pier plank",Boat,new Vector3(0,1.32f,z*.77f+3),new Vector3(5,.25f,.74f),wood*Random.Range(.9f,1.14f),true);
                for(int s=-1;s<=1;s+=2)for(int n=0;n<2;n++)Box("Iron nail",Boat,new Vector3(s*2.17f,1.452f,z*.77f+2.79f+n*.4f),new Vector3(.04f,.015f,.04f),iron);
            }
            for(int z=12;z<27;z+=4)for(int s=-1;s<=1;s+=2) {
                var post=Shape.Part("Pier piling",PrimitiveType.Cylinder,Boat,new Vector3(s*2.65f,.15f,z),new Vector3(.39f,1.7f,.39f),wood,true);post.layer=8;
                for(int n=0;n<4;n++)CoastalMesh.Ring(Boat,new Vector3(s*2.65f,1.38f+n*.055f,z),.22f,.025f,new Color(.65f,.57f,.35f),Quaternion.Euler(90,0,0));
                if(z<23&&s==-1)Shape.Beam(Boat,new Vector3(s*2.65f,1.75f,z),new Vector3(s*2.65f,1.75f,z+4),.048f,new Color(.55f,.48f,.3f));
            }
            Sign(Scenery,new Vector3(-3,1.6f,10.5f),"钓 鱼 码 头","HOLD LMB TO CAST",0,2.4f);
            Shape.Beam(Boat,new Vector3(-2.15f,1.45f,24.3f),new Vector3(-2.15f,4.4f,24.3f),.09f,wood);
            Shape.Beam(Boat,new Vector3(-2.15f,4.4f,24.3f),new Vector3(-1.5f,4.4f,24.3f),.075f,wood);
            CoastalMesh.Tube("Hunt bell",Boat,new[]{new Vector3(-1.5f,4.27f,24.3f),new Vector3(-1.5f,4.1f,24.3f),new Vector3(-1.5f,3.85f,24.3f),new Vector3(-1.5f,3.78f,24.3f)},new[]{.06f,.17f,.25f,.33f},new Color(.69f,.5f,.22f),new Color(.44f,.31f,.13f),12);
            Shape.Beam(Boat,new Vector3(-1.5f,3.9f,24.3f),new Vector3(-1.5f,2.8f,24.3f),.018f,ivory);
            Sign(Boat,new Vector3(-2.15f,2.25f,24.3f),"猎潮钟","BOSS / E",0,1.15f);
            for(int i=0;i<3;i++)Crate(new Vector3(2.9f,1.5f,9+i*.85f),.72f);
            var bucket=Shape.Part("Bait bucket",PrimitiveType.Cylinder,Scenery,new Vector3(1.9f,1.83f,24),new Vector3(.65f,.38f,.65f),iron);
            Shape.Part("Bucket water",PrimitiveType.Cylinder,bucket.transform,new Vector3(0,1.02f,0),new Vector3(.83f,.025f,.83f),new Color(.15f,.44f,.48f));
        }
        void BuildVillage()
        {
            for(int i=0;i<2;i++){
                int firstChild=Scenery.childCount;Vector3 courtyard=i==0?SellPoint:ShopPoint;
                Vector3 p=courtyard+Vector3.back*3.4f;p.y=courtyard.y+.08f;
                Shack(p+Vector3.back*2.7f,0,i==1);NPC(p+Vector3.back*1.5f,i==1);
                Sign(Scenery,p+new Vector3(0,3.25f,.2f),i==0?"鱼获收购":"岛屿工坊",i==0?"SELL YOUR CATCH / E":"BLUEPRINTS & GEAR / E",0,4.8f);
                Box(i==0?"Fish counter":"Workshop counter",Scenery,p+Vector3.up*.55f,new Vector3(4.5f,1.1f,1),wood,true);
                if(i==0){Box("Market ice",Scenery,p+Vector3.up*1.12f,new Vector3(2,.08f,.7f),new Color(.65f,.85f,.82f));var display=CreatureArt.Build(new GameObject("Market specimen").transform,CreatureKind.Snapper,false);display.parent.SetParent(Scenery);display.parent.position=p+Vector3.up*1.4f;display.parent.localScale=Vector3.one*.38f;display.parent.rotation=Quaternion.Euler(0,90,85);foreach(var c in display.GetComponentsInChildren<Collider>()){c.enabled=false;Destroy(c);}}
                else {Box("Anvil",Scenery,p+Vector3.up*1.25f,new Vector3(.8f,.25f,.5f),iron);for(int j=0;j<3;j++)Crate(p+new Vector3(3.7f,-.15f,-1-j),.75f);}
                Barrel(p+new Vector3(-3.7f,-.05f,0));
                // Orient the whole shop ensemble into a reserved roadside court.
                // Rotating only the shack left crates and porch posts across roads.
                int lastChild=Scenery.childCount;var pieces=new List<Transform>();for(int child=firstChild;child<lastChild;child++)pieces.Add(Scenery.GetChild(child));
                var court=new GameObject(i==0?"Market courtyard":"Workshop courtyard").transform;court.SetParent(Scenery,false);court.position=courtyard;
                foreach(var piece in pieces)piece.SetParent(court,true);court.rotation=Quaternion.Euler(0,Layout.MerchantYaw(i==0?2:3),0);
            }
            Sign(Scenery,ChartPoint+Vector3.up*1.4f,"群岛航图","CHART / E    MAP / M",160,2.4f);
            Box("Chart pedestal",Scenery,ChartPoint+Vector3.up*.4f,new Vector3(.24f,1.2f,.24f),wood,true);
            BuildGuide();
            for(int s=-1;s<=1;s+=2)Shape.Beam(Scenery,new Vector3(s*4,Height(s*4,-3),-3),new Vector3(s*4,4.7f,-3),.12f,wood);
            for(int i=0;i<9;i++)Shape.Part("Village lantern",PrimitiveType.Sphere,Scenery,new Vector3(i-4,4.7f-Mathf.Sin(i/8f*Mathf.PI)*.7f,-3),Vector3.one*.17f,new Color(1,.79f,.4f),false,true);
            Shape.Beam(Scenery,new Vector3(-4,4.7f,-3),new Vector3(4,4.7f,-3),.025f,iron);
        }
        void Shack(Vector3 position,float angle,bool workshop)
        {
            var r=new GameObject(workshop?"Captain's workshop":"Fish market").transform;r.SetParent(Scenery);r.position=position;r.rotation=Quaternion.Euler(0,angle,0);
            Color wall=Color.Lerp(workshop?new Color(.24f,.39f,.41f):new Color(.61f,.42f,.24f),Definition.accent,.4f);
            Box("Foundation",r,Vector3.zero,new Vector3(6.1f,.2f,4.7f),wood,true);
            for(int i=0;i<12;i++) {
                Box("Back siding",r,new Vector3(-2.75f+i*.5f,1.5f,-2.2f),new Vector3(.48f,3,.16f),wall*Random.Range(.94f,1.06f),true);
                if(i<8)for(int s=-1;s<=1;s+=2)Box("Side siding",r,new Vector3(s*2.95f,1.5f,-1.9f+i*.5f),new Vector3(.18f,3,.48f),wall,true);
            }
            for(int s=-1;s<=1;s+=2)Box("Porch post",r,new Vector3(s*2.8f,1.5f,2.3f),new Vector3(.22f,3,.22f),wood,true);
            var roof=new CoastalMesh();roof.Quad(new Vector3(-3.4f,3,2.8f),new Vector3(0,4.25f,2.8f),new Vector3(0,4.25f,-2.7f),new Vector3(-3.4f,3,-2.7f),iron);
            roof.Quad(new Vector3(0,4.25f,2.8f),new Vector3(3.4f,3,2.8f),new Vector3(3.4f,3,-2.7f),new Vector3(0,4.25f,-2.7f),iron*.8f);roof.Build("Steep shingle roof",r);
            for(int x=-8;x<=8;x++){float xx=x*.4f;Shape.Beam(r,new Vector3(xx,4.27f-Mathf.Abs(xx)*.3676f,-2.7f),new Vector3(xx,4.27f-Mathf.Abs(xx)*.3676f,2.8f),.022f,new Color(.25f,.34f,.34f));}
            for(int s=-1;s<=1;s+=2) {
                Box("Canvas valance",r,new Vector3(s*1.45f,2.8f,2.65f),new Vector3(2.85f,.4f,.06f),s==-1?ivory:orange);
                Box("Back shelf",r,new Vector3(s*1.8f,1.6f,-1.8f),new Vector3(1.65f,.13f,.75f),wood);
                for(int i=0;i<3;i++)Shape.Part("Stock tin",PrimitiveType.Cylinder,r,new Vector3(s*1.8f+(i-1)*.4f,1.85f,-1.8f),new Vector3(.22f,.2f,.22f),i%2==0?ivory:orange);
            }
        }
        void NPC(Vector3 pos,bool smith)
        {
            var p=new GameObject(smith?"Captain Rowan":"Fishmonger Miro").transform;p.SetParent(Scenery);p.position=pos;
            Color skin=new Color(.7f,.49f,.31f),coat=smith?new Color(.2f,.3f,.35f):new Color(.39f,.43f,.26f);
            for(int s=-1;s<=1;s+=2){Shape.Beam(p,new Vector3(s*.17f,.16f,0),new Vector3(s*.17f,.8f,0),.22f,iron);Box("Boot",p,new Vector3(s*.17f,.08f,.07f),new Vector3(.26f,.2f,.4f),iron);Shape.Beam(p,new Vector3(s*.38f,1.3f,0),new Vector3(s*.45f,.82f,.2f),.23f,coat);Shape.Part("Hand",PrimitiveType.Sphere,p,new Vector3(s*.45f,.8f,.22f),Vector3.one*.21f,skin);}
            Shape.Part("Coat",PrimitiveType.Capsule,p,new Vector3(0,1.05f,0),new Vector3(.75f,.52f,.48f),coat);
            Box("Apron",p,new Vector3(0,1.08f,.27f),new Vector3(.52f,.75f,.08f),orange);
            Shape.Part("Face",PrimitiveType.Sphere,p,new Vector3(0,1.71f,0),new Vector3(.47f,.56f,.43f),skin);
            Shape.Part("Nose",PrimitiveType.Sphere,p,new Vector3(0,1.72f,.25f),new Vector3(.13f,.15f,.18f),skin);
            Shape.Part("Beard",PrimitiveType.Sphere,p,new Vector3(0,1.51f,.15f),new Vector3(.39f,.25f,.3f),smith?new Color(.26f,.22f,.19f):new Color(.67f,.66f,.54f));
            for(int s=-1;s<=1;s+=2){Shape.Part("Eye",PrimitiveType.Sphere,p,new Vector3(s*.1f,1.81f,.195f),Vector3.one*.095f,ivory);Shape.Part("Pupil",PrimitiveType.Sphere,p,new Vector3(s*.1f,1.81f,.239f),Vector3.one*.04f,iron);}
            Shape.Part("Hat brim",PrimitiveType.Cylinder,p,new Vector3(0,2,0),new Vector3(.74f,.035f,.64f),smith?orange:ivory);
            Shape.Part("Hat",PrimitiveType.Cylinder,p,new Vector3(0,2.13f,0),new Vector3(.49f,.13f,.43f),smith?orange:ivory);
        }
        void Sign(Transform parent,Vector3 pos,string title,string caption,float yaw,float width)
        {
            var r=new GameObject(title).transform;r.SetParent(parent);r.position=pos;r.rotation=Quaternion.Euler(0,yaw,0);
            Box("Signboard",r,Vector3.zero,new Vector3(width,.98f,.12f),iron);
            Box("Sign trim",r,new Vector3(0,.47f,.07f),new Vector3(width,.05f,.05f),ivory);
            WorldText(r,title,new Vector3(0,.14f,.075f),width,.34f,ivory);
            WorldText(r,caption,new Vector3(0,-.24f,.075f),width,.13f,new Color(.65f,.79f,.69f));
            WorldText(r,title,new Vector3(0,.14f,-.075f),width,.34f,ivory,true);
            WorldText(r,caption,new Vector3(0,-.24f,-.075f),width,.13f,new Color(.65f,.79f,.69f),true);
            r.gameObject.AddComponent<HarborSignVisibility>();
        }
        void WorldText(Transform p,string value,Vector3 pos,float width,float size,Color color,bool back=false)
        {
            var g=new GameObject(value);g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localRotation=Quaternion.Euler(0,back?0:180,0);
            var text=g.AddComponent<TextMeshPro>();text.font=Resources.Load<TMP_FontAsset>("Fonts/SeaFont");text.text=value;text.fontSize=size*10;text.color=color;text.alignment=TextAlignmentOptions.Center;text.rectTransform.sizeDelta=new Vector2(width,.7f);text.enableWordWrapping=false;
        }
        void Crate(Vector3 pos,float size)
        {var r=new GameObject("Cargo crate").transform;r.SetParent(Scenery);r.position=pos+Vector3.up*size*.5f;Box("Crate",r,Vector3.zero,Vector3.one*size,wood,true);for(int s=-1;s<=1;s+=2){Box("Crate straps",r,new Vector3(s*size*.36f,0,0),new Vector3(.065f,size*1.03f,size*1.03f),ivory*.65f);} }
        void Barrel(Vector3 pos)
        {
            var r=new GameObject("Oak barrel").transform;r.SetParent(Scenery);r.position=pos;
            CoastalMesh.Tube("Barrel staves",r,new[]{Vector3.zero,Vector3.up*.2f,Vector3.up*.7f,Vector3.up*1.2f,Vector3.up*1.4f},new[]{.46f,.51f,.56f,.51f,.46f},wood,wood*.75f,12);
            for(int i=0;i<3;i++)CoastalMesh.Ring(r,Vector3.up*(.2f+i*.5f),i==1?.56f:.51f,.05f,iron,Quaternion.Euler(90,0,0));
            Shape.Part("Barrel lid",PrimitiveType.Cylinder,r,Vector3.up*1.38f,new Vector3(.92f,.04f,.92f),wood);
            var col=r.gameObject.AddComponent<CapsuleCollider>();col.radius=.54f;col.height=1.4f;col.center=Vector3.up*.7f;r.gameObject.layer=8;
        }
        void BuildWilds()
        {
            var grass=new CoastalMesh();
            for(int i=0;i<(Region==3||Region==5||Region==7?900:6500);i++) {
                float x=Random.Range(-39f,39f),z=Random.Range(-64f,8f),y=Height(x,z);
                if(y<1.25f||ClearTrail(x,z))continue;
                if(Vector2.Distance(new Vector2(x,z),new Vector2(-10,-9))<5||Vector2.Distance(new Vector2(x,z),new Vector2(10,-10))<5)continue;
                float h=Random.Range(.15f,.48f),w=Random.Range(.06f,.12f);var p=new Vector3(x,y-.035f,z);Color col=Color.Lerp(new Color(.31f,.43f,.16f),new Color(.65f,.66f,.29f),Random.value);
                grass.Tri(p+Vector3.left*w,p+Vector3.up*h+Vector3.right*.09f,p+Vector3.right*w,col);grass.Tri(p+Vector3.forward*w,p+Vector3.up*h+Vector3.back*.07f,p+Vector3.back*w,col);
            }
            grass.Build("Meadow grasses",Scenery);
            for(int i=0;i<70;i++) {
                float x=Random.Range(-39f,39f),z=Random.Range(-63f,7f),y=Height(x,z);
                if(y<1.2f||ClearTrail(x,z))continue;
                Pine(Scenery,new Vector3(x,y,z),Random.Range(3.8f,7.8f));
            }
            for(int i=0;i<35;i++) {
                float x=Random.Range(-39f,39f),z=Random.Range(-64f,8f),y=Height(x,z);
                if(y<0||ClearTrail(x,z))continue;
                float scale=Random.Range(.45f,1.6f);var r=Shape.Rock(Scenery,new Vector3(x,y-.2f,z),new Vector3(scale,scale*.7f,scale),new Color(.43f,.48f,.43f));r.AddComponent<MeshCollider>();r.layer=8;
            }
            for(int i=0;i<10;i++){float x=Random.Range(-35f,35f),z=Random.Range(-58f,-20f);if(Height(x,z)>1&&!ClearTrail(x,z))Crate(new Vector3(x,Height(x,z),z),.45f);}
        }
        void Pine(Transform parent,Vector3 pos,float height)
        {
            if(Region==0||Region==5){CoastalVegetation.Pine(parent,pos,height,Region==5,parent==Scenery);return;}
            ThemePlant(parent,pos,height);
        }
        void BuildDistantIslands()
        {
            Vector3[] positions={new Vector3(-62,-1,72),new Vector3(74,-1,101),new Vector3(-115,-1,-25),new Vector3(95,-1,-98)};
            for(int j=0;j<positions.Length;j++) {
                var r=new GameObject("Outer island").transform;r.SetParent(Scenery);r.position=Quaternion.Euler(0,Region*37,0)*positions[j];
                Shape.Rock(r,Vector3.zero,new Vector3(23,12,18),new Color(.34f,.43f,.4f),11);
                var cap=Shape.Rock(r,Vector3.up*7,new Vector3(17,4,14),new Color(.28f,.4f,.23f),11);var surface=cap.AddComponent<MeshCollider>();Physics.SyncTransforms();
                for(int i=0;i<12;i++){var p=new Vector3(Random.Range(-10,10),9,Random.Range(-8,8));RaycastHit hit;if(surface.Raycast(new Ray(r.TransformPoint(new Vector3(p.x,25,p.z)),Vector3.down),out hit,40))p.y=r.InverseTransformPoint(hit.point).y-.12f;Pine(r,p,Random.Range(5,10));}Destroy(surface);
                if(j==0&&Region==0){for(int i=0;i<6;i++)Shape.Part("Lighthouse tier",PrimitiveType.Cylinder,r,new Vector3(0,11+i*1.7f,0),new Vector3(2.8f-i*.14f,.85f,2.8f-i*.14f),i%2==0?ivory:orange);Shape.Part("Lantern room",PrimitiveType.Cylinder,r,new Vector3(0,20.8f,0),new Vector3(2.1f,.7f,2.1f),new Color(1,.83f,.44f),false,true);Shape.Rock(r,new Vector3(0,21.5f,0),new Vector3(1.6f,1.2f,1.6f),iron,8);beacon=new GameObject("Beacon").transform;beacon.SetParent(r,false);beacon.localPosition=new Vector3(0,21,0);var l=beacon.gameObject.AddComponent<Light>();l.type=LightType.Spot;l.range=190;l.spotAngle=18;l.color=new Color(1,.8f,.5f);l.intensity=5;}
            }
            for(int i=0;i<16;i++){float a=i*Mathf.PI*2/16;Shape.Rock(Scenery,new Vector3(Mathf.Cos(a)*190,-1,Mathf.Sin(a)*190),new Vector3(18,Random.Range(8,23),14),new Color(.31f,.45f,.45f));}

        }
        void BuildBoat()
        {
            boatFloat=new GameObject("Moored Wayfarer").transform;boatFloat.SetParent(transform);boatFloat.position=new Vector3(7,.2f,19);boatFloat.rotation=Quaternion.Euler(0,-8,0);
            var m=new CoastalMesh();Vector3[] top={new Vector3(-1.6f,.65f,-3.3f),new Vector3(1.6f,.65f,-3.3f),new Vector3(1.8f,.65f,1.7f),new Vector3(0,.65f,4),new Vector3(-1.8f,.65f,1.7f)};
            for(int i=0;i<5;i++){int j=(i+1)%5;m.Quad(top[i],top[j],new Vector3(top[j].x*.55f,-.7f,top[j].z*.85f),new Vector3(top[i].x*.55f,-.7f,top[i].z*.85f),ivory);Shape.Beam(boatFloat,top[i],top[j],.1f,iron);}
            m.Build("Curved painted hull",boatFloat);
            Box("Boat deck",boatFloat,new Vector3(0,.25f,-.3f),new Vector3(2.7f,.12f,5.5f),wood);
            for(int i=0;i<3;i++)Box("Bench",boatFloat,new Vector3(0,.65f,-2+i*1.45f),new Vector3(2.7f,.15f,.45f),wood);
            Box("Outboard",boatFloat,new Vector3(0,.6f,-3.5f),new Vector3(.7f,.8f,.6f),iron);Box("Motor stripe",boatFloat,new Vector3(0,.6f,-3.81f),new Vector3(.72f,.18f,.02f),orange);
            for(int s=-1;s<=1;s+=2)Shape.Beam(boatFloat,new Vector3(s*.7f,.9f,-2),new Vector3(s*.7f,3.7f,-2),.04f,iron);
            Box("Canvas canopy",boatFloat,new Vector3(0,3.75f,-1.1f),new Vector3(2.8f,.07f,3),orange);
            CoastalMesh.Ring(boatFloat,new Vector3(1.82f,.7f,.3f),.45f,.1f,orange,Quaternion.Euler(0,90,0));
        }
        void BuildBiomes(){BuildLandmark();}
        public void SetAct(int act)
        {
            Color accent=Definition.accent;bool night=Region>=6;bool snow=Region==5;
            Ocean.SetColor("_DeepColor",Color.Lerp(new Color(.04f,.23f,.31f),accent,.15f));Ocean.SetColor("_CrestColor",Color.Lerp(new Color(.32f,.65f,.66f),accent,.2f));
            RenderSettings.fogColor=night?Color.Lerp(new Color(.17f,.23f,.34f),accent,.13f):Color.Lerp(new Color(.69f,.8f,.82f),accent,.17f);
            sun.intensity=night?.8f:snow?1.1f:1.25f;sun.color=Region==3||Region==7?new Color(1,.8f,.57f):new Color(.91f,.94f,1);
            skyMaterial.SetColor("_SkyTint",night?Color.Lerp(new Color(.1f,.16f,.28f),accent,.09f):Definition.sky);skyMaterial.SetColor("_Horizon",RenderSettings.fogColor);skyMaterial.SetColor("_Cloud",night?new Color(.4f,.46f,.58f):new Color(.94f,.94f,.86f));skyMaterial.SetFloat("_Exposure",night?.9f:1.15f);
            RenderSettings.ambientSkyColor=night?new Color(.42f,.53f,.65f):new Color(.68f,.76f,.8f);
            CoastalLook.Apply(this,sun,skyMaterial);
        }
        void Update()
        {
            if(beacon)beacon.localRotation=Quaternion.Euler(0,Time.time*13,0);
            if(boatFloat){boatFloat.position=new Vector3(7,.2f+Mathf.Sin(Time.time*1.3f)*.06f,19);boatFloat.rotation=Quaternion.Euler(Mathf.Sin(Time.time)*1.5f,-8,Mathf.Sin(Time.time*.7f)*2);}
            for(int i=0;i<birds.Count;i++){float a=Time.time*.042f+i*1.2f;birds[i].position=new Vector3(Mathf.Cos(a)*(30+i*4),19+Mathf.Sin(a*2+i)*3,Mathf.Sin(a)*(30+i*4));birds[i].rotation=Quaternion.Euler(0,-a*Mathf.Rad2Deg,Mathf.Sin(Time.time*2+i)*8);}
        }
        void OnDestroy(){if(Ocean)Destroy(Ocean);if(skyMaterial)Destroy(skyMaterial);}
    }
}

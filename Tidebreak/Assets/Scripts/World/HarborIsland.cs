using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Tidebreak
{
    public class SeaWorld : MonoBehaviour
    {
        public Transform Boat,Scenery;
        public int Region;
        float CoastWidth {get{return 28+Region*2;}}
        float CoastLength {get{return 33+Region;}}
        public Material Ocean;
        public readonly Vector3 SellPoint=new Vector3(-10,1.1f,-6);
        public readonly Vector3 ShopPoint=new Vector3(10,1.1f,-7);
        public readonly Vector3 ChartPoint=new Vector3(0,1.1f,-15);
        public Vector3 Spawn {get{return new Vector3(0,3.3f,8);}}
        public const int GroundMask=1<<8;
        Light sun;
        Material skyMaterial;
        Transform beacon,boatFloat;
        Transform[] biomeDetails=new Transform[3];
        readonly List<Transform> birds=new List<Transform>();
        readonly Color wood=new Color(.39f,.25f,.15f),ivory=new Color(.86f,.85f,.68f),iron=new Color(.13f,.22f,.24f),orange=new Color(.87f,.35f,.12f);
        public float Height(float x,float z)
        {
            float d=Mathf.Sqrt(x*x/(CoastWidth*CoastWidth)+(z+15)*(z+15)/(CoastLength*CoastLength));
            float edge=.9f+Mathf.Sin(Mathf.Atan2(z+15,x)*(5+Region))*(.035f+Region*.01f);
            if(d>edge)return Mathf.Lerp(.1f,-5,Mathf.Clamp01((d-edge)/.2f));
            float hill=Mathf.PerlinNoise(x*.09f+51,z*.09f+31)*.7f;
            float h=1.25f+hill;
            if(z < -22)h+=Mathf.Clamp01((-z-22)/19)*(4+Region*1.5f)*Mathf.PerlinNoise(x*.05f+3+Region*7,z*.06f+5);
            return Mathf.Lerp(h,.15f,Mathf.Clamp01((d-.72f)/(edge-.72f)));
        }
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
            Scenery=new GameObject("Pinehaven Island").transform;Scenery.SetParent(transform);
            BuildTerrain();BuildPier();BuildVillage();BuildWilds();BuildDistantIslands();BuildBoat();BuildBiomes();
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
            for(int z=-53;z<24;z++)for(int x=-35;x<35;x++) {
                var a=new Vector3(x,Height(x,z),z);var b=new Vector3(x+1,Height(x+1,z),z);
                var c=new Vector3(x,Height(x,z+1),z+1);var d=new Vector3(x+1,Height(x+1,z+1),z+1);
                float h=(a.y+b.y+c.y+d.y)*.25f;
                bool path=Mathf.Abs(x)<2.6f&&z>-20||z>-10&&z<-5&&Mathf.Abs(x)<14;
                Color col=h<.65f?new Color(.7f,.67f,.49f):path?new Color(.59f,.55f,.39f):Region==0?new Color(.37f,.51f,.24f):Region==1?new Color(.34f,.43f,.3f):new Color(.28f,.39f,.37f);
                col*=Random.Range(.93f,1.06f);col.a=1;
                mesh.Tri(a,c,b,col);mesh.Tri(b,c,d,col);
            }
            mesh.Build("Grass, paths and tidal sand",Scenery,true);
            // Rolling foam ribbons follow the actual perimeter, leaving the pier water open.
            for(int k=0;k<3;k++) {
                var foam=new GameObject("Shore foam").AddComponent<LineRenderer>();foam.transform.SetParent(transform);
                foam.loop=true;foam.positionCount=181;foam.startWidth=foam.endWidth=.13f+k*.035f;foam.material=Shape.Mat(new Color(.69f,.85f,.8f));
                for(int i=0;i<=180;i++){float a=i*Mathf.PI*2/180;float d=.935f+Mathf.Sin(a*(5+Region))*(.035f+Region*.01f)+k*.009f;foam.SetPosition(i,new Vector3(Mathf.Cos(a)*CoastWidth*d,-.38f,Mathf.Sin(a)*CoastLength*d-15));}
            }
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
            Shack(new Vector3(-10,1.55f,-9),-8,false);
            Shack(new Vector3(10,1.55f,-10),8,true);
            Sign(Scenery,new Vector3(-10,4.85f,-6.6f),"鱼 获 收 购","FISH MARKET  /  E",-8,4.9f);
            Sign(Scenery,new Vector3(10,4.85f,-7.6f),"老 船 长 工 坊","GEAR & GUNS  /  E",8,5.6f);
            NPC(new Vector3(-10,1.65f,-8.15f),false);NPC(new Vector3(10,1.65f,-9.15f),true);
            // Fish scale, open crate and coin jar establish the sell counter's purpose.
            Box("Fish counter",Scenery,new Vector3(-10,2.2f,-6.4f),new Vector3(4.5f,1.2f,1.1f),wood,true);
            Box("Market ice",Scenery,new Vector3(-10.8f,2.84f,-6.4f),new Vector3(2,.08f,.7f),new Color(.65f,.85f,.82f));
            var display=CreatureArt.Build(new GameObject("Display fish").transform,CreatureKind.Snapper,false);display.parent.SetParent(Scenery);display.parent.position=new Vector3(-10.9f,3,-6.4f);display.parent.localScale=Vector3.one*.38f;display.parent.rotation=Quaternion.Euler(0,90,85);foreach(var co in display.GetComponentsInChildren<Collider>())Destroy(co);
            Box("Workshop counter",Scenery,new Vector3(10,2.2f,-7.4f),new Vector3(4.7f,1.2f,1.1f),wood,true);
            Shape.Part("Anvil",PrimitiveType.Cube,Scenery,new Vector3(9.6f,3,-7.4f),new Vector3(.8f,.22f,.35f),iron);
            Shape.Beam(Scenery,new Vector3(10.7f,2.85f,-7.5f),new Vector3(11.3f,2.87f,-7.3f),.075f,wood);
            Box("Hammer",Scenery,new Vector3(11.3f,2.88f,-7.3f),new Vector3(.17f,.13f,.32f),iron);
            for(int i=0;i<3;i++)Crate(new Vector3(6.5f,1.5f,-11+i),.8f);
            Barrel(new Vector3(-13.8f,1.6f,-6.4f));Barrel(new Vector3(13.7f,1.6f,-6.8f));
            Sign(Scenery,new Vector3(0,2.7f,-17),"远 征 航 图","CHART A COURSE  /  E",0,4.2f);
            Box("Chart support",Scenery,new Vector3(0,1.9f,-17),new Vector3(.22f,1.2f,.22f),wood,true);
            for(int i=0;i<6;i++) {
                float a=i*Mathf.PI/3;Shape.Rock(Scenery,new Vector3(Mathf.Cos(a)*.85f,1.66f,-21+Mathf.Sin(a)*.85f),Vector3.one*.35f,new Color(.35f,.38f,.33f));
            }
            Shape.Beam(Scenery,new Vector3(-.6f,1.7f,-21),new Vector3(.6f,1.7f,-21),.16f,wood);
            Shape.Beam(Scenery,new Vector3(0,1.75f,-21.6f),new Vector3(0,1.75f,-20.4f),.16f,wood);
            var fire=Shape.Part("Campfire",PrimitiveType.Sphere,Scenery,new Vector3(0,2,-21),new Vector3(.5f,.7f,.5f),new Color(1,.39f,.06f),false,true);var light=fire.AddComponent<Light>();light.color=new Color(1,.47f,.16f);light.range=9;light.intensity=2;
            // String lights frame the village without blocking the walking path.
            for(int s=-1;s<=1;s+=2)Shape.Beam(Scenery,new Vector3(s*15,1.5f,-4),new Vector3(s*15,6,-4),.13f,wood);
            for(int i=0;i<16;i++) {
                float x=-15+i*2;float y=5.7f-Mathf.Sin((x+15)/30*Mathf.PI)*1.3f;
                if(i<15){float nx=x+2,ny=5.7f-Mathf.Sin((nx+15)/30*Mathf.PI)*1.3f;Shape.Beam(Scenery,new Vector3(x,y,-4),new Vector3(nx,ny,-4),.025f,iron);}
                Shape.Part("Warm bulb",PrimitiveType.Sphere,Scenery,new Vector3(x,y-.12f,-4),Vector3.one*.13f,new Color(1,.77f,.4f),false,true);
            }
        }
        void Shack(Vector3 position,float angle,bool workshop)
        {
            var r=new GameObject(workshop?"Captain's workshop":"Fish market").transform;r.SetParent(Scenery);r.position=position;r.rotation=Quaternion.Euler(0,angle,0);
            Color wall=workshop?new Color(.24f,.39f,.41f):new Color(.61f,.42f,.24f);
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
            for(int i=0;i<9000;i++) {
                float x=Random.Range(-26f,26f),z=Random.Range(-46f,14f),y=Height(x,z);
                if(y<1.25f||Mathf.Abs(x)<3.5f||z>-13&&z<-3&&Mathf.Abs(x)<16)continue;
                if(Vector2.Distance(new Vector2(x,z),new Vector2(-10,-9))<5||Vector2.Distance(new Vector2(x,z),new Vector2(10,-10))<5)continue;
                float h=Random.Range(.15f,.48f),w=Random.Range(.06f,.12f);var p=new Vector3(x,y-.035f,z);Color col=Color.Lerp(new Color(.31f,.43f,.16f),new Color(.65f,.66f,.29f),Random.value);
                grass.Tri(p+Vector3.left*w,p+Vector3.up*h+Vector3.right*.09f,p+Vector3.right*w,col);grass.Tri(p+Vector3.forward*w,p+Vector3.up*h+Vector3.back*.07f,p+Vector3.back*w,col);
            }
            grass.Build("Meadow grasses",Scenery);
            for(int i=0;i<70;i++) {
                float x=Random.Range(-25f,25f),z=Random.Range(-45f,10f),y=Height(x,z);
                if(y<1.2f||Mathf.Abs(x)<4||z>-16&&z<-1&&Mathf.Abs(x)<18)continue;
                Pine(Scenery,new Vector3(x,y,z),Random.Range(3.8f,7.8f));
            }
            for(int i=0;i<35;i++) {
                float x=Random.Range(-27f,27f),z=Random.Range(-45f,15f),y=Height(x,z);
                if(y<0||Mathf.Abs(x)<4||z>-16&&z<-2&&Mathf.Abs(x)<18)continue;
                float scale=Random.Range(.45f,1.6f);var r=Shape.Rock(Scenery,new Vector3(x,y-.2f,z),new Vector3(scale,scale*.7f,scale),new Color(.43f,.48f,.43f));r.AddComponent<MeshCollider>();r.layer=8;
            }
            for(int i=0;i<10;i++){float x=Random.Range(-21f,21f),z=Random.Range(-42f,-25f);Crate(new Vector3(x,Height(x,z),z),.45f);}
        }
        void Pine(Transform parent,Vector3 pos,float height)
        {
            var m=new CoastalMesh();m.Cone(pos,.19f,height,new Color(.31f,.24f,.15f),7,.06f);
            Color low=Region==0?new Color(.17f,.32f,.22f):Region==1?new Color(.2f,.3f,.27f):new Color(.22f,.28f,.35f);
            Color high=Region==0?new Color(.32f,.47f,.26f):Region==1?new Color(.38f,.46f,.35f):new Color(.34f,.48f,.47f);
            for(int i=0;i<5;i++)m.Cone(pos+Vector3.up*(height*.24f+i*height*.13f),height*(.25f-i*.034f),height*.38f,Color.Lerp(low,high,i*.2f),9);
            var g=m.Build("Coastal pine",parent);if(parent==Scenery){var col=g.AddComponent<CapsuleCollider>();col.center=pos+Vector3.up*height*.4f;col.radius=.23f;col.height=height*.8f;g.layer=8;}
        }
        void BuildDistantIslands()
        {
            Vector3[] positions={new Vector3(-62,-1,72),new Vector3(74,-1,101),new Vector3(-115,-1,-25),new Vector3(95,-1,-98)};
            for(int j=0;j<positions.Length;j++) {
                var r=new GameObject("Outer island").transform;r.SetParent(Scenery);r.position=positions[j];
                Shape.Rock(r,Vector3.zero,new Vector3(23,12,18),new Color(.34f,.43f,.4f),11);
                var cap=Shape.Rock(r,Vector3.up*7,new Vector3(17,4,14),new Color(.28f,.4f,.23f),11);var surface=cap.AddComponent<MeshCollider>();Physics.SyncTransforms();
                for(int i=0;i<12;i++){var p=new Vector3(Random.Range(-10,10),9,Random.Range(-8,8));RaycastHit hit;if(surface.Raycast(new Ray(r.TransformPoint(new Vector3(p.x,25,p.z)),Vector3.down),out hit,40))p.y=r.InverseTransformPoint(hit.point).y-.12f;Pine(r,p,Random.Range(5,10));}Destroy(surface);
                if(j==0){for(int i=0;i<6;i++)Shape.Part("Lighthouse tier",PrimitiveType.Cylinder,r,new Vector3(0,11+i*1.7f,0),new Vector3(2.8f-i*.14f,.85f,2.8f-i*.14f),i%2==0?ivory:orange);Shape.Part("Lantern room",PrimitiveType.Cylinder,r,new Vector3(0,20.8f,0),new Vector3(2.1f,.7f,2.1f),new Color(1,.83f,.44f),false,true);Shape.Rock(r,new Vector3(0,21.5f,0),new Vector3(1.6f,1.2f,1.6f),iron,8);beacon=new GameObject("Beacon").transform;beacon.SetParent(r,false);beacon.localPosition=new Vector3(0,21,0);var l=beacon.gameObject.AddComponent<Light>();l.type=LightType.Spot;l.range=190;l.spotAngle=18;l.color=new Color(1,.8f,.5f);l.intensity=5;}
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
        void BuildBiomes()
        {
            for(int act=0;act<3;act++){biomeDetails[act]=new GameObject("Sea region details "+act).transform;biomeDetails[act].SetParent(transform);}
            // A storm-wrecked hull and old navigation stones mark the second region.
            var wreck=new GameObject("Wreck of the Northstar").transform;wreck.SetParent(biomeDetails[1]);wreck.position=new Vector3(-19,Height(-19,3),3);wreck.rotation=Quaternion.Euler(0,32,18);
            for(int i=0;i<9;i++){float z=-4+i;Shape.Beam(wreck,new Vector3(-2,.4f,z),new Vector3(-1,-.5f,z),.18f,wood*.6f);Shape.Beam(wreck,new Vector3(1,-.5f,z),new Vector3(2,.4f,z),.18f,wood*.6f);}
            for(int s=-1;s<=1;s+=2)for(int i=0;i<3;i++)Shape.Beam(wreck,new Vector3(s*(1.1f+i*.3f),-.45f+i*.3f,-4),new Vector3(s*(1.1f+i*.3f),-.45f+i*.3f,Random.Range(1.2f,4)),.19f,wood*.7f);
            Shape.Beam(wreck,Vector3.zero,new Vector3(-2.4f,4.8f,1),.16f,wood);
            for(int s=-1;s<=1;s+=2){float x=s*19,z=-16;float y=Height(x,z);Shape.Rock(biomeDetails[1],new Vector3(x,y,z),new Vector3(1.2f,4.5f,1.1f),new Color(.28f,.36f,.37f));}
            // Deepwater plants, luminous crystals and an old gate provide a distinct final shoreline.
            for(int i=0;i<27;i++) {
                float x=(i%2==0?-1:1)*Random.Range(8f,23f),z=Random.Range(-30f,10f),y=Height(x,z);if(y<.6f||z>-15&&z<-3&&Mathf.Abs(x)<16)continue;
                var p=new Vector3(x,y,z);var col=new Color(.2f,.6f,.64f);
                for(int j=0;j<3;j++){var r=Shape.Rock(biomeDetails[2],p+new Vector3((j-1)*.35f,0,0),new Vector3(.3f,Random.Range(.6f,1.6f),.3f),col,5);r.transform.localRotation=Quaternion.Euler(0,j*45,(j-1)*20);}
                Shape.Part("Bioluminescent bloom",PrimitiveType.Sphere,biomeDetails[2],p+Vector3.up*.85f,new Vector3(.25f,.18f,.25f),new Color(.29f,.86f,.7f),false,true);
            }
            for(int s=-1;s<=1;s+=2){float x=s*6.5f,z=-27;float y=Height(x,z);Shape.Rock(biomeDetails[2],new Vector3(x,y,z),new Vector3(.9f,5.8f,1.2f),new Color(.23f,.3f,.34f),6);}
            Shape.Beam(biomeDetails[2],new Vector3(-6.5f,7.1f,-27),new Vector3(6.5f,7.1f,-27),.7f,new Color(.25f,.33f,.35f));
            for(int i=0;i<7;i++)Shape.Part("Gate rune",PrimitiveType.Cube,biomeDetails[2],new Vector3((i-3)*1.3f,7.15f,-26.7f),new Vector3(.12f,.4f,.06f),new Color(.3f,.85f,.7f),false,true);
            for(int act=0;act<3;act++)StaticBatchingUtility.Combine(biomeDetails[act].gameObject);
        }
        public void SetAct(int act)
        {
            for(int i=0;i<biomeDetails.Length;i++)if(biomeDetails[i])biomeDetails[i].gameObject.SetActive(i==act);
            Ocean.SetColor("_DeepColor",act==0?new Color(.06f,.3f,.37f):act==1?new Color(.065f,.21f,.3f):new Color(.055f,.12f,.23f));
            Ocean.SetColor("_CrestColor",act==0?new Color(.38f,.69f,.68f):new Color(.28f,.52f,.6f));
            RenderSettings.fogColor=act==0?new Color(.69f,.79f,.8f):act==1?new Color(.49f,.61f,.7f):new Color(.25f,.33f,.46f);
            sun.intensity=act==0?1.2f:act==1?.95f:.7f;sun.color=act==0?new Color(1,.92f,.77f):new Color(.77f,.87f,1);
            RenderSettings.skybox.SetColor("_SkyTint",act==0?new Color(.28f,.5f,.7f):act==1?new Color(.25f,.36f,.47f):new Color(.12f,.19f,.32f));
            RenderSettings.skybox.SetColor("_Horizon",RenderSettings.fogColor);RenderSettings.skybox.SetColor("_Cloud",act==0?new Color(.96f,.94f,.87f):new Color(.53f,.61f,.69f));
            RenderSettings.skybox.SetFloat("_Exposure",act==0?1.2f:act==1?1:.85f);
            RenderSettings.ambientSkyColor=act==0?new Color(.66f,.77f,.8f):new Color(.4f,.53f,.66f);
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

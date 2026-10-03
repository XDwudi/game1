using System.Collections.Generic;
using UnityEngine;

namespace Tidebreak
{
    public static class Shape
    {
        static Dictionary<string,Material> materials = new Dictionary<string,Material>();
        public static Material Mat(Color c, bool glow=false)
        {
            string key=c.ToString()+glow;
            Material m;
            if(materials.TryGetValue(key,out m) && m) return m;
            m=new Material(Shader.Find("Standard")); m.color=c;
            m.SetFloat("_Glossiness",.22f);
            if(glow) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c*1.8f); }
            materials[key]=m; return m;
        }
        public static GameObject Part(string name, PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale, Color color, bool collider=false, bool glow=false)
        {
            var g=GameObject.CreatePrimitive(type); g.name=name; g.transform.SetParent(parent,false);
            g.transform.localPosition=pos; g.transform.localScale=scale;
            g.GetComponent<Renderer>().sharedMaterial=Mat(color,glow);
            if(!collider) Object.Destroy(g.GetComponent<Collider>());
            return g;
        }
        public static GameObject MeshObject(string name, Transform parent, Vector3[] vertices, int[] triangles, Color color)
        {
            var g=new GameObject(name); g.transform.SetParent(parent,false);
            var mesh=new Mesh { name=name };
            // Split faces to preserve the deliberately faceted art style.
            var v=new Vector3[triangles.Length]; var t=new int[v.Length];
            for(int i=0;i<v.Length;i++) { v[i]=vertices[triangles[i]]; t[i]=i; }
            mesh.vertices=v; mesh.triangles=t; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            g.AddComponent<MeshFilter>().sharedMesh=mesh;
            g.AddComponent<MeshRenderer>().sharedMaterial=Mat(color);
            return g;
        }
        public static GameObject Rock(Transform parent, Vector3 pos, Vector3 scale, Color color, int sides=7)
        {
            var vs=new Vector3[sides*2+2]; vs[vs.Length-2]=Vector3.up; vs[vs.Length-1]=Vector3.down*.45f;
            for(int i=0;i<sides;i++) {
                float a=i*Mathf.PI*2/sides;
                vs[i]=new Vector3(Mathf.Cos(a),.1f,Mathf.Sin(a));
                vs[i+sides]=new Vector3(Mathf.Cos(a+.12f)*.6f,.75f+Mathf.Sin(i*7)*.17f,Mathf.Sin(a+.12f)*.6f);
            }
            var tris=new List<int>();
            for(int i=0;i<sides;i++) { int j=(i+1)%sides;
                tris.AddRange(new[]{i,i+sides,j+sides,i,j+sides,j,i+sides,vs.Length-2,j+sides,j,vs.Length-1,i});
            }
            var g=MeshObject("Basalt",parent,vs,tris.ToArray(),color); g.transform.localPosition=pos; g.transform.localScale=scale; return g;
        }
        public static GameObject Beam(Transform parent, Vector3 a, Vector3 b, float radius, Color color, bool glow=false)
        {
            var g=Part("Beam",PrimitiveType.Cylinder,parent,(a+b)*.5f,new Vector3(radius,(b-a).magnitude*.5f,radius),color,false,glow);
            g.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a); return g;
        }
    }

    public class SeaWorld : MonoBehaviour
    {
        public Transform Boat, Scenery;
        public Material Ocean;
        Light sun;
        Transform beacon;
        readonly List<Transform> birds = new List<Transform>();
        readonly Color wood=new Color(.36f,.21f,.13f), darkwood=new Color(.13f,.21f,.24f);
        public void Build()
        {
            Random.InitState(2718);
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.6f,.76f,.83f);
            RenderSettings.ambientEquatorColor=new Color(.36f,.58f,.62f);
            RenderSettings.ambientGroundColor=new Color(.2f,.3f,.33f);
            RenderSettings.fog=true; RenderSettings.fogMode=FogMode.ExponentialSquared; RenderSettings.fogDensity=.005f;
            var light=new GameObject("Late afternoon sun"); light.transform.SetParent(transform); sun=light.AddComponent<Light>();
            sun.type=LightType.Directional; sun.color=new Color(1,.87f,.67f); sun.intensity=1.25f;
            sun.shadows=LightShadows.Soft; sun.shadowStrength=.65f; light.transform.rotation=Quaternion.Euler(34,-32,0);
            RenderSettings.sun=sun;
            var sky=new Material(Shader.Find("Skybox/Procedural")); sky.SetFloat("_SunSize",.065f); sky.SetFloat("_AtmosphereThickness",.9f);
            sky.SetColor("_SkyTint",new Color(.4f,.59f,.64f)); sky.SetColor("_GroundColor",new Color(.12f,.36f,.4f)); RenderSettings.skybox=sky;
            var water=GameObject.CreatePrimitive(PrimitiveType.Plane); water.name="Living ocean"; water.transform.SetParent(transform);
            water.transform.position=new Vector3(0,-.6f,0); water.transform.localScale=Vector3.one*100;
            Object.Destroy(water.GetComponent<Collider>());
            Ocean=new Material(Resources.Load<Shader>("Ocean")); water.GetComponent<Renderer>().sharedMaterial=Ocean;
            Boat=new GameObject("The Wayfarer").transform; Boat.SetParent(transform);
            BuildBoat();
            Scenery=new GameObject("Archipelago").transform; Scenery.SetParent(transform);
            Island(new Vector3(-42,-.7f,70),new Vector3(17,14,19),true);
            Island(new Vector3(65,-.7f,100),new Vector3(25,22,23),false);
            Island(new Vector3(-85,-.7f,-28),new Vector3(28,18,25),false);
            Island(new Vector3(90,-.7f,-100),new Vector3(34,20,29),false);
            for(int i=0;i<22;i++) {
                float a=i*Mathf.PI*2/22; float r=Random.Range(95f,180f);
                Shape.Rock(Scenery,new Vector3(Mathf.Cos(a)*r,-1,Mathf.Sin(a)*r),new Vector3(Random.Range(5,18),Random.Range(3,15),Random.Range(5,18)),new Color(.2f,.35f,.38f));
            }
            for(int i=0;i<7;i++) {
                var bird=new GameObject("Sea bird").transform; bird.SetParent(transform);
                Shape.Beam(bird,new Vector3(-.8f,0,0),new Vector3(0,.18f,.1f),.07f,Color.white);
                Shape.Beam(bird,new Vector3(.8f,0,0),new Vector3(0,.18f,.1f),.07f,Color.white);
                birds.Add(bird);
            }
            SetAct(0);
        }
        void BuildBoat()
        {
            Shape.MeshObject("Painted hull",Boat,new[]{new Vector3(-7,-.3f,-7),new Vector3(7,-.3f,-7),new Vector3(7,-.3f,6),new Vector3(0,-.3f,10),new Vector3(-7,-.3f,6),new Vector3(-5,-2,-6),new Vector3(5,-2,-6),new Vector3(5,-2,5),new Vector3(0,-2,8),new Vector3(-5,-2,5)},new[]{0,1,6,0,6,5,1,2,7,1,7,6,2,3,8,2,8,7,3,4,9,3,9,8,4,0,5,4,5,9},darkwood);
            for(int i=0;i<36;i++) {
                float z=-6.6f+i*.46f; float width=z>5.8f?Mathf.Lerp(14,1,(z-5.8f)/3.8f):14;
                Shape.Part("Teak deck",PrimitiveType.Cube,Boat,new Vector3(0,.01f,z),new Vector3(width,.3f,.435f),Color.Lerp(wood,new Color(.65f,.42f,.23f),(i%3)*.2f));
            }
            var gold=new Color(.87f,.63f,.3f);
            for(int s=-1;s<=1;s+=2) {
                Shape.Beam(Boat,new Vector3(s*6.8f,.45f,-6.8f),new Vector3(s*6.8f,.45f,6),.14f,darkwood);
                Shape.Beam(Boat,new Vector3(s*6.8f,.45f,6),new Vector3(0,.45f,9.6f),.14f,darkwood);
                for(int z=-6;z<=6;z+=3) {
                    Shape.Part("Brass cleat",PrimitiveType.Cube,Boat,new Vector3(s*6.5f,.35f,z),new Vector3(.18f,.5f,.6f),gold);
                }
            }
            Shape.Part("Cabin",PrimitiveType.Cube,Boat,new Vector3(0,1.4f,-5.8f),new Vector3(4.8f,2.8f,2.2f),new Color(.77f,.79f,.66f));
            Shape.Part("Cabin roof",PrimitiveType.Cube,Boat,new Vector3(0,3,-5.8f),new Vector3(5.2f,.24f,2.65f),darkwood);
            Shape.Part("Front window",PrimitiveType.Cube,Boat,new Vector3(0,1.8f,-4.65f),new Vector3(3.6f,1.2f,.05f),new Color(.1f,.34f,.4f));
            Shape.Part("Cabin trim",PrimitiveType.Cube,Boat,new Vector3(0,1.8f,-4.59f),new Vector3(.13f,1.3f,.09f),gold);
            Shape.Beam(Boat,new Vector3(-4,0,-4.8f),new Vector3(-4,7,-4.8f),.13f,wood);
            Shape.MeshObject("Wayfarer pennant",Boat,new[]{new Vector3(-4,6.9f,-4.8f),new Vector3(-4,5.8f,-4.8f),new Vector3(-1.5f,6.4f,-4.8f)},new[]{0,1,2,2,1,0},new Color(.96f,.55f,.23f));
            for(int i=0;i<3;i++) {
                var barrel=Shape.Part("Barrel",PrimitiveType.Cylinder,Boat,new Vector3(5.5f,.65f,-4+i*1.3f),new Vector3(.95f,.65f,.95f),wood);
                Shape.Part("Barrel band",PrimitiveType.Cylinder,barrel.transform,new Vector3(0,.55f,0),new Vector3(1.03f,.08f,1.03f),darkwood);
                Shape.Part("Barrel band",PrimitiveType.Cylinder,barrel.transform,new Vector3(0,-.55f,0),new Vector3(1.03f,.08f,1.03f),darkwood);
            }
            for(int i=0;i<2;i++) {
                var lamp=Shape.Part("Deck lantern",PrimitiveType.Cube,Boat,new Vector3(i==0?-6:6,1.2f,5.5f),new Vector3(.3f,.55f,.3f),new Color(1,.7f,.3f),false,true);
                var l=lamp.AddComponent<Light>(); l.color=new Color(1,.61f,.26f); l.range=6; l.intensity=1.2f;
                Shape.Beam(Boat,new Vector3(i==0?-6:6,.1f,5.5f),lamp.transform.position,.08f,darkwood);
            }
        }
        void Island(Vector3 pos, Vector3 size, bool lighthouse)
        {
            var root=new GameObject("Island").transform; root.SetParent(Scenery); root.position=pos;
            Shape.Rock(root,Vector3.zero,size,new Color(.3f,.45f,.44f));
            var grass=Shape.Rock(root,new Vector3(0,size.y*.5f,0),new Vector3(size.x*.83f,size.y*.45f,size.z*.8f),new Color(.37f,.59f,.4f));
            var ground=grass.AddComponent<MeshCollider>();ground.sharedMesh=grass.GetComponent<MeshFilter>().sharedMesh;Physics.SyncTransforms();
            for(int i=0;i<12;i++) {
                var p=new Vector3(Random.Range(-.5f,.5f)*size.x,size.y*.85f,Random.Range(-.5f,.5f)*size.z);
                RaycastHit hit;
                if(ground.Raycast(new Ray(root.TransformPoint(new Vector3(p.x,size.y*2,p.z)),Vector3.down),out hit,size.y*3))p.y=root.InverseTransformPoint(hit.point).y-.1f;
                Shape.Beam(root,p,p+Vector3.up*3,.2f,wood);
                Shape.Rock(root,p+Vector3.up*3,new Vector3(2.4f,1.6f,2.4f),new Color(.2f,.44f,.32f),5);
            }
            if(!lighthouse)return;
            var tower=new GameObject("Lighthouse").transform; tower.SetParent(root,false); tower.localPosition=new Vector3(0,size.y,0);
            for(int i=0;i<5;i++) Shape.Part("Tower tier",PrimitiveType.Cylinder,tower,Vector3.up*i*2,new Vector3(3.3f-i*.18f,1,3.3f-i*.18f),i%2==0?new Color(.91f,.84f,.66f):new Color(.67f,.27f,.2f));
            Shape.Part("Lantern room",PrimitiveType.Cylinder,tower,Vector3.up*9.7f,new Vector3(2.7f,.8f,2.7f),new Color(1,.82f,.4f),false,true);
            Shape.Rock(tower,Vector3.up*10.6f,new Vector3(2.3f,1.3f,2.3f),darkwood,8);
            beacon=new GameObject("Rotating beacon").transform; beacon.SetParent(tower,false); beacon.localPosition=Vector3.up*9.8f;
            var spot=beacon.gameObject.AddComponent<Light>(); spot.type=LightType.Spot; spot.range=160; spot.spotAngle=25; spot.intensity=8; spot.color=new Color(1,.86f,.54f);
        }
        public void SetAct(int act)
        {
            var water=new[]{new Color(.025f,.43f,.48f),new Color(.06f,.26f,.38f),new Color(.07f,.15f,.3f)};
            Ocean.SetColor("_DeepColor",water[act]);
            Ocean.SetColor("_CrestColor",act==2?new Color(.27f,.6f,.66f):new Color(.4f,.78f,.72f));
            RenderSettings.fogColor=act==0?new Color(.56f,.74f,.75f):act==1?new Color(.38f,.52f,.6f):new Color(.2f,.28f,.44f);
            sun.intensity=act==0?1.25f:act==1?.9f:.6f;
            sun.color=act==0?new Color(1,.87f,.67f):act==1?new Color(.72f,.85f,1):new Color(.55f,.73f,1);
            RenderSettings.skybox.SetColor("_SkyTint",act==0?new Color(.4f,.59f,.64f):act==1?new Color(.3f,.36f,.42f):new Color(.12f,.16f,.3f));
            RenderSettings.skybox.SetFloat("_Exposure",act==0?1.1f:act==1?.7f:.38f);
            RenderSettings.skybox.SetFloat("_AtmosphereThickness",act==0?.9f:1.4f);
            RenderSettings.ambientSkyColor=act==0?new Color(.6f,.76f,.83f):act==1?new Color(.4f,.53f,.65f):new Color(.3f,.4f,.62f);
            if(Scenery)Scenery.localRotation=Quaternion.Euler(0,act*112,0);
        }
        void Update()
        {
            if(beacon) beacon.localRotation=Quaternion.Euler(0,Time.time*16,0);
            for(int i=0;i<birds.Count;i++) {
                float a=Time.time*.045f+i*1.4f;
                birds[i].position=new Vector3(Mathf.Cos(a)*(40+i*6),22+Mathf.Sin(a*2+i)*4,Mathf.Sin(a)*(40+i*6)+35);
                birds[i].rotation=Quaternion.Euler(Mathf.Sin(Time.time*3+i)*12,-a*Mathf.Rad2Deg,0);
            }
        }
    }
}

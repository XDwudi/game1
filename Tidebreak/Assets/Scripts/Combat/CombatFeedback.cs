using UnityEngine;

namespace Tidebreak
{
    // Three reusable emitters replace hundreds of short-lived physics/render objects.
    public sealed class CombatFeedback : MonoBehaviour
    {
        ParticleSystem sparks,mist,flash;
        Material material;Texture2D sprite;GameObject root;
        public int Emitted {get;private set;}
        void Awake()
        {
            root=new GameObject("Combat feedback pool");root.transform.SetParent(transform,false);
            sprite=new Texture2D(32,32,TextureFormat.RGBA32,false);sprite.name="Soft impact pigment";
            var pixels=new Color[1024];for(int y=0;y<32;y++)for(int x=0;x<32;x++){
                float d=new Vector2((x-15.5f)/15.5f,(y-15.5f)/15.5f).magnitude;
                pixels[y*32+x]=new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-d),1.6f));
            }
            sprite.SetPixels(pixels);sprite.Apply(false,true);
            material=new Material(Resources.Load<Shader>("CombatParticle"));material.mainTexture=sprite;
            sparks=Emitter("Directional sparks",384,true,.85f);
            mist=Emitter("Salt mist",256,false,.35f);
            flash=Emitter("Hot impact core",96,false,0);
        }
        ParticleSystem Emitter(string name,int limit,bool streak,float gravity)
        {
            var obj=new GameObject(name);obj.transform.SetParent(root.transform,false);
            var ps=obj.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var m=ps.main;m.loop=true;m.playOnAwake=false;m.duration=10;m.maxParticles=limit;m.simulationSpace=ParticleSystemSimulationSpace.World;
            m.startSpeed=0;m.startLifetime=.5f;m.startSize=.1f;m.gravityModifier=gravity;
            var e=ps.emission;e.enabled=false;var shape=ps.shape;shape.enabled=false;
            var color=ps.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(.8f,.3f),new GradientAlphaKey(0,1)});color.color=gradient;
            var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,1,1,.1f));
            var r=ps.GetComponent<ParticleSystemRenderer>();r.sharedMaterial=material;r.renderMode=streak?ParticleSystemRenderMode.Stretch:ParticleSystemRenderMode.Billboard;r.lengthScale=streak?1.6f:1;r.velocityScale=streak?.045f:0;
            r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;
            ps.Play();return ps;
        }
        void Emit(ParticleSystem system,Vector3 p,Vector3 v,Color color,float size,float lifetime)
        {system.Emit(new ParticleSystem.EmitParams{position=p,velocity=v,startColor=color,startSize=size,startLifetime=lifetime,rotation=Random.Range(0,360)},1);Emitted++;}
        public void Burst(Vector3 point,Color color,int count,float size)
        {
            if(!sparks)return;count=Mathf.Clamp(count,1,28);
            for(int i=0;i<count;i++)Emit(sparks,point,Random.insideUnitSphere*2.4f+Vector3.up*1.2f,Color.Lerp(color,Color.white,.32f),size*1.5f,Random.Range(.18f,.48f));
            for(int i=0;i<Mathf.Min(5,count/3+1);i++)Emit(mist,point,Random.insideUnitSphere*.7f+Vector3.up*.5f,new Color(color.r,color.g,color.b,.58f),size*5,Random.Range(.25f,.6f));
        }
        public void Impact(Vector3 point,Vector3 normal,bool flesh,bool weak,bool killed)
        {
            Color hot=weak?new Color(1,.77f,.26f):flesh?new Color(.52f,1,.89f):new Color(1,.69f,.35f);
            Emit(flash,point+normal*.025f,normal*.5f,hot,weak?.42f:.24f,.075f);
            int count=killed?18:weak?10:6;
            for(int i=0;i<count;i++)Emit(sparks,point,(normal+Random.insideUnitSphere*.85f)*(weak?4:2.8f),hot,Random.Range(.035f,.08f),Random.Range(.14f,.36f));
            Emit(mist,point,normal*.8f,new Color(hot.r,hot.g,hot.b,.5f),.32f,.35f);
        }
        public void Muzzle(Vector3 point,Vector3 direction,WeaponKind weapon)
        {
            bool arc=weapon==WeaponKind.ArcCaster;Color c=arc?new Color(.4f,1,1):new Color(1,.8f,.4f);
            Emit(flash,point,direction*2,c,weapon==WeaponKind.Scattergun?.36f:.21f,.045f);
            for(int i=0;i<(weapon==WeaponKind.Scattergun?7:3);i++)Emit(sparks,point,(direction+Random.insideUnitSphere*.2f)*7,c,.07f,.055f);
            Emit(mist,point,direction*.9f+Vector3.up*.2f,new Color(.64f,.7f,.7f,.26f),.18f,.25f);
        }
        public void Clear(){if(sparks)sparks.Clear();if(mist)mist.Clear();if(flash)flash.Clear();}
        void OnDestroy(){if(material)Destroy(material);if(sprite)Destroy(sprite);}
    }
}

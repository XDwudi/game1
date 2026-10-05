using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tidebreak
{
    // Decorations are bounded and disposable independently of the authoritative hazard.
    // No gameplay RNG, collisions, damage or timers are changed by this renderer.
    public sealed class EnemySkillFX : MonoBehaviour
    {
        const int AccentLimit=128,PathLimit=40,WaveLimit=20,WaveSegments=64;
        sealed class Accent
        {
            public Transform transform;public MeshFilter filter;public MeshRenderer renderer;
            public Vector3 start,velocity,scale;public Quaternion rotation;public Color color;
            public float age,life,spin,growth;
        }
        sealed class Path
        {
            public LineRenderer line;public Color color;public float age,life,width;
        }
        sealed class Wave
        {
            public Mesh mesh;public MeshRenderer renderer;public Vector3[] vertices;
            public Color[] colors;public float age,life;public Color color;
        }
        GameDirector game;GameObject pool;Material meshMaterial,particleMaterial,smokeMaterial,pathMaterial;
        Texture2D glintTexture,smokeTexture;MaterialPropertyBlock tint;
        ParticleSystem spray,glints,haze;
        Mesh crest,crystal,root,coral,flame,sail,mirror,tentacle,rib;
        readonly List<Mesh> meshes=new List<Mesh>();
        readonly Accent[] accents=new Accent[AccentLimit];readonly Path[] paths=new Path[PathLimit];readonly Wave[] waves=new Wave[WaveLimit];
        int accentCursor,pathCursor,waveCursor;bool paused,cleared;float nextReleaseSound;
        static readonly int Tint=Shader.PropertyToID("_Tint");
        public int Emitted {get;private set;}
        public int PlayedReleaseSounds {get;private set;}
        public int ActiveAccents {get{int n=0;foreach(var a in accents)if(a!=null&&a.renderer.enabled)n++;return n;}}
        public int ActivePaths {get{int n=0;foreach(var p in paths)if(p!=null&&p.line.enabled)n++;return n;}}
        public int ActiveWaves {get{int n=0;foreach(var w in waves)if(w!=null&&w.renderer.enabled)n++;return n;}}
        public int ParticleCount {get{return spray?spray.particleCount+glints.particleCount+haze.particleCount:0;}}
        public int ResourceMeshes {get{return meshes.Count;}}
        public static EnemySkillFX For(GameDirector director)
        {
            var fx=director.GetComponent<EnemySkillFX>();
            if(!fx){fx=director.gameObject.AddComponent<EnemySkillFX>();fx.game=director;fx.Initialize();}
            return fx;
        }
        public static void ClearFor(GameDirector director){var fx=director.GetComponent<EnemySkillFX>();if(fx)fx.Clear();}
        public static void Warn(Enemy owner,Vector3 point,float radius,float delay,float damage,SkillTheme? theme=null)
        {if(owner)owner.game.Warn(point,radius,delay,damage,owner,theme);}
        public static void Projectile(Enemy owner,Vector3 from,Vector3 to,float speed,float damage,SkillTheme? theme=null)
        {if(owner)owner.game.Projectile(from,to,speed,damage,SkillThemes.ColorOf(SkillThemes.Resolve(owner,theme)),owner,theme);}
        public static void Burst(Enemy owner,Vector3 point,float radius=1)
        {if(owner)Burst(owner.game,SkillThemes.ForSpecies(owner.Spec),point,radius,owner.Spec.id);}
        public static void Burst(GameDirector game,SkillTheme theme,Vector3 point,float radius=1,int variant=0)
        {For(game).Contact(theme,point,radius,variant);}

        void Initialize()
        {
            pool=new GameObject("Bounded enemy skill effect pool");pool.transform.SetParent(transform,false);
            tint=new MaterialPropertyBlock();
            glintTexture=Sprite(false);smokeTexture=Sprite(true);
            meshMaterial=new Material(Resources.Load<Shader>("CombatGeometry")){name="Enemy skill translucent sculpture"};
            particleMaterial=new Material(Resources.Load<Shader>("CombatParticle")){name="Enemy salt sparks",mainTexture=glintTexture};
            smokeMaterial=new Material(Resources.Load<Shader>("CombatParticle")){name="Enemy broken ink and smoke",mainTexture=smokeTexture};
            pathMaterial=new Material(Resources.Load<Shader>("CombatParticle")){name="Lightning solid luminous core",mainTexture=Texture2D.whiteTexture};
            crest=WaveCrest();flame=FlameTongues();
            crystal=Crystal();root=Branch(false);coral=Branch(true);sail=Sail();mirror=Mirror();tentacle=Tentacle();rib=Rib();
            spray=Emitter("Foam and thrown splinters",600,particleMaterial,.75f);
            glints=Emitter("Embers snow spores and sparks",720,particleMaterial,0);
            haze=Emitter("Low opacity breath ash and ink",200,smokeMaterial,-.04f);
            for(int i=0;i<AccentLimit;i++){
                var go=new GameObject("Pooled themed sculpture "+i);go.transform.SetParent(pool.transform,false);
                var a=new Accent{transform=go.transform,filter=go.AddComponent<MeshFilter>(),renderer=go.AddComponent<MeshRenderer>()};
                Setup(a.renderer,meshMaterial);a.renderer.enabled=false;accents[i]=a;
            }
            for(int i=0;i<PathLimit;i++){
                var go=new GameObject("Pooled lightning or acoustic path "+i);go.transform.SetParent(pool.transform,false);
                var line=go.AddComponent<LineRenderer>();Setup(line,pathMaterial);line.useWorldSpace=true;
                line.numCornerVertices=1;line.numCapVertices=2;line.enabled=false;paths[i]=new Path{line=line};
            }
            var triangles=new int[WaveSegments*12];
            for(int i=0;i<WaveSegments;i++)for(int band=0;band<2;band++){
                int p=i*3+band,t=(i*2+band)*6;triangles[t]=p;triangles[t+1]=p+3;triangles[t+2]=p+1;triangles[t+3]=p+1;triangles[t+4]=p+3;triangles[t+5]=p+4;
            }
            for(int i=0;i<WaveLimit;i++){
                var go=new GameObject("Pooled terrain-following wavefront "+i);go.transform.SetParent(pool.transform,false);
                var w=new Wave{mesh=new Mesh{name="Three dimensional wave sheet "+i},renderer=go.AddComponent<MeshRenderer>(),vertices=new Vector3[(WaveSegments+1)*3],colors=new Color[(WaveSegments+1)*3]};
                w.mesh.MarkDynamic();w.mesh.vertices=w.vertices;w.mesh.triangles=triangles;meshes.Add(w.mesh);go.AddComponent<MeshFilter>().sharedMesh=w.mesh;Setup(w.renderer,meshMaterial);w.renderer.enabled=false;waves[i]=w;
            }
        }
        static void Setup(Renderer renderer,Material material){renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;}
        Texture2D Sprite(bool smoke)
        {
            const int size=48;var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name=smoke?"Hand shaped torn vapour":"Hand shaped foam sparkle",wrapMode=TextureWrapMode.Clamp};
            var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++){
                float u=(x+.5f)/size*2-1,v=(y+.5f)/size*2-1;
                float n=Mathf.PerlinNoise(u*4+12,v*4+27);
                float d=smoke?Mathf.Sqrt(u*u+v*v)+(n-.5f)*.5f:Mathf.Abs(u)*.72f+Mathf.Abs(v)*1.05f;
                pixels[y*size+x]=new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-d),smoke?1.7f:2));
            }
            texture.SetPixels(pixels);texture.Apply(false,true);return texture;
        }
        ParticleSystem Emitter(string name,int limit,Material material,float gravity)
        {
            var go=new GameObject(name);go.transform.SetParent(pool.transform,false);var ps=go.AddComponent<ParticleSystem>();
            ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.loop=true;main.playOnAwake=false;main.duration=10;main.maxParticles=limit;main.startSpeed=0;main.startLifetime=.5f;main.startSize=.1f;main.gravityModifier=gravity;main.simulationSpace=ParticleSystemSimulationSpace.World;
            var emission=ps.emission;emission.enabled=false;var shape=ps.shape;shape.enabled=false;
            var c=ps.colorOverLifetime;c.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.1f),new GradientAlphaKey(.65f,.6f),new GradientAlphaKey(0,1)});c.color=gradient;
            var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.8f,1,material==smokeMaterial?2:0));
            var renderer=ps.GetComponent<ParticleSystemRenderer>();Setup(renderer,material);renderer.renderMode=ParticleSystemRenderMode.Billboard;
            ps.Play();return ps;
        }
        float Ground(Vector3 p){return Mathf.Max(-.35f,game.World.GroundAt(p));}
        Vector3 Surface(Vector3 p,float height=.14f){p.y=Ground(p)+height;return p;}
        static float Hash(int seed){float n=Mathf.Sin(seed*12.9898f+78.233f)*43758.5453f;return n-Mathf.Floor(n);}
        static Vector3 Radial(float a){return new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));}
        void Particle(ParticleSystem system,Vector3 point,Vector3 velocity,Color color,float size,float life,int seed)
        {system.Emit(new ParticleSystem.EmitParams{position=point,velocity=velocity,startColor=color,startSize=size,startLifetime=life,rotation=Hash(seed)*360},1);Emitted++;cleared=false;}
        void Sculpt(Mesh mesh,Vector3 point,Vector3 scale,Color color,float life,float yaw,float growth=0,float spin=0,Vector3 velocity=default(Vector3))
        {
            var a=accents[accentCursor++%AccentLimit];a.filter.sharedMesh=mesh;a.start=point;a.velocity=velocity;a.scale=scale;a.rotation=Quaternion.Euler(0,yaw,0);a.color=color;a.age=0;a.life=life;a.growth=growth;a.spin=spin;
            a.transform.SetPositionAndRotation(point,a.rotation);a.transform.localScale=scale;a.renderer.enabled=true;tint.SetColor(Tint,color);a.renderer.SetPropertyBlock(tint);Emitted++;cleared=false;
        }
        Path NewPath(Color color,float width,float life,int points)
        {
            var p=paths[pathCursor++%PathLimit];p.color=color;p.age=0;p.life=life;p.width=width;p.line.positionCount=points;p.line.startColor=p.line.endColor=color;p.line.startWidth=width;p.line.endWidth=width*.45f;p.line.enabled=true;Emitted++;cleared=false;return p;
        }
        void Lightning(Vector3 from,Vector3 to,float width,float life,int seed,bool forks)
        {
            var halo=NewPath(new Color(.3f,.25f,.95f,.72f),width*2.8f,life,13);halo.line.sortingOrder=1;
            var p=NewPath(new Color(.92f,.94f,1,1),width*.9f,life,13);p.line.sortingOrder=2;
            Vector3 axis=to-from,side=Vector3.Cross(axis.normalized,Vector3.forward);if(side.sqrMagnitude<.1f)side=Vector3.right;
            for(int i=0;i<13;i++){float f=i/12f;Vector3 offset=(side*(Hash(seed+i*7)*2-1)+Vector3.forward*(Hash(seed+i*17)*2-1))*Mathf.Sin(f*Mathf.PI)*Mathf.Min(.75f,axis.magnitude*.14f);Vector3 vertex=Vector3.Lerp(from,to,f)+offset;p.line.SetPosition(i,vertex);halo.line.SetPosition(i,vertex);}
            if(forks)for(int j=0;j<2;j++){
                Vector3 start=p.line.GetPosition(4+j*3);Vector3 end=start+new Vector3((j==0?-1:1)*1.1f,-1.4f,.3f);
                var branch=NewPath(new Color(.68f,.72f,1,.86f),width*.48f,life*.8f,4);branch.line.sortingOrder=2;
                for(int k=0;k<4;k++)branch.line.SetPosition(k,Vector3.Lerp(start,end,k/3f)+side*(k%2==0?.12f:-.12f));
            }
        }
        public void Tell(SkillTheme theme,Vector3 point,Vector3 end,float radius,int kind,float progress,int variant)
        {
            // Low, intermittent motif inside the orange boundary; it never hides the countdown.
            int seed=variant*31+(int)(Time.time*8);Color color=SkillThemes.ColorOf(theme);color.a=.34f;
            int count=kind==0?3:2;
            for(int i=0;i<count;i++){
                float a=(Hash(seed+i)*6.283f);Vector3 p=kind==0?Vector3.Lerp(point,end,(i+1f)/(count+1)):point+Radial(a)*radius*.48f;p=Surface(p,.18f);
                if(theme==SkillTheme.Lightning){if(progress>.6f)Lightning(p+Vector3.up*(.3f+progress*.3f),p,.027f,.16f,seed+i,false);Particle(glints,p,Vector3.up*.25f,color,.13f,.35f,seed+i);}
                else if(theme==SkillTheme.Cinder)Particle(glints,p,Vector3.up*(.6f+progress),color,.16f,.45f,seed+i);
                else if(theme==SkillTheme.Frost||theme==SkillTheme.SaltCrystal||theme==SkillTheme.Mirror)Sculpt(theme==SkillTheme.Mirror?mirror:crystal,p,new Vector3(.12f,.2f+progress*.27f,.12f),color,.22f,a*57,0,theme==SkillTheme.Mirror?50:0);
                else if(theme==SkillTheme.VenomRoot||theme==SkillTheme.Coral||theme==SkillTheme.Ink)Sculpt(theme==SkillTheme.Coral?coral:theme==SkillTheme.Ink?tentacle:root,p,Vector3.one*(.18f+progress*.13f),color,.25f,a*57,.18f);
                else Particle(glints,p,Vector3.up*.2f,color,.2f,.35f,seed+i);
            }
        }
        public void Action(Enemy source,AttackStyle attack,Vector3 point)
        {
            SkillTheme theme=SkillThemes.ForSpecies(source.Spec);Color c=SkillThemes.ColorOf(theme);int seed=source.Spec.id;
            // An ability's motion language is independent of its element. Support casts rise,
            // hatch calls spiral, burrows sink, bites close sideways and charges leave a wake.
            if(attack==AttackStyle.Heal){
                for(int i=0;i<5;i++){float a=i*1.2566f;Vector3 p=point+Radial(a)*.55f;Sculpt(rib,p,new Vector3(.25f,.5f,.25f),new Color(.7f,1,.64f,.65f),1.1f,a*Mathf.Rad2Deg,.5f,30,Vector3.up*.7f);Particle(glints,p,Vector3.up*1.2f,Color.Lerp(c,Color.white,.4f),.15f,1.1f,seed+i);}
            }else if(attack==AttackStyle.Split){
                for(int i=0;i<6;i++){float a=i*1.047f;Vector3 p=Surface(point+Radial(a)*.5f);Sculpt(coral,p,new Vector3(.17f,.45f,.17f),c,.85f,a*Mathf.Rad2Deg,.6f,110,Radial(a)*.45f);}
            }else if(attack==AttackStyle.Burrow){
                for(int i=0;i<6;i++){float a=i*1.047f;Vector3 p=Surface(point+Radial(a)*.55f,.5f);Sculpt(theme==SkillTheme.VenomRoot?root:crystal,p,new Vector3(.15f,.5f,.15f),c,.7f,a*Mathf.Rad2Deg,0,70,Vector3.down*.65f);Particle(haze,p,Radial(a)*.35f,new Color(.5f,.44f,.3f,.22f),.6f,.65f,seed+i);}
            }else if(attack==AttackStyle.Bite){
                Vector3 mouth=point+source.transform.forward*.8f+Vector3.up*.25f;
                for(int i=-1;i<=1;i+=2)Sculpt(crest,mouth+source.transform.right*.32f*i,new Vector3(.4f,.35f,.5f),c,.24f,source.transform.eulerAngles.y+i*70,0,-i*160,-source.transform.right*i*.7f);
            }else if(attack==AttackStyle.Charge||attack==AttackStyle.Leap){
                for(int i=-1;i<=1;i+=2)Sculpt(crest,Surface(point+source.transform.right*i*.45f),new Vector3(.45f,.45f,1),c,.5f,source.transform.eulerAngles.y+i*20,.3f,0,-source.transform.forward*1.3f);
            }else if(attack==AttackStyle.Pull){
                for(int i=0;i<4;i++){float a=i*1.57f;Sculpt(theme==SkillTheme.Ink?tentacle:sail,point+Radial(a)*.5f,Vector3.one*.4f,c,.5f,a*Mathf.Rad2Deg,.2f,180);}
            }else if(attack==AttackStyle.Spiral||attack==AttackStyle.Fan||attack==AttackStyle.Boomerang){
                for(int i=0;i<3;i++){float a=i*2.094f;Particle(glints,point+Radial(a)*.5f,Radial(a)*.7f,c,.17f,.35f,seed+i);}
            }else {Particle(glints,point+Vector3.up*.4f,Vector3.up*.35f,c,.2f,.35f,seed);}
        }
        public void Guard(Enemy source,Vector3 point)
        {
            for(int i=0;i<6;i++){float a=i*1.047f;Sculpt(coral,point+Radial(a)*.52f,new Vector3(.25f,.7f,.25f),new Color(1,.63f,.49f,.72f),1.15f,a*Mathf.Rad2Deg,.3f);}
        }
        void Contact(SkillTheme theme,Vector3 point,float radius,int variant)
        {
            // Mechanism success, projectile contacts and airborne targets keep their
            // supplied world-space point; only ground hazards are snapped to terrain.
            float scale=Mathf.Clamp(radius,.25f,2.8f);Color c=SkillThemes.ColorOf(theme);c.a=.82f;
            for(int i=0;i<3;i++){
                float a=i*2.094f+variant*.71f;Vector3 outward=Radial(a),p=point+outward*scale*.18f;
                if(theme==SkillTheme.Lightning)Lightning(p+Vector3.up*scale*.7f,p+outward*scale*.45f,.065f,.32f,variant+i,false);
                else{
                    Mesh form=theme==SkillTheme.Cinder?flame:theme==SkillTheme.Frost||theme==SkillTheme.SaltCrystal?crystal:theme==SkillTheme.Coral?coral:theme==SkillTheme.VenomRoot?root:theme==SkillTheme.Wraith?sail:theme==SkillTheme.Mirror?mirror:theme==SkillTheme.Ink?tentacle:theme==SkillTheme.WhaleSong?rib:crest;
                    Sculpt(form,p,new Vector3(scale*.45f,scale*.8f,scale*.45f),c,.6f,a*Mathf.Rad2Deg,.35f,35,outward*.3f+Vector3.up*.25f);
                }
                EmitTheme(theme,p,outward,4,scale*.7f,variant+i);
            }
        }
        public void Release(SkillTheme theme,Vector3 point,Vector3 end,float radius,int kind,int variant)
        {
            if(Time.time>=nextReleaseSound){
                nextReleaseSound=Time.time+.11f;PlayedReleaseSounds++;
                string cue=theme==SkillTheme.Lightning?"arc":theme==SkillTheme.Cinder?"explosion":theme==SkillTheme.Frost||theme==SkillTheme.SaltCrystal?"freeze":theme==SkillTheme.Mirror||theme==SkillTheme.Wraith?"beam":theme==SkillTheme.WhaleSong?"splash":kind==0?"beam":"splash";
                game.Audio.Cue(cue);
            }
            float r=Mathf.Clamp(radius,.4f,5);int count=kind==0?6:theme==SkillTheme.Lightning?3:6;
            Color color=SkillThemes.ColorOf(theme);Vector3 center=Surface(point);
            for(int i=0;i<count;i++){
                float a=i*6.283185f/count+variant*.63f;Vector3 outward=Radial(a);
                Vector3 p=kind==0?Surface(Vector3.Lerp(point,end,(i+.5f)/count)):Surface(point+outward*r*(i==0?0:.57f));
                float size=(.7f+r*.26f)*Mathf.Lerp(.72f,1.24f,Hash(variant+i*19)),yaw=a*Mathf.Rad2Deg+90;
                switch(theme){
                    case SkillTheme.Lightning:Lightning(p+Vector3.up*(6.4f+Hash(variant+i)*2),p,.10f,.38f,variant+i*19,true);Lightning(p,p+outward*r*.65f,.046f,.3f,variant+i,false);break;
                    case SkillTheme.Cinder:
                        Sculpt(flame,p,new Vector3(size*1.3f,1.5f*size,size*1.3f),new Color(1,.3f,.045f,.9f),.8f+Hash(i+variant)*.25f,yaw,.25f,42,Vector3.up*.4f);
                        Sculpt(flame,p+Vector3.up*.06f,new Vector3(size*.62f,.95f*size,size*.62f),new Color(1,.85f,.3f,.95f),.6f,-yaw,.2f,-70,Vector3.up*.65f);
                        Particle(haze,p+Vector3.up*.17f,Vector3.up*.15f,new Color(1,.4f,.05f,.75f),size*1.5f,.6f,variant+i);break;
                    case SkillTheme.Frost:Sculpt(crystal,p,new Vector3(.42f*size,1.7f*size,.42f*size),new Color(.47f,.79f,1,.86f),1.2f,yaw,.15f);break;
                    case SkillTheme.SaltCrystal:Sculpt(crystal,p,new Vector3(.72f*size,.93f*size,.72f*size),new Color(1,.78f,.38f,.85f),1.15f,yaw,.35f,22);break;
                    case SkillTheme.VenomRoot:Sculpt(root,p,Vector3.one*size,color,1.1f,yaw,.45f,-28);break;
                    case SkillTheme.Coral:Sculpt(coral,p,new Vector3(size*.9f,size*1.25f,size*.9f),color,1.0f,yaw,.55f);break;
                    case SkillTheme.Wraith:Sculpt(sail,p,new Vector3(size*.85f,size*1.8f,size*.8f),new Color(.38f,.86f,.76f,.66f),1.25f,yaw,.4f,50,Vector3.up*.45f);break;
                    case SkillTheme.Mirror:Sculpt(mirror,p,new Vector3(size*.45f,size*1.6f,size*.45f),new Color(.8f,.93f,1,.82f),.95f,yaw,.25f,180);Sculpt(mirror,p+outward*.3f,new Vector3(size*.28f,size,size*.28f),new Color(.62f,.44f,.9f,.65f),1.1f,-yaw,.25f,-180);break;
                    case SkillTheme.Ink:Sculpt(tentacle,p,new Vector3(size*.7f,size*1.7f,size*.7f),new Color(.29f,.1f,.42f,.96f),1.1f,yaw,.4f,22);break;
                    case SkillTheme.WhaleSong:Sculpt(rib,p,new Vector3(size*1.2f,size*1.35f,size),new Color(.75f,1,1,.6f),.85f,yaw,.55f);break;
                    default:Sculpt(crest,p,new Vector3(size*1.15f,size*.85f,size),new Color(.65f,.93f,.96f,.75f),.75f,yaw,.35f,0,outward*.7f);break;
                }
                EmitTheme(theme,p,outward,theme==SkillTheme.Cinder||theme==SkillTheme.Tide?11:5,size,variant+i);
            }
            if(kind!=0&&(theme==SkillTheme.Tide||theme==SkillTheme.WhaleSong||theme==SkillTheme.Wraith))WaveAt(theme,center,r*.7f,.65f,variant);
        }
        public void Sustain(SkillTheme theme,Vector3 point,Vector3 end,float radius,int kind,int variant,float age)
        {
            if(kind==1){WaveAt(theme,point,radius,.075f,variant);return;}
            int step=(int)(age*6);float a=step*2.399963f+variant;Vector3 outward=Radial(a);Vector3 p=Surface(point+outward*radius*.57f);
            if(kind==0)p=Surface(Vector3.Lerp(point,end,Hash(step+variant)));
            Color c=SkillThemes.ColorOf(theme);c.a=.68f;
            if(theme==SkillTheme.Lightning)Lightning(p+Vector3.up*.65f,p+outward*.65f,.048f,.2f,step+variant,false);
            else if(theme==SkillTheme.Cinder)Sculpt(flame,p,new Vector3(.48f,1.25f,.48f),c,.45f,a*57,.3f,30);
            else if(theme==SkillTheme.Frost||theme==SkillTheme.SaltCrystal)Sculpt(crystal,p,new Vector3(.32f,theme==SkillTheme.Frost?.95f:.5f,.32f),c,.45f,a*57,.1f);
            else if(theme==SkillTheme.VenomRoot||theme==SkillTheme.Ink||theme==SkillTheme.Coral)Sculpt(theme==SkillTheme.VenomRoot?root:theme==SkillTheme.Ink?tentacle:coral,p,Vector3.one*.55f,c,.5f,a*57,.25f);
            else if(theme==SkillTheme.Mirror)Sculpt(mirror,p,new Vector3(.35f,.9f,.35f),c,.45f,a*57,.1f,120);
            else if(theme==SkillTheme.Wraith)Sculpt(sail,p,new Vector3(.45f,1.1f,.5f),c,.6f,a*57,.3f,40);
            else if(kind==3){Sculpt(crest,p,new Vector3(.7f,.45f,.7f),c,.45f,a*57-90,.1f,70);}
            EmitTheme(theme,p,kind==3?-outward:outward,3,.6f,variant+step);
        }
        void EmitTheme(SkillTheme theme,Vector3 p,Vector3 outward,int count,float size,int seed)
        {
            var c=SkillThemes.ColorOf(theme);
            for(int n=0;n<count;n++){
                float h=Hash(seed+n*7),s=Hash(seed+n*11);Vector3 spread=Radial(h*6.283f);
            if(theme==SkillTheme.Cinder){Particle(glints,p+spread*.32f,spread*.75f+Vector3.up*(1.6f+h*2.1f),Color.Lerp(c,new Color(1,.85f,.36f),s),.2f*size,.6f+h*.5f,seed+n);if(n%3==0)Particle(haze,p+Vector3.up*.5f,spread*.3f+Vector3.up*.7f,new Color(.16f,.14f,.16f,.37f),.65f*size,1,seed+n);}
                else if(theme==SkillTheme.Ink){Particle(haze,p+Vector3.up*.35f,spread*.6f+Vector3.up*.55f,new Color(.09f,.04f,.16f,.57f),.9f*size,.9f,seed+n);Particle(spray,p,spread+Vector3.up*(1+h),c,.2f,.7f,seed+n);}
                else if(theme==SkillTheme.VenomRoot){Particle(glints,p+Vector3.up*.25f,spread*.25f+Vector3.up*(.6f+h),new Color(.7f,.93f,.23f,.7f),.21f*size,1,seed+n);}
                else if(theme==SkillTheme.Frost){Particle(glints,p+Vector3.up*(.5f+h),spread*.5f+Vector3.down*.25f,new Color(.85f,.97f,1,.8f),.13f*size,1.1f,seed+n);}
                else if(theme==SkillTheme.Wraith){Particle(haze,p+Vector3.up*.3f,spread*.2f+Vector3.up*.5f,new Color(.4f,.8f,.64f,.25f),.7f*size,1.2f,seed+n);}
                else if(theme==SkillTheme.Coral){Particle(glints,p+Vector3.up*.45f,spread*.45f+Vector3.up*.7f,new Color(1,.75f,.67f,.8f),.17f*size,.8f,seed+n);}
                else if(theme==SkillTheme.Lightning){Particle(glints,p+Vector3.up*.1f,spread*(2+h)+Vector3.up*h,new Color(.75f,.86f,1,.9f),.15f,.3f,seed+n);}
                else {Particle(spray,p+Vector3.up*.3f,(outward+spread*.4f)*(1.2f+h)+Vector3.up*(1.1f+h*1.8f),Color.Lerp(c,Color.white,.65f),.14f*size,.65f+h*.2f,seed+n);}
            }
        }
        public void Bolt(SkillTheme theme,Vector3 point,Vector3 velocity,int variant,float age)
        {
            Vector3 back=-velocity.normalized;Color c=SkillThemes.ColorOf(theme);c.a=.88f;
            if(theme==SkillTheme.Lightning)Lightning(point+back*.7f,point,.04f,.13f,variant+(int)(age*30),false);
            else if(theme==SkillTheme.Mirror)Sculpt(mirror,point,new Vector3(.16f,.42f,.16f),c,.15f,age*370,0,240);
            else if(theme==SkillTheme.Frost||theme==SkillTheme.SaltCrystal)Sculpt(crystal,point,new Vector3(.13f,.37f,.13f),c,.15f,age*100,0,90);
            else if(theme==SkillTheme.VenomRoot)Sculpt(root,point,Vector3.one*.23f,c,.16f,age*240,0,90);
            else if(theme==SkillTheme.Coral)Sculpt(coral,point,Vector3.one*.22f,c,.16f,age*200,0,90);
            else if(theme==SkillTheme.WhaleSong)Sculpt(rib,point,Vector3.one*.35f,c,.2f,age*90,.3f);
            else if(theme==SkillTheme.Tide)Sculpt(crest,point,Vector3.one*.3f,c,.17f,Mathf.Atan2(velocity.x,velocity.z)*Mathf.Rad2Deg);
            else if(theme==SkillTheme.Cinder)Sculpt(flame,point,new Vector3(.23f,.55f,.23f),c,.17f,age*180,.2f);
            else if(theme==SkillTheme.Wraith)Sculpt(sail,point,new Vector3(.25f,.5f,.25f),c,.2f,age*170);
            else if(theme==SkillTheme.Ink)Sculpt(tentacle,point,Vector3.one*.25f,c,.2f,age*180);
            Particle(theme==SkillTheme.Ink||theme==SkillTheme.Wraith?haze:glints,point,back*.6f,c,theme==SkillTheme.Ink?.4f:.15f,.25f,variant+(int)(age*12));
        }
        public void WaveAt(SkillTheme theme,Vector3 point,float radius,float life,int variant)
        {
            var w=waves[waveCursor++%WaveLimit];w.age=0;w.life=life;w.color=SkillThemes.ColorOf(theme);w.color.a=.7f;
            float width=theme==SkillTheme.WhaleSong?.24f:.4f,height=theme==SkillTheme.Cinder?.45f:theme==SkillTheme.Ink?.4f:.32f;
            // Foam hugs the same moving radial hit band and terrain as its boundary.
            for(int i=0;i<=WaveSegments;i++){
                float a=i*6.283185f/WaveSegments;Vector3 radial=Radial(a);float surface=Ground(point+radial*radius);
                float scallop=theme==SkillTheme.WhaleSong?Mathf.Sin(a*18)*.035f:Mathf.Sin(a*12+variant)*.075f;
                for(int band=0;band<3;band++){
                    Vector3 p=point+radial*Mathf.Max(.05f,radius+(band-1)*width);p.y=surface+.12f+(band==1?height+scallop:0);
                    w.vertices[i*3+band]=p;w.colors[i*3+band]=band==1?Color.white:new Color(.65f,.86f,.95f,band==2?.2f:.03f);
                }
            }
            w.mesh.vertices=w.vertices;w.mesh.colors=w.colors;w.mesh.RecalculateBounds();w.renderer.enabled=true;tint.SetColor(Tint,w.color);w.renderer.SetPropertyBlock(tint);Emitted++;cleared=false;
            int seed=variant+(int)(Time.time*10);
            for(int i=0;i<5;i++){float a=(i+.2f*Hash(seed))*6.283f/5+Time.time*.7f;Vector3 radial=Radial(a);EmitTheme(theme,Surface(point+radial*radius,.24f),radial,1,.65f,seed+i);}
            if(theme==SkillTheme.Lightning){float a=seed*.81f;Vector3 from=Surface(point+Radial(a)*radius,.3f),to=Surface(point+Radial(a+.18f)*radius,.3f);Lightning(from,to,.045f,life,seed,false);}
            if(theme==SkillTheme.WhaleSong){for(int i=0;i<3;i++){float a=Time.time*.3f+i*2.094f;Sculpt(rib,Surface(point+Radial(a)*radius),new Vector3(.6f,.35f,.6f),new Color(.7f,1,1,.55f),life,a*Mathf.Rad2Deg+90);}}
        }
        void Update()
        {
            if(!game||!pool)return;
            if(game.Paused){if(!paused){spray.Pause();glints.Pause();haze.Pause();paused=true;}return;}
            if(paused){spray.Play();glints.Play();haze.Play();paused=false;}
            if(game.State!=VoyageState.Combat){if(!cleared)Clear();return;}
            float dt=Time.deltaTime;
            foreach(var a in accents){if(!a.renderer.enabled)continue;a.age+=dt;float f=a.age/a.life;if(f>=1){a.renderer.enabled=false;continue;}a.transform.position=a.start+a.velocity*a.age;a.transform.localScale=a.scale*(1+a.growth*Mathf.Sin(f*Mathf.PI*.5f));a.transform.rotation=a.rotation*Quaternion.Euler(0,a.spin*a.age,0);var c=a.color;c.a*=Mathf.Min(1,(1-f)*3);tint.SetColor(Tint,c);a.renderer.SetPropertyBlock(tint);}
            foreach(var p in paths){if(!p.line.enabled)continue;p.age+=dt;float f=p.age/p.life;if(f>=1){p.line.enabled=false;continue;}Color c=p.color;c.a*=1-f;p.line.startColor=p.line.endColor=c;p.line.startWidth=p.width*(1-f*.6f);p.line.endWidth=p.width*.45f*(1-f*.6f);}
            foreach(var w in waves){if(!w.renderer.enabled)continue;w.age+=dt;float f=w.age/w.life;if(f>=1){w.renderer.enabled=false;continue;}var c=w.color;c.a*=1-f;tint.SetColor(Tint,c);w.renderer.SetPropertyBlock(tint);}
        }
        public void Clear()
        {
            foreach(var a in accents)if(a!=null)a.renderer.enabled=false;foreach(var p in paths)if(p!=null)p.line.enabled=false;foreach(var w in waves)if(w!=null)w.renderer.enabled=false;
            if(spray){spray.Clear();glints.Clear();haze.Clear();}nextReleaseSound=Time.time;cleared=true;
        }
        void OnDestroy(){foreach(var mesh in meshes)if(mesh)Destroy(mesh);if(meshMaterial)Destroy(meshMaterial);if(particleMaterial)Destroy(particleMaterial);if(smokeMaterial)Destroy(smokeMaterial);if(pathMaterial)Destroy(pathMaterial);if(glintTexture)Destroy(glintTexture);if(smokeTexture)Destroy(smokeTexture);if(pool)Destroy(pool);}

        Mesh Mesh(string name,List<Vector3> vertices,List<int> triangles,List<Color> colors)
        {var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetColors(colors);mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);return mesh;}
        static void Quad(List<int> t,int a,int b,int c,int d){t.Add(a);t.Add(b);t.Add(c);t.Add(a);t.Add(c);t.Add(d);}
        Mesh WaveCrest()
        {
            var v=new List<Vector3>();var t=new List<int>();var c=new List<Color>();
            const int columns=18,rows=10;
            for(int x=0;x<=columns;x++)for(int y=0;y<=rows;y++){
                float u=x/(float)columns,f=y/(float)rows,edge=Mathf.Pow(Mathf.Sin(u*Mathf.PI),.45f);
                float curl=Mathf.SmoothStep(0,1,Mathf.Clamp01((f-.63f)/.37f));
                float height=Mathf.Sin(f*Mathf.PI*.76f)*(.66f+.075f*Mathf.Sin(u*34))*edge;
                v.Add(new Vector3((u-.5f)*1.85f,height,(f*.78f-.38f-curl*.24f)*edge));
                float foam=Mathf.SmoothStep(.55f,.9f,f);Color pigment=Color.Lerp(new Color(.5f,.8f,.87f),new Color(1.5f,1.12f,1.1f),foam);
                pigment.a=edge*Mathf.Clamp01(f*4)*(.48f+foam*.38f);c.Add(pigment);
                if(x>0&&y>0){int k=x*(rows+1)+y;Quad(t,k-rows-2,k-1,k,k-rows-1);}
            }
            return Mesh("Feathered foaming wave with curled lip",v,t,c);
        }
        Mesh FlameTongues()
        {
            var v=new List<Vector3>();var t=new List<int>();var c=new List<Color>();
            // Three curved feathered surfaces: no solid card silhouette from any view.
            for(int tongue=0;tongue<3;tongue++){
                int start=v.Count;float phase=tongue*2.094f;
                for(int i=0;i<=16;i++){
                    float f=i/16f,width=(.19f+.1f*Mathf.Sin(f*7+phase))*Mathf.Pow(1-f,.75f);
                    Vector3 side=Radial(phase+f*3.5f),p=new Vector3(Mathf.Sin(f*7+phase)*f*.2f,f*(1-tongue*.12f),Mathf.Cos(f*6+phase)*f*.13f);
                    for(int j=-1;j<=1;j++){v.Add(p+side*width*j);Color pigment=Color.Lerp(new Color(1,1,.9f),new Color(1,.65f,.32f),f);pigment.a=j==0?Mathf.Clamp01((1-f)*2):0;c.Add(pigment);}
                    if(i>0)for(int strip=0;strip<2;strip++){int k=start+i*3+strip;Quad(t,k-3,k,k+1,k-2);}
                }
            }
            return Mesh("Three twisting feathered flame tongues",v,t,c);
        }
        Mesh Crystal()
        {
            var v=new List<Vector3>();var t=new List<int>();var c=new List<Color>();
            // Independent faces retain the sharp cut of the prism, rather than a smooth cone.
            for(int side=0;side<6;side++){
                float a=side*6.283185f/6,b=(side+1)*6.283185f/6;Vector3 p=Radial(a)*.5f,q=Radial(b)*.5f;
                int k=v.Count;v.Add(p);v.Add(q);v.Add(q*.7f+Vector3.up*.68f);v.Add(p*.7f+Vector3.up*.68f);v.Add(new Vector3(.13f,1,0));
                for(int i=0;i<5;i++)c.Add(Color.Lerp(new Color(.38f,.58f,.7f,.65f),Color.white,side%2==0?.85f:.38f));Quad(t,k,k+1,k+2,k+3);t.Add(k+3);t.Add(k+2);t.Add(k+4);
            }
            return Mesh("Six-sided cleaved crystal",v,t,c);
        }
        static void Tube(List<Vector3> v,List<int> t,List<Color> c,Vector3[] points,float radius,Color bottom,Color tip)
        {
            int start=v.Count;const int sides=5;
            for(int i=0;i<points.Length;i++){
                float f=i/(float)(points.Length-1);Vector3 forward=(points[Mathf.Min(i+1,points.Length-1)]-points[Mathf.Max(i-1,0)]).normalized;Vector3 axis=Vector3.Cross(forward,Vector3.forward);if(axis.sqrMagnitude<.01f)axis=Vector3.right;axis.Normalize();Vector3 other=Vector3.Cross(forward,axis);
                for(int j=0;j<sides;j++){float a=j*6.283185f/sides;v.Add(points[i]+(axis*Mathf.Cos(a)+other*Mathf.Sin(a))*radius*Mathf.Lerp(1,.06f,f));c.Add(Color.Lerp(bottom,tip,f));}
                if(i>0)for(int j=0;j<sides;j++){int a=start+(i-1)*sides+j,b=start+(i-1)*sides+(j+1)%sides;Quad(t,a,b,b+sides,a+sides);}
            }
        }
        Mesh Branch(bool reef)
        {
            var v=new List<Vector3>();var t=new List<int>();var c=new List<Color>();
            Vector3[] points=reef?new[]{Vector3.zero,new Vector3(.05f,.3f,0),new Vector3(-.1f,.63f,.03f),new Vector3(.08f,1,.06f)}:new[]{Vector3.zero,new Vector3(.22f,.18f,0),new Vector3(.3f,.54f,.12f),new Vector3(.06f,.86f,.22f),new Vector3(-.17f,.75f,.27f)};
            Tube(v,t,c,points,.12f,new Color(.35f,.5f,.27f),Color.white);
            for(int j=0;j<3;j++){Vector3 p=points[j+1];float side=j%2==0?-1:1;Tube(v,t,c,new[]{p,p+new Vector3(side*.23f,.16f,.04f),p+new Vector3(side*.28f,.37f,-.04f)},reef?.065f:.048f,new Color(.65f,.75f,.7f),Color.white);}
            return Mesh(reef?"Branching coral antlers":"Curling thorn root",v,t,c);
        }
        Mesh Sail()
        {
            var v=new List<Vector3>();var t=new List<int>();var c=new List<Color>();
            for(int i=0;i<=12;i++){float f=i/12f;float w=Mathf.Sin(f*Mathf.PI)*.4f+.035f;Vector3 p=new Vector3(Mathf.Sin(f*6)*.2f,f,Mathf.Cos(f*4)*.12f);v.Add(p+Vector3.right*w);v.Add(p-Vector3.right*w*(i%3==0?.5f:1));c.Add(new Color(.75f,1,.86f,.7f*(1-f)));c.Add(new Color(1,1,1,.5f*(1-f)));if(i>0)Quad(t,i*2-2,i*2,i*2+1,i*2-1);}
            return Mesh("Tattered spectral sail",v,t,c);
        }
        Mesh Mirror()
        {
            var v=new List<Vector3>{new Vector3(0,0,0),new Vector3(.4f,.4f,0),new Vector3(.12f,1,0),new Vector3(-.3f,.62f,0),new Vector3(0,.5f,.12f),new Vector3(0,.5f,-.12f)};
            var t=new List<int>();var c=new List<Color>{new Color(.4f,.5f,.9f),Color.white,new Color(.6f,1,1),new Color(.7f,.6f,1),Color.white,new Color(.5f,.6f,.8f)};
            for(int i=0;i<4;i++){t.Add(i);t.Add((i+1)%4);t.Add(4);t.Add((i+1)%4);t.Add(i);t.Add(5);}return Mesh("Cut mirrored kite",v,t,c);
        }
        Mesh Tentacle()
        {
            var v=new List<Vector3>();var t=new List<int>();var c=new List<Color>();var p=new Vector3[13];
            for(int i=0;i<p.Length;i++){float f=i/12f;p[i]=new Vector3(Mathf.Sin(f*4.5f)*.3f,f*.95f,Mathf.Cos(f*3)*f*.18f);}
            Tube(v,t,c,p,.18f,new Color(.36f,.22f,.47f),new Color(.85f,.62f,.9f));
            for(int i=1;i<6;i++)Tube(v,t,c,new[]{p[i]+Vector3.forward*.06f,p[i]+Vector3.forward*.18f,p[i]+Vector3.forward*.2f+Vector3.up*.02f},.06f,new Color(.8f,.55f,.73f),Color.white);
            return Mesh("Curling ink arm with suckers",v,t,c);
        }
        Mesh Rib()
        {
            var v=new List<Vector3>();var t=new List<int>();var c=new List<Color>();
            for(int i=0;i<=24;i++){float a=i*Mathf.PI/24;Vector3 p=new Vector3(Mathf.Cos(a)*.8f,Mathf.Sin(a)*.8f,0);v.Add(p);v.Add(p+new Vector3(0,.07f,0));c.Add(new Color(.6f,.9f,1,.1f));c.Add(Color.white);if(i>0)Quad(t,i*2-2,i*2,i*2+1,i*2-1);}
            return Mesh("Standing acoustic arch",v,t,c);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace Tidebreak
{
    // Geometry, textures, materials and renderers are created once. A crowded volley
    // replaces the oldest decoration instead of allocating or hiding gameplay objects.
    public sealed class CombatFeedback : MonoBehaviour
    {
        const int ShapeLimit=64,TraceLimit=36;
        sealed class Accent
        {
            public Transform transform;public MeshFilter filter;public MeshRenderer renderer;
            public Vector3 start,velocity,scale;public Color color;public float age,life,growth,spin;
        }
        sealed class Trace
        {
            public LineRenderer line;public Vector3 a,b;public Color color;public float age,life;public bool electric;
        }
        readonly Accent[] accents=new Accent[ShapeLimit];readonly Trace[] traces=new Trace[TraceLimit];
        readonly System.Random random=new System.Random(71147);
        MaterialPropertyBlock block;
        readonly List<Mesh> meshes=new List<Mesh>();
        ParticleSystem sparks,mist,droplets,motes;
        Material particleMaterial,mistMaterial,geometryMaterial;Texture2D sparkSprite,mistSprite;
        Mesh flameMesh,ringMesh,crownMesh,shardMesh,boltMesh;GameObject root;
        int accentCursor,traceCursor;
        static readonly int Tint=Shader.PropertyToID("_Tint");
        public int Emitted {get;private set;}
        public int ActiveAccents {get {int n=0;foreach(var a in accents)if(a!=null&&a.renderer.enabled)n++;return n;}}
        public int ActiveTracers {get {int n=0;foreach(var t in traces)if(t!=null&&t.line.enabled)n++;return n;}}
        float R(float min,float max){return Mathf.Lerp(min,max,(float)random.NextDouble());}
        Vector3 Noise(){return new Vector3(R(-1,1),R(-1,1),R(-1,1));}

        void Awake()
        {
            root=new GameObject("Pooled maritime combat effects");root.transform.SetParent(transform,false);block=new MaterialPropertyBlock();
            sparkSprite=Texture(false);mistSprite=Texture(true);
            particleMaterial=new Material(Resources.Load<Shader>("CombatParticle")){name="Salt light particles",mainTexture=sparkSprite};
            mistMaterial=new Material(Resources.Load<Shader>("CombatParticle")){name="Broken smoke particles",mainTexture=mistSprite};
            geometryMaterial=new Material(Resources.Load<Shader>("CombatGeometry")){name="Salt brass combat geometry"};
            flameMesh=Flame();ringMesh=Ring();crownMesh=Crown();shardMesh=Shard();boltMesh=Bolt();
            sparks=Emitter("Directional brass splinters",320,particleMaterial,.78f,shardMesh);
            droplets=Emitter("Sea spray needles",240,particleMaterial,1.15f,shardMesh);
            mist=Emitter("Layered powder and salt mist",128,mistMaterial,-.035f,null);
            motes=Emitter("Small glints",128,particleMaterial,.04f,null);
            for(int i=0;i<ShapeLimit;i++){
                var go=new GameObject("Reused accent "+i);go.transform.SetParent(root.transform,false);
                var a=new Accent{transform=go.transform,filter=go.AddComponent<MeshFilter>(),renderer=go.AddComponent<MeshRenderer>()};
                a.renderer.sharedMaterial=geometryMaterial;a.renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                a.renderer.receiveShadows=false;a.renderer.enabled=false;accents[i]=a;
            }
            for(int i=0;i<TraceLimit;i++){
                var go=new GameObject("Reused shot path "+i);go.transform.SetParent(root.transform,false);
                var line=go.AddComponent<LineRenderer>();line.sharedMaterial=particleMaterial;line.useWorldSpace=true;
                line.textureMode=LineTextureMode.Stretch;line.numCapVertices=2;line.numCornerVertices=1;
                line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;line.receiveShadows=false;line.enabled=false;
                traces[i]=new Trace{line=line};
            }
        }
        Texture2D Texture(bool smoke)
        {
            const int n=64;var texture=new Texture2D(n,n,TextureFormat.RGBA32,false);
            texture.name=smoke?"Hand shaped curling smoke":"Tapered salt glint";texture.wrapMode=TextureWrapMode.Clamp;
            var colors=new Color[n*n];
            for(int y=0;y<n;y++)for(int x=0;x<n;x++){
                float u=(x+.5f)/n*2-1,v=(y+.5f)/n*2-1;
                float noise=Mathf.PerlinNoise(u*3.2f+5,v*3.2f+8)*.62f+Mathf.PerlinNoise(u*7+13,v*7+9)*.38f;
                float d=smoke?Mathf.Sqrt(u*u+v*v)+(.5f-noise)*.38f:Mathf.Abs(u)*.76f+Mathf.Abs(v)*1.25f;
                float alpha=Mathf.Pow(Mathf.Clamp01(1-d),smoke?1.35f:1.7f);
                if(smoke)alpha*=Mathf.Lerp(.2f,1,noise);
                colors[y*n+x]=new Color(1,1,1,alpha);
            }
            texture.SetPixels(colors);texture.Apply(false,true);return texture;
        }
        ParticleSystem Emitter(string name,int limit,Material mat,float gravity,Mesh mesh)
        {
            var go=new GameObject(name);go.transform.SetParent(root.transform,false);
            var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.loop=true;main.playOnAwake=false;main.duration=10;main.maxParticles=limit;
            main.simulationSpace=ParticleSystemSimulationSpace.World;main.startSpeed=0;main.startLifetime=.4f;main.startSize=.1f;main.gravityModifier=gravity;
            var emission=ps.emission;emission.enabled=false;var shape=ps.shape;shape.enabled=false;
            var color=ps.colorOverLifetime;color.enabled=true;var gradient=new Gradient();
            gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(new Color(.62f,.73f,.76f),1)},
                new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(.8f,.25f),new GradientAlphaKey(0,1)});color.color=gradient;
            var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,mesh?AnimationCurve.Linear(0,1,1,.35f):AnimationCurve.Linear(0,.55f,1,1.7f));
            var rotation=ps.rotationOverLifetime;rotation.enabled=mesh;rotation.z=new ParticleSystem.MinMaxCurve(-5,5);
            var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=mat;
            renderer.renderMode=mesh?ParticleSystemRenderMode.Mesh:ParticleSystemRenderMode.Billboard;
            if(mesh){renderer.mesh=mesh;renderer.alignment=ParticleSystemRenderSpace.Velocity;}
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            ps.Play();return ps;
        }
        void Emit(ParticleSystem ps,Vector3 point,Vector3 velocity,Color color,float size,float life)
        {
            ps.Emit(new ParticleSystem.EmitParams{position=point,velocity=velocity,startColor=color,startSize=size,startLifetime=life,rotation=R(0,360)},1);Emitted++;
        }
        void AccentAt(Mesh mesh,Vector3 point,Vector3 direction,Vector3 scale,Color color,float life,float growth=0,float spin=0,Vector3 velocity=default(Vector3))
        {
            var a=accents[accentCursor++%ShapeLimit];a.filter.sharedMesh=mesh;a.start=point;a.velocity=velocity;a.scale=scale;
            a.color=color;a.age=0;a.life=life;a.growth=growth;a.spin=spin;
            a.transform.SetPositionAndRotation(point,Quaternion.LookRotation(direction.sqrMagnitude>.001f?direction.normalized:Vector3.up));
            a.transform.localScale=scale;a.renderer.enabled=true;block.SetColor(Tint,color);a.renderer.SetPropertyBlock(block);Emitted++;
        }
        void Update()
        {
            float dt=Time.deltaTime;
            foreach(var a in accents){
                if(a==null||!a.renderer.enabled)continue;a.age+=dt;float t=a.age/a.life;
                if(t>=1){a.renderer.enabled=false;continue;}
                a.transform.position=a.start+a.velocity*a.age;a.transform.localScale=a.scale*(1+a.growth*(1-(1-t)*(1-t)));
                if(a.spin!=0)a.transform.Rotate(0,0,a.spin*dt,Space.Self);
                var c=a.color;c.a*=Mathf.Pow(1-t,.7f);block.SetColor(Tint,c);a.renderer.SetPropertyBlock(block);
            }
            foreach(var t in traces){
                if(t==null||!t.line.enabled)continue;t.age+=dt;float f=t.age/t.life;if(f>=1){t.line.enabled=false;continue;}
                var c=t.color;c.a*=Mathf.Pow(1-f,1.4f);t.line.startColor=c;t.line.endColor=new Color(c.r,c.g,c.b,0);
                if(!t.electric){
                    // Travelling highlights give shot direction without leaving a beam through the target.
                    t.line.SetPosition(0,Vector3.Lerp(t.a,t.b,Mathf.Clamp01(f*.8f)));
                    t.line.SetPosition(1,Vector3.Lerp(t.a,t.b,Mathf.Clamp01(.35f+f*1.8f)));
                }
            }
        }
        public void Tracer(Vector3 a,Vector3 b,Color color)
        {
            if(!root||(b-a).sqrMagnitude<.001f)return;
            var t=traces[traceCursor++%TraceLimit];t.a=a;t.b=b;t.age=0;t.color=color;
            t.electric=color.r<.48f&&color.g>.8f&&color.b>.8f;t.life=t.electric?.12f:.095f;
            t.line.enabled=true;t.line.startWidth=t.electric?.033f:.025f;t.line.endWidth=t.electric?.017f:.007f;
            t.line.startColor=color;t.line.endColor=new Color(color.r,color.g,color.b,0);
            if(t.electric){
                Vector3 delta=b-a,right=Vector3.Cross(delta.normalized,Vector3.up).normalized;if(right.sqrMagnitude<.01f)right=Vector3.right;
                t.line.positionCount=9;
                for(int i=0;i<9;i++){float f=i/8f;Vector3 offset=(right*R(-1,1)+Vector3.up*R(-.7f,.7f))*Mathf.Sin(f*Mathf.PI)*Mathf.Min(.24f,delta.magnitude*.03f);t.line.SetPosition(i,Vector3.Lerp(a,b,f)+offset);}
            }else {t.line.positionCount=2;t.line.SetPosition(0,a);t.line.SetPosition(1,Vector3.Lerp(a,b,.35f));}
            Emitted++;
        }
        public void Muzzle(Vector3 point,Vector3 direction,WeaponKind weapon)
        {
            if(!root)return;
            bool arc=weapon==WeaponKind.ArcCaster,harpoon=weapon==WeaponKind.Harpoon,scatter=weapon==WeaponKind.Scattergun;
            float length=scatter?.65f:weapon==WeaponKind.Revolver?.44f:weapon==WeaponKind.BurstRifle?.48f:.32f;
            float width=scatter?.19f:weapon==WeaponKind.Revolver?.13f:.085f;
            Color amber=new Color(1,.63f,.24f,.7f),ivory=new Color(1,.94f,.7f,.9f),salt=new Color(.46f,.92f,.91f,.7f);
            if(arc){
                AccentAt(boltMesh,point,direction,new Vector3(.18f,.18f,.7f),salt,.09f,.1f,180);
                AccentAt(ringMesh,point+direction*.16f,direction,Vector3.one*.09f,new Color(.7f,1,1,.55f),.12f,1.3f,120);
            }else if(harpoon){
                AccentAt(ringMesh,point+direction*.07f,direction,Vector3.one*.09f,salt,.14f,1.7f);
                for(int i=0;i<5;i++)Emit(droplets,point,(direction+Noise()*.15f)*4,new Color(.8f,.93f,.89f,.7f),R(.035f,.07f),R(.12f,.23f));
            }else {
                AccentAt(flameMesh,point,direction,new Vector3(width,width,length),amber,.055f,.23f,R(-240,240));
                AccentAt(flameMesh,point+direction*.015f,direction,new Vector3(width*.46f,width*.46f,length*.58f),ivory,.035f,.1f);
                for(int i=0;i<(scatter?7:3);i++)Emit(sparks,point,(direction+Noise()*.2f)*R(4,8),ivory,R(.026f,.05f),R(.05f,.12f));
            }
            Emit(mist,point+direction*.1f,direction*.5f+Vector3.up*.35f,new Color(.7f,.78f,.75f,arc?.1f:.25f),scatter?.25f:.15f,harpoon?.24f:.42f);
        }
        public void Impact(Vector3 point,Vector3 normal,bool flesh,bool weak,bool killed)
        {
            if(!root)return;if(normal.sqrMagnitude<.01f)normal=Vector3.up;
            Color salt=new Color(.55f,.9f,.8f),gold=new Color(1,.75f,.28f);Color hot=weak?gold:flesh?salt:new Color(1,.68f,.39f);
            Vector3 p=point+normal*.025f;
            AccentAt(flameMesh,p,normal,new Vector3(weak?.19f:.12f,weak?.19f:.12f,.22f),new Color(hot.r,hot.g,hot.b,.75f),.08f,.8f);
            if(weak||killed)AccentAt(ringMesh,p,normal,Vector3.one*(killed?.27f:.19f),new Color(1,.84f,.51f,.74f),killed?.32f:.22f,killed?2:1.3f,55);
            int count=killed?16:weak?10:5;
            for(int i=0;i<count;i++)Emit(sparks,p,(normal+Noise()*.82f)*R(1.3f,killed?4.8f:3.4f),hot,R(.045f,weak?.1f:.075f),R(.14f,killed?.62f:.36f));
            Emit(mist,p,normal*.55f+Vector3.up*.2f,new Color(.55f,.7f,.66f,flesh?.26f:.4f),weak?.32f:.22f,.4f);
            if(killed){
                AccentAt(crownMesh,p,normal,new Vector3(.28f,.28f,.34f),new Color(.6f,.95f,.87f,.55f),.38f,1.3f,90);
                for(int i=0;i<5;i++)Emit(motes,p,Noise()*.75f+Vector3.up*.6f,new Color(1,.88f,.58f,.8f),R(.045f,.08f),R(.35f,.7f));
            }
        }
        public void Splash(Vector3 point)
        {
            if(!root)return;point.y=-.35f;Color foam=new Color(.74f,.95f,.89f,.68f);
            AccentAt(crownMesh,point,Vector3.up,new Vector3(.58f,.58f,.62f),foam,.48f,.9f,15);
            AccentAt(ringMesh,point+Vector3.up*.025f,Vector3.up,Vector3.one*.3f,new Color(.8f,.94f,.9f,.55f),.64f,3,12);
            for(int i=0;i<18;i++){
                float a=i*Mathf.PI*2/18;Vector3 velocity=new Vector3(Mathf.Cos(a),R(1.1f,2.6f),Mathf.Sin(a))*R(.9f,1.8f);
                Emit(droplets,point+new Vector3(velocity.x,0,velocity.z)*.15f,velocity,foam,R(.045f,.11f),R(.3f,.68f));
            }
            Emit(mist,point,Vector3.up*.35f,new Color(.71f,.91f,.88f,.22f),.65f,.55f);
        }
        public void Burst(Vector3 point,Color color,int count,float size)
        {
            if(!root)return;count=Mathf.Clamp(count,1,24);float scale=Mathf.Clamp(size*3,.15f,.7f);
            bool hostile=color.r>.82f&&color.g<.61f&&color.b<.45f;
            Color pigment=Color.Lerp(color,hostile?new Color(1,.61f,.23f):new Color(.75f,.93f,.84f),.2f);pigment.a=.65f;
            AccentAt(crownMesh,point,Vector3.up,new Vector3(scale,scale,scale*.8f),pigment,.3f,1.1f,hostile?-70:70);
            for(int i=0;i<count;i++)Emit(sparks,point,Noise()*R(.8f,2.8f)+Vector3.up*.8f,pigment,R(size*.45f,size*.9f),R(.18f,.5f));
            for(int i=0;i<Mathf.Min(3,count/6+1);i++)Emit(mist,point,Noise()*.5f+Vector3.up*.4f,new Color(pigment.r*.7f,pigment.g*.7f,pigment.b*.7f,.22f),scale*1.5f,R(.35f,.65f));
        }
        Mesh Make(string name,List<Vector3> vertices,List<int> indices,List<Color> colors)
        {
            var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.SetColors(colors);
            var uv=new Vector2[vertices.Count];for(int i=0;i<uv.Length;i++)uv[i]=new Vector2(.5f,.5f);mesh.uv=uv;
            mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);return mesh;
        }
        static void Quad(List<int> t,int i){t.Add(i);t.Add(i+1);t.Add(i+2);t.Add(i);t.Add(i+2);t.Add(i+3);}
        Mesh Flame()
        {
            var v=new List<Vector3>();var t=new List<int>();var c=new List<Color>();
            // Three curved uneven leaves: the flash has a silhouette, not a cone or rectangle.
            for(int blade=0;blade<3;blade++)for(int j=0;j<4;j++){
                float a=blade*Mathf.PI/3;Vector3 side=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);
                float z0=j/4f,z1=(j+1)/4f,w0=Mathf.Sin((z0*.88f+.12f)*Mathf.PI)*(1-z0*.45f),w1=j==3?0:Mathf.Sin((z1*.88f+.12f)*Mathf.PI)*(1-z1*.45f);
                int k=v.Count;float bend0=Mathf.Sin(z0*Mathf.PI)*.17f,bend1=Mathf.Sin(z1*Mathf.PI)*.17f;
                v.Add(side*(-w0+bend0)+Vector3.forward*(z0*(1-blade*.08f)));v.Add(side*(w0+bend0)+Vector3.forward*(z0*(1-blade*.08f)));
                v.Add(side*(w1+bend1)+Vector3.forward*(z1*(1-blade*.08f)));v.Add(side*(-w1+bend1)+Vector3.forward*(z1*(1-blade*.08f)));
                c.Add(new Color(1,1,1,1-z0*.6f));c.Add(new Color(1,1,1,1-z0*.6f));c.Add(new Color(1,.77f,.46f,1-z1*.9f));c.Add(new Color(1,.77f,.46f,1-z1*.9f));Quad(t,k);
            }
            return Make("Three leaf powder flame",v,t,c);
        }
        Mesh Ring()
        {
            var v=new List<Vector3>();var t=new List<int>();var c=new List<Color>();
            for(int s=0;s<48;s++){
                if(s%12>9)continue;float a=s*Mathf.PI*2/48,b=(s+1)*Mathf.PI*2/48;
                float inner=.82f+.035f*Mathf.Sin(s*1.7f);int k=v.Count;
                v.Add(new Vector3(Mathf.Cos(a),Mathf.Sin(a),0)*inner);v.Add(new Vector3(Mathf.Cos(a),Mathf.Sin(a),0));
                v.Add(new Vector3(Mathf.Cos(b),Mathf.Sin(b),0));v.Add(new Vector3(Mathf.Cos(b),Mathf.Sin(b),0)*inner);
                c.Add(new Color(1,1,1,0));c.Add(Color.white);c.Add(Color.white);c.Add(new Color(1,1,1,0));Quad(t,k);
            }
            return Make("Broken salt pressure halo",v,t,c);
        }
        Mesh Crown()
        {
            var v=new List<Vector3>();var t=new List<int>();var c=new List<Color>();
            for(int j=0;j<12;j++){
                float a=j*Mathf.PI/6,b=a+.2f,h=.55f+.4f*Mathf.Sin(j*2.3f);int k=v.Count;
                v.Add(new Vector3(Mathf.Cos(a)*.38f,Mathf.Sin(a)*.38f,0));
                v.Add(new Vector3(Mathf.Cos(b)*.53f,Mathf.Sin(b)*.53f,.03f));
                v.Add(new Vector3(Mathf.Cos(b+.05f)*.98f,Mathf.Sin(b+.05f)*.98f,h));
                v.Add(new Vector3(Mathf.Cos(a+.07f)*.88f,Mathf.Sin(a+.07f)*.88f,h*.83f));
                c.Add(new Color(1,1,1,.25f));c.Add(new Color(1,1,1,.3f));c.Add(new Color(1,1,1,.05f));c.Add(Color.white);Quad(t,k);
            }
            return Make("Twelve curved sea spray leaves",v,t,c);
        }
        Mesh Shard()
        {
            var v=new List<Vector3>{new Vector3(0,0,.9f),new Vector3(-.25f,-.13f,0),new Vector3(.25f,-.13f,0),new Vector3(0,.22f,.05f),new Vector3(0,0,-.65f)};
            var t=new List<int>{0,1,2,0,2,3,0,3,1,4,2,1,4,3,2,4,1,3};
            var c=new List<Color>{Color.white,new Color(.55f,.75f,.82f),Color.white,new Color(.74f,.92f,1),new Color(.45f,.62f,.74f)};
            return Make("Faceted tapered salt sliver",v,t,c);
        }
        Mesh Bolt()
        {
            var v=new List<Vector3>();var t=new List<int>();var c=new List<Color>();
            for(int branch=0;branch<3;branch++)for(int j=0;j<5;j++){
                float a=branch*Mathf.PI*2/3;Vector3 side=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);
                Vector3 p=side*(j==0?0:Mathf.Sin(j*2.4f+branch)*.6f)+Vector3.forward*j*.2f;
                Vector3 q=side*Mathf.Sin((j+1)*2.4f+branch)*.6f+Vector3.forward*(j+1)*.2f;
                Vector3 w=side*(.11f-j*.014f);int k=v.Count;v.Add(p-w);v.Add(p+w);v.Add(q+w);v.Add(q-w);
                for(int n=0;n<4;n++)c.Add(new Color(1,1,1,1-j*.13f));Quad(t,k);
            }
            return Make("Three fork capacitor discharge",v,t,c);
        }
        public void Clear()
        {
            if(sparks)sparks.Clear();if(mist)mist.Clear();if(droplets)droplets.Clear();if(motes)motes.Clear();
            foreach(var a in accents)if(a!=null)a.renderer.enabled=false;
            foreach(var t in traces)if(t!=null)t.line.enabled=false;
        }
        void OnDestroy()
        {
            if(particleMaterial)Destroy(particleMaterial);if(mistMaterial)Destroy(mistMaterial);if(geometryMaterial)Destroy(geometryMaterial);
            if(sparkSprite)Destroy(sparkSprite);if(mistSprite)Destroy(mistSprite);foreach(var mesh in meshes)if(mesh)Destroy(mesh);
        }
    }
}

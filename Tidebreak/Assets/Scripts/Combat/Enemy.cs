using UnityEngine;

namespace Tidebreak
{
    public class Enemy : MonoBehaviour
    {
        public GameDirector game;
        public CreatureKind kind;
        public bool elite,dead,lastWeak;
        public int quality;
        public float weight,health,maxHealth,exposedUntil;
        public int phase=1;
        public bool IsBoss {get{return kind>=CreatureKind.Crab;}}
        public bool Exposed {get{return !IsBoss||Time.time<exposedUntil;}}
        public bool Airborne {get{return !IsBoss&&transform.position.y-game.World.GroundAt(transform.position)>.95f;}}
        Transform rig,tail;
        Vector3 origin;
        Rigidbody body;
        float age,nextAttack,flash,stagger,hopAt,launchUntil,attackPose;
        Renderer[] renderers;
        MaterialPropertyBlock properties;
        public void Init(GameDirector director,CreatureKind type,bool isElite,Vector3 position)
        {
            game=director;kind=type;elite=isElite;origin=position;transform.position=position;
            double rarity=game.Rng.NextDouble();quality=IsBoss?0:rarity<.045?2:rarity<.16?1:0;
            weight=(float)(game.Rng.NextDouble()*2.2+1.3)*(elite?1.7f:1)*(quality==1?2.1f:1);
            maxHealth=Balance.Health(kind,game.Run.stage,elite)*(game.Run.route==RouteKind.Hunt?1.12f:1)*(quality==1?1.25f:1);health=maxHealth;
            rig=CreatureArt.Build(transform,kind,elite);if(elite)rig.localScale=Vector3.one*1.15f;if(quality==1)rig.localScale*=1.35f;
            if(kind==CreatureKind.Crab)rig.localScale=Vector3.one*1.65f;
            foreach(var t in rig.GetComponentsInChildren<Transform>())if(t.name=="Tail fin")tail=t;
            renderers=GetComponentsInChildren<Renderer>();properties=new MaterialPropertyBlock();
            if(!IsBoss){body=gameObject.AddComponent<Rigidbody>();body.mass=2.4f;body.drag=.12f;body.angularDrag=3;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;body.constraints=RigidbodyConstraints.FreezeRotation;}
            nextAttack=Time.time+(IsBoss?3:2.8f);hopAt=Time.time+2;game.Enemies.Add(this);SetTint(false);
        }
        public void LaunchToward(Vector3 point)
        {
            if(IsBoss)return;float time=1.3f;point.y=game.World.GroundAt(point)+.6f;
            if(point.y<0){point=game.Player.transform.position+game.Player.transform.forward*1.1f;point.y=game.World.GroundAt(point)+.6f;}
            body.velocity=(point-transform.position-Physics.gravity*time*time*.5f)/time;launchUntil=Time.time+time;game.Splash(transform.position);
        }
        void Update()
        {
            if(dead||!game||game.State!=VoyageState.Combat||game.Paused)return;age+=Time.deltaTime;
            Vector3 to=game.Player.transform.position-transform.position;to.y=0;
            if(to.sqrMagnitude>.01f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(to),Time.deltaTime*4);
            if(IsBoss) {
                transform.position=origin+new Vector3(Mathf.Sin(age*.3f)*3,Mathf.Sin(age*.9f)*.17f-Mathf.Max(0,1-age/1.4f)*4,Mathf.Cos(age*.4f)*1.2f);
                float windup=Mathf.Clamp01(1-(nextAttack-Time.time)/1.1f);
                rig.localEulerAngles=new Vector3(windup*-13+Mathf.Sin(age*2)*2,0,Mathf.Sin(age)*2);
                if(kind==CreatureKind.Kraken)for(int i=0;i<rig.childCount;i++){var arm=rig.GetChild(i);if(arm.name.StartsWith("Tentacle")){int index=int.Parse(arm.name.Substring(9));arm.localRotation=Quaternion.Euler(Mathf.Sin(age*1.4f+index)*8-windup*20,index*45,Mathf.Sin(age*.9f+index)*8);}}
                if(phase==1&&health<=maxHealth*.5f){phase=2;nextAttack=Time.time+2;game.Notice("首领进入狂暴 · 连续攻击后再反击",3);game.Audio.Cue("boss");}
            } else {
                if(Time.time>launchUntil&&Time.time>hopAt&&!Airborne) {
                    Vector3 dir=to.normalized;body.velocity=dir*(kind==CreatureKind.Razorfin?4.7f:2.8f)+Vector3.up*(kind==CreatureKind.Puffer?3:4.2f);hopAt=Time.time+(elite?.9f:1.35f);
                    if(to.magnitude<1.7f){game.Player.TakeDamage(elite?12:7);game.Notice("怪鱼扑咬 · 后撤或冲刺拉开距离",1.3f);}
                }
                rig.localRotation=Quaternion.Euler(Airborne?-15:Mathf.Sin(age*13)*9,0,Mathf.Sin(age*(Airborne?8:13))*12);
                if(transform.position.y<-1.6f){var p=game.Player.transform.position+game.Player.transform.forward*2;p.y=game.World.GroundAt(p)+1;transform.position=p;body.velocity=Vector3.up*3;game.Splash(p);}
            }
            if(tail)tail.localRotation=Quaternion.Euler(0,Mathf.Sin(age*12)*22,0);
            if(Time.time>=nextAttack)Attack();
            if(flash>0){flash-=Time.deltaTime;if(flash<=0)SetTint(false);}
        }
        void SetTint(bool hit)
        {properties.Clear();if(hit)properties.SetColor("_Color",new Color(1.5f,1.3f,1.1f));else if(quality==2)properties.SetColor("_Color",new Color(1.6f,1.28f,.32f));foreach(var r in renderers)if(r)r.SetPropertyBlock(properties);}
        void Attack()
        {
            var p=game.Player.transform.position;float damage=IsBoss?16+game.Run.Act*3:elite?12:8+game.Run.Act*2;float delay=game.Run.easy?1.8f:1.35f;
            if(kind==CreatureKind.Kraken) {
                game.Notice(phase==1?"克拉肯抬起触腕 · 离开落点":"深渊狂潮 · 三次连续触腕重击",2);
                game.Warn(p,2.6f,delay,damage);game.Warn(p+game.Player.transform.right*3,2.3f,delay+.7f,damage);
                if(phase==2)game.Warn(p-game.Player.transform.right*3,2.3f,delay+1.4f,damage);
                for(int i=-1;i<=1;i++)game.Projectile(transform.position+Vector3.up,game.Player.transform.position+game.Player.transform.right*i*2,9,10,new Color(.53f,.25f,.56f));
            } else if(kind==CreatureKind.WhiteWhale) {
                game.Notice("霜潮齐射 · 横移避弹，再离开冰爆落点",2);
                for(int i=-2;i<=2;i++)game.Projectile(transform.position+Vector3.up,p+game.Player.transform.right*i*2,phase==1?9:12,damage*.65f,new Color(.62f,.92f,1));
                game.Warn(p,2.3f,delay+.6f,damage);if(phase==2){game.Warn(p+Vector3.right*4,2,delay+1,damage);game.Warn(p-Vector3.right*4,2,delay+1,damage);}
            } else if(kind==CreatureKind.Crab) {
                game.Notice("铁钳重击 · 躲开落点，随后攻击腹部弱点",2);game.Warn(p,2.2f,delay,damage);
                if(phase==2)game.Warn(p+game.Player.transform.forward*2.8f,2,delay+.65f,damage);
                game.Projectile(transform.position+Vector3.up,p,10,10,new Color(.78f,.52f,.22f));
            } else if(kind==CreatureKind.Angler) {
                game.Notice("灯笼蓄光 · 横向躲避扇形弹幕",2);
                for(int i=-2;i<=2;i++)game.Projectile(transform.position,p+game.Player.transform.right*i*1.6f,10,damage*.7f,new Color(.35f,.91f,.59f));if(phase==2)game.Warn(p,2,delay,damage);
            } else if(kind==CreatureKind.Leviathan) {
                game.Notice("风暴汇聚 · 冲出水柱落点",2);game.Warn(p,2.8f,delay,damage);
                for(int i=-1;i<=1;i++)game.Projectile(transform.position,p+game.Player.transform.right*i*2,12,damage*.7f,new Color(.37f,.73f,.9f));
                if(phase==2)game.Warn(p-game.Player.transform.forward*3,2.5f,delay+.8f,damage);
            } else if(kind==CreatureKind.Puffer)game.Warn(p,elite?1.7f:1.2f,delay+.2f,damage);
            exposedUntil=Time.time+delay+1.65f;nextAttack=Time.time+(IsBoss?(phase==2?4.2f:5.4f):elite?4:5.6f);
        }
        public void Hit(float damage,bool weak=false,bool critical=false)
        {
            if(dead||game.State!=VoyageState.Combat)return;lastWeak=weak;float mult=IsBoss&&!Exposed?.7f:1;if(weak)mult*=1.65f;health-=damage*mult;
            game.UI.HitMarker(critical||weak,damage*mult);flash=.09f;SetTint(true);game.Effect(transform.position,new Color(.88f,.64f,.36f),4,.065f);
            if(body)body.AddForce(game.Player.View.transform.forward*.65f+Vector3.up*.25f,ForceMode.Impulse);
            if(weak&&IsBoss){stagger+=damage;if(stagger>maxHealth*.14f){stagger=0;exposedUntil=Time.time+3.2f;nextAttack=Time.time+3.2f;game.Notice("弱点击破！首领失衡 3 秒",2);}}
            if(health<=0){dead=true;game.EnemyKilled(this);game.Effect(transform.position,new Color(.6f,.81f,.71f),15,.1f);Destroy(gameObject);}
        }
    }
    public class SeaProjectile : MonoBehaviour
    {
        public GameDirector game;
        public Vector3 velocity;
        public float damage;
        float life=8;
        void Update() {
            if(game.Paused)return;
            if(game.State!=VoyageState.Combat){Destroy(gameObject);return;}
            Vector3 prev=transform.position; transform.position+=velocity*Time.deltaTime;
            if(Physics.Linecast(prev,transform.position,SeaWorld.GroundMask)){game.Effect(transform.position,new Color(.63f,.75f,.7f),5,.05f);Destroy(gameObject);return;}
            Vector3 player=game.Player.transform.position;
            Vector3 delta=transform.position-prev;
            float f=delta.sqrMagnitude>0?Mathf.Clamp01(Vector3.Dot(player-prev,delta)/delta.sqrMagnitude):0;
            if(Vector3.Distance(player,prev+delta*f)<.75f){game.Player.TakeDamage(damage);Destroy(gameObject);return;}
            life-=Time.deltaTime;if(life<0 || transform.position.y<-.5f)Destroy(gameObject);
        }
    }
    public class DeckWarning : MonoBehaviour
    {
        public GameDirector game;
        public float radius, delay, damage;
        float age;
        public float Remaining { get { return Mathf.Max(0,delay-age); } }
        Transform disk;
        Transform arm;
        public void Init() {
            disk=Shape.Part("Danger zone",PrimitiveType.Cylinder,transform,Vector3.zero,new Vector3(radius*2,.015f,radius*2),new Color(.67f,.12f,.14f),false,true).transform;
            var ring=gameObject.AddComponent<LineRenderer>();ring.useWorldSpace=false;ring.loop=true;ring.positionCount=64;
            ring.startWidth=ring.endWidth=.07f;ring.material=Shape.Mat(new Color(1,.59f,.3f),true);
            for(int i=0;i<64;i++){float a=i*Mathf.PI*2/64;ring.SetPosition(i,new Vector3(Mathf.Cos(a)*radius,.035f,Mathf.Sin(a)*radius));}
            foreach(var e in game.Enemies)if(e&&e.kind==CreatureKind.Kraken) {
                arm=CoastalMesh.Tube("Descending Kraken arm",transform,new[]{new Vector3(0,-1,-1),new Vector3(0,2,-.7f),new Vector3(0,4,.2f),new Vector3(0,5,1)},new[]{.72f,.65f,.42f,.04f},new Color(.35f,.2f,.4f),new Color(.62f,.39f,.45f),9);
                arm.localScale=Vector3.zero;break;
            }
        }
        void Update() {
            if(game.Paused)return;
            if(game.State!=VoyageState.Combat){Destroy(gameObject);return;}
            age+=Time.deltaTime;disk.localScale=new Vector3(radius*2*Mathf.Lerp(.1f,1,age/delay),.015f,radius*2*Mathf.Lerp(.1f,1,age/delay));
            if(arm){arm.localScale=Vector3.one*Mathf.Clamp01(age/delay*2);arm.localRotation=Quaternion.Euler(Mathf.Lerp(-45,60,Mathf.Pow(age/delay,5)),0,0);}
            if(age<delay)return;
            Vector3 p=game.Player.transform.position-transform.position;p.y=0;
            if(p.magnitude<radius)game.Player.TakeDamage(damage);
            game.Effect(transform.position+Vector3.up*.4f,new Color(1,.45f,.22f),18,.23f);game.Audio.Cue("splash");Destroy(gameObject);
        }
    }
    public class Fleck : MonoBehaviour
    {
        public Vector3 velocity;
        public float life=.5f;
        float initial;
        Vector3 scale;
        void Start(){initial=life;scale=transform.localScale;}
        void Update(){life-=Time.deltaTime;transform.position+=velocity*Time.deltaTime;velocity+=Vector3.down*6*Time.deltaTime;transform.localScale=scale*Mathf.Max(0,life/initial);if(life<=0)Destroy(gameObject);}
    }
    public class SurfaceRipple : MonoBehaviour
    {
        LineRenderer ring;float age;
        void Start(){ring=gameObject.AddComponent<LineRenderer>();ring.useWorldSpace=false;ring.loop=true;ring.positionCount=48;ring.material=Shape.Mat(new Color(.58f,.77f,.73f));}
        void Update(){age+=Time.deltaTime;float r=.12f+age*2.2f;ring.startWidth=ring.endWidth=.065f*(1-age/1.2f);for(int i=0;i<48;i++){float a=i*Mathf.PI*2/48;ring.SetPosition(i,new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r));}if(age>=1.2f)Destroy(gameObject);}
    }
}

using UnityEngine;

namespace Tidebreak
{
    public class Enemy : MonoBehaviour
    {
        public GameDirector game;
        public CreatureKind kind;
        public bool elite, dead;
        public float health, maxHealth, exposedUntil;
        public int phase=1;
        public bool IsBoss { get { return kind>=CreatureKind.Crab; } }
        public bool Exposed { get { return !IsBoss || Time.time<exposedUntil; } }
        Transform rig;
        Vector3 origin;
        float age, nextAttack, flash, stagger;
        Renderer[] renderers;
        MaterialPropertyBlock properties;
        public void Init(GameDirector director,CreatureKind type,bool isElite,Vector3 position)
        {
            game=director; kind=type; elite=isElite; origin=position; transform.position=position;
            maxHealth=Balance.Health(kind,game.Run.stage,elite)*(game.Run.route==RouteKind.Hunt?1.12f:1); health=maxHealth;
            rig=CreatureArt.Build(transform,kind,elite);
            if(elite)rig.localScale=Vector3.one*1.22f;
            if(kind==CreatureKind.Kraken)rig.localScale=Vector3.one*1.3f;
            renderers=GetComponentsInChildren<Renderer>(); properties=new MaterialPropertyBlock();
            nextAttack=Time.time+(IsBoss?3:2.5f)+Random.value;
            game.Enemies.Add(this);
        }
        void Update()
        {
            if(dead || !game || game.State!=VoyageState.Combat || game.Paused)return;
            age+=Time.deltaTime;
            float speed=IsBoss?.36f:.9f;
            float width=IsBoss?2.7f:3;
            transform.position=origin+new Vector3(Mathf.Sin(age*speed+origin.x)*width,Mathf.Sin(age*1.7f+origin.x)*.5f,Mathf.Cos(age*.7f)*.65f);
            transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(game.Player.transform.position+Vector3.up*.2f-transform.position),Time.deltaTime*3);
            rig.localEulerAngles=new Vector3(Mathf.Sin(age*2)*4,0,Mathf.Sin(age*3)*3);
            if(kind==CreatureKind.Kraken)for(int i=0;i<rig.childCount;i++) {
                var arm=rig.GetChild(i); if(arm.name.StartsWith("Tentacle")) arm.localRotation=Quaternion.Euler(Mathf.Sin(age*2+i)*12,(i-6)*45,Mathf.Sin(age*1.3f+i)*9);
            }
            if(IsBoss && phase==1 && health<=maxHealth*.5f) {
                phase=2; nextAttack=Time.time+1.5f;
                game.Notice("狂暴阶段 · 注意甲板预警！",3); game.Audio.Cue("boss");
            }
            if(Time.time>=nextAttack)Attack();
            if(flash>0) { flash-=Time.deltaTime; if(flash<=0)foreach(var r in renderers)if(r)r.SetPropertyBlock(null); }
        }
        void Attack()
        {
            var p=game.Player.transform.position; p.y=.23f;
            float damage=IsBoss?17+game.Run.Act*3:elite?13:8+game.Run.Act*2;
            float delay=game.Run.easy?1.65f:1.3f;
            if(kind==CreatureKind.Kraken) {
                game.Notice(phase==1?"触腕重击 · 离开红圈":"深渊狂潮 · 连续冲刺躲避",1.8f);
                game.Warn(p,2.1f,delay,damage);
                game.Warn(new Vector3(Mathf.Clamp(p.x+3,-5,5),.23f,Mathf.Clamp(p.z+2,-3,5)),1.9f,delay+.45f,damage);
                if(phase==2)game.Warn(new Vector3(Mathf.Clamp(p.x-3,-5,5),.23f,Mathf.Clamp(p.z-2,-3,5)),1.9f,delay+.8f,damage);
                for(int i=-1;i<=1;i++)game.Projectile(transform.position+Vector3.up,game.Player.transform.position+new Vector3(i*2,0,0),8,10,new Color(.72f,.35f,1));
            } else if(kind==CreatureKind.WhiteWhale) {
                game.Notice("霜潮齐射 · 横移后冲刺避开冰爆",1.8f);
                for(int i=-2;i<=2;i++)game.Projectile(transform.position+Vector3.up,game.Player.transform.position+new Vector3(i*2,0,0),phase==1?8:12,damage*.65f,new Color(.62f,1,1));
                game.Warn(p,2.1f,delay+.45f,damage);
                if(phase==2){game.Warn(new Vector3(-4,.23f,p.z),1.7f,delay+.8f,damage);game.Warn(new Vector3(4,.23f,p.z),1.7f,delay+.8f,damage);}
            } else if(kind==CreatureKind.Crab) {
                game.Notice("铁钳砸击 · 离开红圈后反击弱点",1.8f);
                game.Warn(p,2.2f,delay,damage);
                if(phase==2)game.Warn(new Vector3(-p.x,.23f,p.z+2),1.9f,delay+.35f,damage);
            } else if(kind==CreatureKind.Angler) {
                game.Notice("荧光弹幕 · 横向移动躲避",1.7f);
                for(int i=-2;i<=2;i++)game.Projectile(transform.position,game.Player.transform.position+new Vector3(i*1.45f,0,0),9,damage*.75f,new Color(.3f,1,.7f));
                if(phase==2)game.Warn(p,1.8f,delay,damage);
            } else if(kind==CreatureKind.Leviathan) {
                game.Notice("风暴轰击 · 冲刺穿过危险区域",1.8f);
                game.Warn(p,2.5f,delay,damage);
                for(int i=-1;i<=1;i++)game.Projectile(transform.position,game.Player.transform.position+new Vector3(i*2,0,0),11,damage*.7f,new Color(.4f,.85f,1));
            } else {
                if(kind==CreatureKind.Puffer)game.Warn(p,elite?1.9f:1.4f,delay+.2f,damage);
                else game.Projectile(transform.position,game.Player.transform.position,elite?10:8,damage,new Color(1,.55f,.24f));
            }
            exposedUntil=Time.time+delay+1.5f;
            nextAttack=Time.time+(IsBoss?(phase==2?3.6f:4.7f):elite?3.5f:4.8f)+Random.value*.5f;
        }
        public void Hit(float damage,bool weak=false,bool critical=false)
        {
            if(dead || game.State!=VoyageState.Combat)return;
            float mult=IsBoss&&!Exposed?.6f:1;
            if(weak)mult*=1.6f;
            health-=damage*mult;
            game.UI.HitMarker(critical||weak,damage*mult);
            flash=.09f; properties.SetColor("_Color",Color.white); foreach(var r in renderers)if(r)r.SetPropertyBlock(properties);
            game.Effect(transform.position,new Color(1,.83f,.4f),5,.12f);
            if(weak && IsBoss) {
                stagger+=damage; if(stagger>maxHealth*.15f){stagger=0;exposedUntil=Time.time+3.5f;nextAttack=Time.time+3.5f;game.Notice("弱点击破 · BOSS 失衡！",1.6f);}
            }
            if(health<=0) { dead=true; game.EnemyKilled(this); game.Effect(transform.position,elite?new Color(.76f,.39f,1):new Color(.3f,.95f,.82f),24,.25f); Destroy(gameObject); }
        }
    }
    public class SeaProjectile : MonoBehaviour
    {
        public GameDirector game;
        public Vector3 velocity;
        public float damage;
        float life=8;
        void Update() {
            if(game.Paused || game.State!=VoyageState.Combat)return;
            Vector3 prev=transform.position; transform.position+=velocity*Time.deltaTime;
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
        public void Init() {
            disk=Shape.Part("Danger zone",PrimitiveType.Cylinder,transform,Vector3.zero,new Vector3(radius*2,.015f,radius*2),new Color(.67f,.12f,.14f),false,true).transform;
            var ring=gameObject.AddComponent<LineRenderer>();ring.useWorldSpace=false;ring.loop=true;ring.positionCount=64;
            ring.startWidth=ring.endWidth=.07f;ring.material=Shape.Mat(new Color(1,.59f,.3f),true);
            for(int i=0;i<64;i++){float a=i*Mathf.PI*2/64;ring.SetPosition(i,new Vector3(Mathf.Cos(a)*radius,.035f,Mathf.Sin(a)*radius));}
        }
        void Update() {
            if(game.Paused || game.State!=VoyageState.Combat)return;
            age+=Time.deltaTime;disk.localScale=new Vector3(radius*2*Mathf.Lerp(.1f,1,age/delay),.015f,radius*2*Mathf.Lerp(.1f,1,age/delay));
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
}

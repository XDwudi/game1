using UnityEngine;
namespace Tidebreak
{
    public class ThreatField : MonoBehaviour
    {
        GameDirector game;int type;Vector3 end;float radius,delay,damage,age,nextTick;Color color;LineRenderer line;bool hit;
        static ThreatField Create(GameDirector g,int type,Vector3 p,Vector3 end,float radius,float delay,float damage,Color color)
        {
            var obj=new GameObject("Telegraph "+type);obj.transform.SetParent(g.Hazards);obj.transform.position=p;var f=obj.AddComponent<ThreatField>();f.game=g;f.type=type;f.end=end;f.radius=radius;f.delay=delay;f.damage=damage;f.color=color;
            f.line=obj.AddComponent<LineRenderer>();f.line.material=Shape.Mat(color,true);f.line.startWidth=f.line.endWidth=.055f;f.line.useWorldSpace=true;f.line.positionCount=type==0?2:48;f.line.loop=type!=0;return f;
        }
        public static void Line(GameDirector g,Vector3 from,Vector3 to,float width,float delay,float damage,Color c){Create(g,0,from,to,width,delay,damage,c);}
        public static void Ring(GameDirector g,Vector3 center,float damage,Color c){Create(g,1,center,center,1,.9f,damage,c);}
        public static void Pool(GameDirector g,Vector3 center,float radius,float delay,float damage,Color c){Create(g,2,center,center,radius,delay,damage,c);}
        public static void Vortex(GameDirector g,Vector3 center,float radius,float delay,float damage,Color c){Create(g,3,center,center,radius,delay,damage,c);}
        void Update()
        {
            if(game.Paused)return;if(game.State!=VoyageState.Combat){Destroy(gameObject);return;}age+=Time.deltaTime;
            var player=game.Player;Vector3 p=player.transform.position;
            if(type==0){line.SetPosition(0,transform.position);line.SetPosition(1,end);line.startWidth=line.endWidth=age<delay?.05f:radius*.4f;
                if(age>=delay&&!hit){hit=true;Vector3 d=end-transform.position;float t=Mathf.Clamp01(Vector3.Dot(p-transform.position,d)/d.sqrMagnitude);if(Vector3.Distance(p,transform.position+d*t)<radius)player.TakeDamage(damage);game.Audio.Cue("beam");}if(age>delay+.22f)Destroy(gameObject);return;}
            float r=type==1?Mathf.Max(.2f,(age-delay)*6):radius;
            for(int i=0;i<48;i++){float a=i*Mathf.PI*2/48+ (type==3?age*1.5f:0);Vector3 at=transform.position+new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r);at.y=game.World.GroundAt(at)+.09f;line.SetPosition(i,at);}
            float distance=GameDirector.FlatDistance(p,transform.position);
            if(age<delay)return;
            line.startWidth=line.endWidth=type==1?.2f:.11f;
            if(type==1){float feet=p.y-1.6f;if(!hit&&Mathf.Abs(distance-r)<.6f&&feet<game.World.GroundAt(p)+.5f){hit=true;player.TakeDamage(damage);}if(r>40)Destroy(gameObject);}
            else {if(distance<radius){if(type==3&&player.Motor.enabled){Vector3 toward=transform.position-p;toward.y=0;player.Motor.Move(toward.normalized*Time.deltaTime*2.5f);}if(Time.time>nextTick){nextTick=Time.time+1;float before=game.Run.health;player.TakeDamage(damage);if(type==2&&game.Run.health<before)player.ApplyStatus(SeaTrait.Venom);}}
                if(age>delay+4.5f)Destroy(gameObject);}
        }
    }
    public class ThrownUtility : MonoBehaviour
    {
        GameDirector game;bool frost;float age;
        public static void Create(GameDirector game,bool frost)
        {
            var obj=Shape.Part(frost?"Ice bottle":"Depth charge",PrimitiveType.Sphere,game.Hazards,game.Player.View.transform.position+game.Player.View.transform.forward*.6f,Vector3.one*.18f,frost?Color.cyan:new Color(.45f,.38f,.2f),true);
            var body=obj.AddComponent<Rigidbody>();body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;body.velocity=game.Player.View.transform.forward*11+Vector3.up*2;
            var u=obj.AddComponent<ThrownUtility>();u.game=game;u.frost=frost;game.Audio.Cue("cast");
        }
        void Update(){if(game.Paused)return;age+=Time.deltaTime;if(age<1.2f)return;foreach(var e in game.Enemies.ToArray())if(e&&Vector3.Distance(e.transform.position,transform.position)<6){if(frost){e.Slow(5);e.Stun(.8f);e.Hit(12);}else {e.Hit(85+game.Run.weaponLevel*12);e.Stun(1);}}game.Effect(transform.position,frost?Color.cyan:new Color(1,.57f,.2f),24,.18f);game.Audio.Cue(frost?"freeze":"explosion");Destroy(gameObject);}
    }
    public class BrassCasing : MonoBehaviour
    {
        float life=1.7f;void Update(){life-=Time.deltaTime;if(life<=0)Destroy(gameObject);}
    }
}

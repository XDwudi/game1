using UnityEngine;
namespace Tidebreak
{
    public class ThreatField : MonoBehaviour
    {
        GameDirector game;int type;Vector3 end;float radius,delay,damage,age,nextTick;Color color;LineRenderer line,border;bool hit;SeaTrait status;
        public int Kind {get{return type;}}
        public float Remaining {get{return Mathf.Max(0,delay-age);}}
        public Vector3 End {get{return end;}}
        public float Radius {get{return radius;}}
        public bool Threatens(Vector3 p,out string response)
        {
            response=type==1?"潮环逼近 · SPACE 跳跃":type==3?"漩涡牵引 · 向外冲刺":type==2?"危险地面 · 离开光圈":"锁定直线 · 横向离开宽带";
            if(type==0){Vector3 d=end-transform.position;d.y=0;Vector3 q=p-transform.position;q.y=0;float t=d.sqrMagnitude>.01f?Mathf.Clamp01(Vector3.Dot(q,d)/d.sqrMagnitude):0;return !hit&&(q-d*t).magnitude<radius+.25f;}
            float distance=GameDirector.FlatDistance(p,transform.position);
            if(type==1){float r=Mathf.Max(.2f,(age-delay)*6);return !hit&&distance>r-.6f&&distance<r+4.5f;}
            return distance<radius;
        }
        static ThreatField Create(GameDirector g,int type,Vector3 p,Vector3 end,float radius,float delay,float damage,Color color)
        {
            var obj=new GameObject("Telegraph "+type);obj.transform.SetParent(g.Hazards);obj.transform.position=p;var f=obj.AddComponent<ThreatField>();f.game=g;f.type=type;f.end=end;f.radius=radius;f.delay=delay;f.damage=damage;f.color=color;
            f.line=obj.AddComponent<LineRenderer>();f.line.material=Shape.Mat(color,true);f.line.startWidth=f.line.endWidth=.055f;f.line.useWorldSpace=true;f.line.positionCount=type==0?2:48;f.line.loop=type!=0;
            if(type==0){var edges=new GameObject("Strike corridor boundary");edges.transform.SetParent(obj.transform,false);f.border=edges.AddComponent<LineRenderer>();f.border.material=Shape.Mat(new Color(1,.63f,.26f),true);f.border.startWidth=f.border.endWidth=.09f;f.border.useWorldSpace=true;f.border.loop=true;f.DrawCorridor();}
            return f;
        }
        static Vector3 OnSurface(SeaWorld world,Vector3 p){p.y=Mathf.Max(-.36f,world.GroundAt(p))+.13f;return p;}
        public static void DrawSurfaceLine(LineRenderer target,SeaWorld world,Vector3 from,Vector3 to)
        {
            const int segments=32;target.positionCount=segments+1;
            for(int i=0;i<=segments;i++)target.SetPosition(i,OnSurface(world,Vector3.Lerp(from,to,(float)i/segments)));
        }
        void DrawCorridor()
        {
            DrawSurfaceLine(line,game.World,transform.position,end);const int segments=32;border.positionCount=(segments+1)*2;
            Vector3 axis=end-transform.position;axis.y=0;Vector3 side=Vector3.Cross(axis.normalized,Vector3.up)*radius;
            for(int i=0;i<=segments;i++){float t=(float)i/segments;border.SetPosition(i,OnSurface(game.World,Vector3.Lerp(transform.position,end,t)+side));border.SetPosition(i+segments+1,OnSurface(game.World,Vector3.Lerp(end,transform.position,t)-side));}
        }
        public static void Line(GameDirector g,Vector3 from,Vector3 to,float width,float delay,float damage,Color c){Create(g,0,from,to,width,delay,damage,c);}
        public static void Ring(GameDirector g,Vector3 center,float damage,Color c){Create(g,1,center,center,1,.9f,damage,c);}
        public static void RingDelayed(GameDirector g,Vector3 center,float damage,Color c,float delay){Create(g,1,center,center,1,delay,damage,c);}
        public static void Pool(GameDirector g,Vector3 center,float radius,float delay,float damage,Color c,SeaTrait status=SeaTrait.None){Create(g,2,center,center,radius,delay,damage,c).status=status;}
        public static void Vortex(GameDirector g,Vector3 center,float radius,float delay,float damage,Color c){Create(g,3,center,center,radius,delay,damage,c);}
        void Update()
        {
            if(game.Paused)return;if(game.State!=VoyageState.Combat){Destroy(gameObject);return;}age+=Time.deltaTime;
            var player=game.Player;Vector3 p=player.transform.position;
            if(type==0){Vector3 a=transform.position,b=end;a.y=game.World.GroundAt(a)+.13f;b.y=game.World.GroundAt(b)+.13f;
                line.startWidth=line.endWidth=age<delay?Mathf.Lerp(.04f,radius*.35f,Mathf.Clamp01(age/delay)):radius*.85f;
                Vector3 axis=b-a;axis.y=0;
                if(age>=delay&&!hit){Vector3 flat=p-a;flat.y=0;float t=axis.sqrMagnitude>.01f?Mathf.Clamp01(Vector3.Dot(flat,axis)/axis.sqrMagnitude):0;bool threatened=(flat-axis*t).magnitude<radius;hit=true;if(threatened&&p.y-game.World.GroundAt(p)<2.8f)player.TakeDamage(damage);game.Audio.Cue("beam");}if(age>delay+.3f)Destroy(gameObject);return;}
            float r=type==1?Mathf.Max(.2f,(age-delay)*6):radius;
            for(int i=0;i<48;i++){float a=i*Mathf.PI*2/48+ (type==3?age*1.5f:0);Vector3 at=transform.position+new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r);at.y=game.World.GroundAt(at)+.09f;line.SetPosition(i,at);}
            float distance=GameDirector.FlatDistance(p,transform.position);
            if(age<delay)return;
            line.startWidth=line.endWidth=type==1?.2f:.11f;
            if(type==1){float feet=p.y-1.6f;if(!hit&&Mathf.Abs(distance-r)<.6f&&feet<game.World.GroundAt(p)+.5f){hit=true;player.TakeDamage(damage);}if(r>40)Destroy(gameObject);}
            else {if(distance<radius){if(type==3&&player.Motor.enabled){Vector3 toward=transform.position-p;toward.y=0;player.Motor.Move(toward.normalized*Time.deltaTime*2.5f);}if(Time.time>nextTick){nextTick=Time.time+1;float before=game.Run.health;player.TakeDamage(damage);if(type==2&&game.Run.health<before)player.ApplyStatus(status);}}
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

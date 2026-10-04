using UnityEngine;
namespace Tidebreak
{
    // The line follows the captain for one second, visibly locks, then breaches.
    // Lateral movement after the lock is the counter; a constant circle is not guaranteed safety.
    public sealed class TrackingBreach : MonoBehaviour
    {
        GameDirector game;Vector3 origin,endpoint;float age,damage;bool struck;LineRenderer center,left,right;
        const float Width=1.65f,LockTime=1,StrikeTime=1.85f;
        public static void Create(GameDirector game,Vector3 origin,float damage,float delay=0)
        {
            var obj=new GameObject("White whale tracking breach");obj.transform.SetParent(game.Hazards);
            var b=obj.AddComponent<TrackingBreach>();b.game=game;b.origin=origin;b.damage=damage;b.age=-delay;
            b.center=b.MakeLine("Sonar bearing");b.left=b.MakeLine("Port boundary");b.right=b.MakeLine("Starboard boundary");
            b.endpoint=game.Player.transform.position;
        }
        LineRenderer MakeLine(string label){var obj=new GameObject(label);obj.transform.SetParent(transform);var l=obj.AddComponent<LineRenderer>();l.positionCount=2;l.material=Shape.Mat(new Color(.25f,.85f,1),true);l.startWidth=l.endWidth=.07f;return l;}
        void Update()
        {
            if(game.Paused)return;if(game.State!=VoyageState.Combat){Destroy(gameObject);return;}age+=Time.deltaTime;
            center.enabled=left.enabled=right.enabled=age>=0;if(age<0)return;
            if(age<LockTime)endpoint=game.Player.transform.position;
            Vector3 direction=endpoint-origin;direction.y=0;direction.Normalize();Vector3 side=Vector3.Cross(Vector3.up,direction)*Width;
            Vector3 from=origin,to=endpoint+direction*12;from.y=game.World.GroundAt(from)+.15f;to.y=game.World.GroundAt(to)+.15f;
            Color c=age<LockTime?new Color(.25f,.8f,1):new Color(1,.45f,.15f);
            foreach(var l in new[]{center,left,right}){l.startColor=l.endColor=c;}
            center.SetPosition(0,from);center.SetPosition(1,to);left.SetPosition(0,from+side);left.SetPosition(1,to+side);right.SetPosition(0,from-side);right.SetPosition(1,to-side);
            if(age>=StrikeTime&&!struck){struck=true;Vector3 a=origin,b=endpoint+direction*12,p=game.Player.transform.position;a.y=b.y=p.y=0;Vector3 d=b-a;float t=Mathf.Clamp01(Vector3.Dot(p-a,d)/Mathf.Max(.01f,d.sqrMagnitude));if(Vector3.Distance(p,a+d*t)<Width)game.Player.TakeDamage(damage);
                center.startWidth=center.endWidth=Width*1.3f;game.Audio.Cue("explosion");for(int i=0;i<12;i++){Vector3 v=Vector3.Lerp(from,to,i/11f);v.y=game.World.GroundAt(v)+.2f;game.Effect(v,Color.cyan,4,.22f);}}
            if(age>StrikeTime+.35f)Destroy(gameObject);
        }
    }
}

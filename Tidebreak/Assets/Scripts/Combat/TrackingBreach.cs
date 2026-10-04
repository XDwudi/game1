using UnityEngine;
namespace Tidebreak
{
    // The line follows the captain for one second, visibly locks, then breaches.
    // Lateral movement after the lock is the counter; a constant circle is not guaranteed safety.
    public sealed class TrackingBreach : MonoBehaviour
    {
        GameDirector game;Vector3 origin,endpoint;float age,damage;bool struck,paintedLock;LineRenderer center,left,right;System.Action<bool> resolved;
        const float Width=1.65f,LockTime=1,StrikeTime=1.85f;
        public bool Locked {get{return age>=LockTime;}}
        public float Remaining {get{return Mathf.Max(0,StrikeTime-age);}}
        public bool Threatens(Vector3 point)
        {
            if(age<0||struck)return false;Vector3 direction=endpoint-origin;direction.y=0;direction.Normalize();
            Vector3 d=endpoint+direction*12-origin,q=point-origin;d.y=q.y=0;
            float t=Mathf.Clamp01(Vector3.Dot(q,d)/Mathf.Max(.01f,d.sqrMagnitude));return (q-d*t).magnitude<Width+.25f;
        }
        public static TrackingBreach Create(GameDirector game,Vector3 origin,float damage,float delay=0,System.Action<bool> onResolved=null)
        {
            var obj=new GameObject("White whale tracking breach");obj.transform.SetParent(game.Hazards);
            var b=obj.AddComponent<TrackingBreach>();b.game=game;b.origin=origin;b.damage=damage;b.age=-delay;b.resolved=onResolved;
            b.center=b.MakeLine("Sonar bearing");b.left=b.MakeLine("Port boundary");b.right=b.MakeLine("Starboard boundary");
            b.endpoint=game.Player.transform.position;
            return b;
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
            foreach(var l in new[]{center,left,right})Shape.TintLine(l,c);
            if(!paintedLock){ThreatField.DrawSurfaceLine(center,game.World,from,to);ThreatField.DrawSurfaceLine(left,game.World,from+side,to+side);ThreatField.DrawSurfaceLine(right,game.World,from-side,to-side);paintedLock=age>=LockTime;}
            if(age>=StrikeTime&&!struck){struck=true;Vector3 a=origin,b=endpoint+direction*12,p=game.Player.transform.position;a.y=b.y=p.y=0;Vector3 d=b-a;float t=Mathf.Clamp01(Vector3.Dot(p-a,d)/Mathf.Max(.01f,d.sqrMagnitude));bool inLane=Vector3.Distance(p,a+d*t)<Width;bool evaded=!inLane||game.Player.DodgeActive;if(inLane)game.Player.TakeDamage(damage);
                center.startWidth=center.endWidth=Width*1.3f;game.Audio.Cue("explosion");for(int i=0;i<12;i++){Vector3 v=Vector3.Lerp(from,to,i/11f);v.y=game.World.GroundAt(v)+.2f;game.Effect(v,Color.cyan,4,.22f);}resolved?.Invoke(evaded);resolved=null;}
            if(age>StrikeTime+.35f)Destroy(gameObject);
        }
    }
}

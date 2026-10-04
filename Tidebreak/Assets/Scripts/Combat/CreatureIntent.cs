using UnityEngine;

namespace Tidebreak
{
    // This is an intent marker only. Damage is always owned by the creature or
    // its projectile, so the drawing cannot accidentally hit the player twice.
    public sealed class CreatureIntent : MonoBehaviour
    {
        Enemy owner;LineRenderer arrow,ring;
        public void Init(Enemy e)
        {
            owner=e;arrow=Make("Locked attack direction",4,false);ring=Make("Attack charge halo",32,true);
        }
        LineRenderer Make(string label,int count,bool loop)
        {
            var obj=new GameObject(label);obj.transform.SetParent(transform,false);
            var line=obj.AddComponent<LineRenderer>();line.material=Shape.Mat(new Color(1,.69f,.27f),true);
            line.useWorldSpace=true;line.positionCount=count;line.loop=loop;line.startWidth=line.endWidth=.065f;return line;
        }
        void LateUpdate()
        {
            if(!owner||owner.dead)return;
            bool active=owner.AttackWindup>0&&owner.game.State==VoyageState.Combat;
            arrow.enabled=ring.enabled=active;if(!active)return;
            Vector3 p=owner.transform.position;float r=.65f+owner.AttackWindup*.5f;
            for(int i=0;i<32;i++){float a=i*Mathf.PI*2/32;ring.SetPosition(i,p+new Vector3(Mathf.Cos(a)*r,.35f,Mathf.Sin(a)*r));}
            bool line=owner.Spec.attack==AttackStyle.Charge||owner.Spec.attack==AttackStyle.Beam||owner.Spec.attack==AttackStyle.Leap||owner.Spec.attack==AttackStyle.Bite;
            arrow.enabled=line;if(!line)return;
            Vector3 end=owner.LockedAim;end.y=owner.game.World.GroundAt(end)+.15f;
            p.y=owner.game.World.GroundAt(p)+.15f;Vector3 axis=(end-p).normalized,side=Vector3.Cross(axis,Vector3.up);
            arrow.SetPosition(0,p);arrow.SetPosition(1,end);arrow.SetPosition(2,end-axis*.7f+side*.4f);arrow.SetPosition(3,end);
        }
    }
}
